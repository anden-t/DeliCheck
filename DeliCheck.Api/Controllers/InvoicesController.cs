using DeliCheck.Models;
using DeliCheck.Schemas;
using DeliCheck.Schemas.Requests;
using DeliCheck.Schemas.Responses;
using DeliCheck.Services;
using DeliCheck.Utils;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DeliCheck.Controllers
{
    /// <summary>
    /// Контроллер чеков и счетов
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IImagePreprocessingService _preprocessingService;
        private readonly IOcrService _ocrService;
        private readonly IParsingService _parsingService;
        private readonly IQrCodeReader _qrCodeReader;
        private readonly IFnsParser _fnsParser;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(IImagePreprocessingService preprocessingService, IOcrService ocrService, IParsingService parsingService, IQrCodeReader qrCodeReader, IFnsParser fnsParser, IAuthService authService, IConfiguration configuration, ILogger<InvoicesController> logger)
        {
            _authService = authService;
            _preprocessingService = preprocessingService;
            _ocrService = ocrService;
            _parsingService = parsingService;
            _qrCodeReader = qrCodeReader;
            _fnsParser = fnsParser;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Распознает чек по OCR либо по QR-коду из ФНС (если найдет его на фото)
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="file">Изображение</param>
        /// <param name="split">Тип деления чека; 0 - владелец выбирает позиции; 1 - участники выбирают сами свои позиции</param>
        /// <param name="x1">Левая точка для обрезки фото</param>
        /// <param name="y1">Верхняя точка для обрезки фото</param>
        /// <param name="x2">Правая точка для обрезки фото</param>
        /// <param name="y2">Нижняя точка для обрезки фото</param>
        /// <returns>Ответ с моделью чека</returns>
        [HttpPost("ocr")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(InvoiceResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> OcrAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required][FromQuery] int split, [FromQuery] int x1, [FromQuery] int y1, [FromQuery] int x2, [FromQuery] int y2, [Required] IFormFile file)
        {
            InvoiceSplitType splitType = (InvoiceSplitType)split;
            if (splitType != InvoiceSplitType.ByOwner && splitType != InvoiceSplitType.ByMembers)
                return BadRequest(ApiResponse.Failure("Неправильно указан тип деления чека"));
            if (x1 > x2 || y1 > y2 || y1 < 0 || x1 < 0) return BadRequest(ApiResponse.Failure("Неправильные координаты точек"));

            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            InvoiceModel? invoice = null;
            List<InvoiceItemModel>? items = null;
            bool parsed = false;

            try
            {
                using var fs = file.OpenReadStream();
                try
                {
                    var qr = await _qrCodeReader.ReadQrCodeAsync(fs);
                    if (qr != null)
                    {
                        await _fnsParser.UpdateKeyAsync();
                        Console.WriteLine(qr);
                        (invoice, items) = await _fnsParser.GetInvoiceModelAsync(qr);
                        if(invoice != null && items != null) parsed = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Ошибка при распознавании QR-кода или получении данных с ФНС: {ex.GetType().Name} {ex.Message} {ex.StackTrace} {ex.InnerException?.GetType()?.Name ?? ""} {ex.InnerException?.Message ?? ""}");
                }


                if (!parsed)
                {
                    fs.Position = 0;
                    var preprocessedPath = await _preprocessingService.PreprocessImageAsync(fs, x1, y1, x2, y2);
                    var text = await _ocrService.GetTextFromImageAsync(preprocessedPath);

                    if (string.IsNullOrWhiteSpace(text))
                        return BadRequest(ApiResponse.Failure("Не удалось распознать чек. Попробуйте сфотографировать еще раз!"));

                    (invoice, items) = _parsingService.GetInvoiceModelFromText(text);

                    if (invoice.TotalCost == 0 && items.Count == 0)
                        return BadRequest(ApiResponse.Failure("Не удалось распознать чек. Попробуйте сфотографировать еще раз!"));
                }

                if(invoice != null && items != null)
                {
                    using (var db = new DatabaseContext())
                    {
                        invoice.OwnerId = token.UserId;
                        invoice.CreatedTime = DateTime.UtcNow;
                        db.Invoices.Add(invoice);
                        await db.SaveChangesAsync();

                        foreach (var item in items)
                        {
                            item.InvoiceId = invoice.Id;
                            db.InvoicesItems.Add(item);
                        }

                        await db.SaveChangesAsync();
                        return Ok(new InvoiceResponse(invoice.ToResponseModel(db)));
                    }
                }
                else return BadRequest(ApiResponse.Failure("Не удалось распознать чек. Попробуйте сфотографировать еще раз!"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при распознавании чека: {ex.GetType().Name} {ex.Message} {ex.StackTrace} {ex.InnerException?.GetType()?.Name ?? ""} {ex.InnerException?.Message ?? ""}");
                return StatusCode(500, ApiResponse.Failure("Не удалось распознать чек. Попробуйте сфотографировать еще раз!"));
            }
        }

        /// <summary>
        /// Распознает по QR-коду из ФНС
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="split">Тип деления чека; 0 - владелец выбирает позиции; 1 - участники выбирают сами свои позиции</param>
        /// <param name="request">Тело запроса</param>
        /// <returns>Ответ с моделью чека</returns>
        [HttpPost("qr")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(InvoiceResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> QrAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required][FromQuery]int split, [Required][FromBody]QrFnsRequest request)
        {
            InvoiceSplitType splitType = (InvoiceSplitType)split;
            if (splitType != InvoiceSplitType.ByOwner && splitType != InvoiceSplitType.ByMembers)
                return BadRequest(ApiResponse.Failure("Неправильно указан тип деления чека"));

            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            try
            {
                await _fnsParser.UpdateKeyAsync();

                InvoiceModel? invoice;
                List<InvoiceItemModel>? items;

                (invoice, items) = await _fnsParser.GetInvoiceModelAsync(request.QrCodeText);

                if (invoice != null && items != null)
                {
                    using (var db = new DatabaseContext())
                    {
                        invoice.OwnerId = token.UserId;
                        invoice.CreatedTime = DateTime.UtcNow;
                        db.Invoices.Add(invoice);
                        await db.SaveChangesAsync();

                        foreach (var item in items)
                        {
                            item.InvoiceId = invoice.Id;
                            db.InvoicesItems.Add(item);
                        }

                        await db.SaveChangesAsync();
                        return Ok(new InvoiceResponse(invoice.ToResponseModel(db)));
                    }
                }
                else return BadRequest(ApiResponse.Failure("Не удалось получить информацию по QR-коду"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при получении информацию по QR-коду: {ex.GetType().Name} {ex.Message} {ex.StackTrace} {ex.InnerException?.GetType()?.Name ?? ""} {ex.InnerException?.Message ?? ""}");
                return StatusCode(500, ApiResponse.Failure("Не удалось получить информацию по QR-коду"));
            }
        }

        /// <summary>
        /// Получает информацию о чеке по invoiceId
        /// </summary>
        /// <param name="invoiceId">Идентификатор чека</param>
        /// <returns></returns>
        [HttpGet("get")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(InvoiceResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult GetInvoice([Required] int invoiceId)
        {
            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == invoiceId);
                if (invoice == null)
                    return BadRequest(ApiResponse.Failure("Не удалось найти чек с таким ID"));

                return Ok(new InvoiceResponse(invoice.ToResponseModel(db)));
            }
        }

        /// <summary>
        /// Завершает изменения чека. Необходимо вызвать перед делением чека.
        /// </summary>
        /// <param name="invoiceId">Идентификатор чека</param>
        /// <returns></returns>
        [HttpGet("finish-editing")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> FinishEditingInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] int invoiceId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == invoiceId && x.OwnerId == token.UserId && !x.EditingFinished);
                if (invoice == null)
                    return BadRequest(ApiResponse.Failure("Не удалось найти подходящий чек с таким ID"));

                invoice.EditingFinished = true;
                await db.SaveChangesAsync();
                return Ok();
            }
        }

        /// <summary>
        /// Изменить чек. Если какой-либо параметр не надо изменять, присвойте null
        /// </summary>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("edit")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(InvoiceResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> EditInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] InvoiceEditRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.Id && x.OwnerId == token.UserId && !x.EditingFinished);
                if (invoice == null)
                    return BadRequest(ApiResponse.Failure("Не удалось найти чек с таким ID"));

                if (request.Items != null && request.Items.Count > 0)
                {
                    foreach (var oldItem in db.InvoicesItems.Where(x => x.InvoiceId == invoice.Id))
                        db.InvoicesItems.Remove(oldItem);

                    foreach (var item in request.Items)
                        db.InvoicesItems.Add(new InvoiceItemModel() { InvoiceId = invoice.Id, Cost = item.Cost, Name = item.Name, Quantity = item.Quantity });
                }
                await db.SaveChangesAsync();

                invoice.TotalCost = db.InvoicesItems.Where(x => x.InvoiceId == invoice.Id).Sum(x => x.Cost);

                if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Length < 100)
                    invoice.Name = request.Name;

                await db.SaveChangesAsync();

                return Ok(new InvoiceResponse(invoice.ToResponseModel(db)));
            }
        }

        /// <summary>
        /// Удаляет чек из списка чеков пользователя
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="invoiceId">Идентификатор чека</param>
        /// <param name="deleteBills">Удалить счета чека</param>
        /// <returns></returns>
        [HttpGet("remove")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] int invoiceId, [Required] bool deleteBills)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == invoiceId && x.OwnerId == token.UserId);
                if (invoice == null)
                    return BadRequest(ApiResponse.Failure("Не удалось найти чек с таким ID"));

                db.Invoices.Remove(invoice);

                foreach (var item in db.InvoicesItems.Where(x => x.InvoiceId == invoice.Id))
                    db.InvoicesItems.Remove(item);

                if (invoice.BillsCreated && deleteBills)
                {
                    foreach (var item in db.Bills.Where(x => x.InvoiceId == invoice.Id))
                    {
                        foreach (var billItem in db.BillsItems.Where(x => x.BillId == item.Id))
                            db.BillsItems.Remove(billItem);

                        db.Bills.Remove(item);
                    }
                }

                await db.SaveChangesAsync();
                return Ok(ApiResponse.Success());
            }
        }

        /// <summary>
        /// Удалить позицию из чека
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="invoiceItemId">Индентификатор позиции в чеке</param>
        /// <returns></returns>
        [HttpGet("remove-item")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveInvoiceItemAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] int invoiceItemId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var item = db.InvoicesItems.FirstOrDefault(x => x.Id == invoiceItemId);
                if (item == null) return BadRequest(ApiResponse.Failure("Не удалось найти позицию с данным ID"));

                var invoice = db.Invoices.FirstOrDefault(x => x.Id == item.InvoiceId && x.OwnerId == token.UserId && !x.EditingFinished);
                if (invoice == null) return StatusCode(403, ApiResponse.Failure("Вы не являетесь владельцем чека"));

                db.InvoicesItems.Remove(item);
                await db.SaveChangesAsync();

                invoice.TotalCost = db.InvoicesItems.Where(x => x.InvoiceId == invoice.Id).Sum(x => x.Cost);
                await db.SaveChangesAsync();

                return Ok(ApiResponse.Success());
            }
        }

        /// <summary>
        /// Изменяет позицию чека
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="request">Тело запросае</param>
        /// <returns></returns>
        [HttpPost("edit-item")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> EditInvoiceItemAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] EditInvoiceItemRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var item = db.InvoicesItems.FirstOrDefault(x => x.Id == request.Id);
                if (item == null) return BadRequest(ApiResponse.Failure("Не удалось найти позицию с данным ID"));

                var invoice = db.Invoices.FirstOrDefault(x => x.Id == item.InvoiceId && x.OwnerId == token.UserId && !x.EditingFinished);
                if (invoice == null) return StatusCode(403, ApiResponse.Failure("Вы не являетесь владельцем чека или чек не найден"));

                item.Cost = request.Cost;
                item.Quantity = request.Count;
                item.Name = request.Name;

                await db.SaveChangesAsync();
                invoice.TotalCost = db.InvoicesItems.Where(x => x.InvoiceId == invoice.Id).Sum(x => x.Cost);
                await db.SaveChangesAsync();

                return Ok(ApiResponse.Success());
            }
        }
        
        /// <summary>
        /// Добавляет позицию в чек
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("add-item")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(InvoiceItemResponseModel), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> AddInvoiceItemAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] AddInvoiceItemRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.InvoiceId && x.OwnerId == token.UserId && !x.EditingFinished);
                if (invoice == null) return StatusCode(403, ApiResponse.Failure("Вы не являетесь владельцем чека или чек не найден"));

                var item = new InvoiceItemModel()
                {
                    Cost = request.Cost,
                    Quantity = request.Count,
                    InvoiceId = request.InvoiceId,
                    Name = request.Name
                };

                db.InvoicesItems.Add(item);
                await db.SaveChangesAsync();
                invoice.TotalCost = db.InvoicesItems.Where(x => x.InvoiceId == invoice.Id).Sum(x => x.Cost);
                await db.SaveChangesAsync();
                return Ok(new InvoiceItemResponse(new InvoiceItemResponseModel() { Name = item.Name, Id = item.Id, Cost = item.Cost, Quantity = item.Quantity }));
            }
        }

        /// <summary>
        /// Получает список чеков, в которых пользовтель принимает участие
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <returns></returns>
        [HttpGet("list")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(InvoicesListResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult ListInvoices([FromHeader(Name = "x-session-token")][Required] string sessionToken)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var list = db.Invoices.ToList().Where(x => x.OwnerId == token.UserId || (x.BillsCreated && db.Bills.Any(c => c.InvoiceId == x.Id && !c.OfflineOwner && c.OwnerId == token.UserId))).ToList().Select(x => x.ToResponseModel(db)).ToList(); 
                return Ok(new InvoicesListResponse(new InvoicesListResponseModel() { Invoices = list }));
            }
        }

    }
}
