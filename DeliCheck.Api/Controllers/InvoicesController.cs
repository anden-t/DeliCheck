using DeliCheck.Models;
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
        private readonly ILogger _logger;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

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
        /// Распознает чек по OCR либо по QR-коду из ФНС
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="file">Изображение</param>
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
        public async Task<IActionResult> OcrAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, int x1, int y1, int x2, int y2, IFormFile file)
        {
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
        /// Распознает чек по OCR либо по QR-коду из ФНС
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="file">Изображение</param>
        /// <param name="x1">Левая точка для обрезки фото</param>
        /// <param name="y1">Верхняя точка для обрезки фото</param>
        /// <param name="x2">Правая точка для обрезки фото</param>
        /// <param name="y2">Нижняя точка для обрезки фото</param>
        /// <returns>Ответ с моделью чека</returns>
        [HttpPost("qr")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(InvoiceResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> QrAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, QrFnsRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));
            try
            {
                await _fnsParser.UpdateKeyAsync();

                InvoiceModel? invoice;
                List<InvoiceItemModel>? items;

                (invoice, items) = await _fnsParser.GetInvoiceModelAsync(request.Content);

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
        /// Изменить чек. Если какой-либо параметр не надо изменять, присвойте null
        /// </summary>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("edit")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(InvoiceResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> EditInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, InvoiceEditRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.Id && x.OwnerId == token.UserId && !x.BillsCreated);
                if (invoice == null)
                    return BadRequest(ApiResponse.Failure("Не удалось найти чек с таким ID"));

                if (request.Items != null && request.Items.Count > 0)
                {
                    foreach (var oldItem in db.InvoicesItems.Where(x => x.InvoiceId == invoice.Id))
                        db.InvoicesItems.Remove(oldItem);

                    foreach (var item in request.Items)
                        db.InvoicesItems.Add(new InvoiceItemModel() { InvoiceId = invoice.Id, Cost = item.Cost, Name = item.Name, Count = item.Count });
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
        public async Task<IActionResult> RemoveInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, int invoiceId, bool deleteBills)
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
        /// Получить счета для чека
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="invoiceId">Идентификатор чека</param>
        /// <returns></returns>
        [HttpGet("get-bills")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(BillsListResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult GetBillsForInvoice([Required] int invoiceId)
        {
            using (var db = new DatabaseContext())
            {
                var bills = db.Bills.Where(x => x.InvoiceId == invoiceId).ToList().Select(x => x.ToResponseModel(db, _configuration)).Where(x => x != null).ToList();
                return Ok(new BillsListResponse(bills));
            }
        }

        /// <summary>
        /// Пометить счет оплаченным
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="billId">Идентификатор счета</param>
        /// <returns></returns>
        [HttpGet("pay-bill")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> PayBillAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] int billId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var bill = db.Bills.FirstOrDefault(x => x.Id == billId && !x.Payed);

                if (bill == null) return BadRequest(ApiResponse.Failure("Не удалось найти счет с данным ID"));

                if (bill.OfflineOwner ?
                    (db.OfflineFriends.FirstOrDefault(x => x.Id == bill.OwnerId)?.OwnerId != token.UserId) :
                    (bill.OwnerId != token.UserId)) return StatusCode(403, ApiResponse.Failure("Вы не являетесь владельцем счета"));

                bill.Payed = true;
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
        [HttpGet("remove-invoice-item")]
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

                var invoice = db.Invoices.FirstOrDefault(x => x.Id == item.InvoiceId && x.OwnerId == token.UserId);
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
        [HttpPost("edit-invoice-item")]
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

                var invoice = db.Invoices.FirstOrDefault(x => x.Id == item.InvoiceId && x.OwnerId == token.UserId);
                if (invoice == null) return StatusCode(403, ApiResponse.Failure("Вы не являетесь владельцем чека или чек не найден"));

                item.Cost = request.Cost;
                item.Count = request.Count;
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
        [HttpPost("add-invoice-item")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(InvoiceItemResponseModel), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> AddInvoiceItemAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] AddInvoiceItemRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.InvoiceId && x.OwnerId == token.UserId);
                if (invoice == null) return StatusCode(403, ApiResponse.Failure("Вы не являетесь владельцем чека или чек не найден"));

                var item = new InvoiceItemModel()
                {
                    Cost = request.Cost,
                    Count = request.Count,
                    InvoiceId = request.InvoiceId,
                    Name = request.Name
                };

                db.InvoicesItems.Add(item);
                await db.SaveChangesAsync();
                invoice.TotalCost = db.InvoicesItems.Where(x => x.InvoiceId == invoice.Id).Sum(x => x.Cost);
                await db.SaveChangesAsync();
                return Ok(new InvoiceItemResponse(new InvoiceItemResponseModel() { Name = item.Name, Id = item.Id, Cost = item.Cost, Count = item.Count }));
            }
        }

        /// <summary>
        /// Создать счета для пользователей (т. е. разделить чек между пользователями)
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("create-bills")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> CreateBillsForInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] CreateBillsRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.InvoiceId && x.OwnerId == token.UserId && !x.BillsCreated);
                if (invoice == null)
                    return BadRequest(ApiResponse.Failure("Не удалось найти чек с таким ID"));

                foreach (var userBill in request.Bills)
                {
                    var bill = new BillModel()
                    {
                        InvoiceId = request.InvoiceId,
                        OfflineOwner = userBill.OfflineOwner,
                        OwnerId = userBill.OwnerId,
                        Payed = false
                    };
                    db.Bills.Add(bill);
                    await db.SaveChangesAsync();

                    foreach (var userBillItem in userBill.Items)
                    {
                        var invoiceItem = db.InvoicesItems.FirstOrDefault(x => x.Id == userBillItem.ItemId);
                        if(invoiceItem != null)
                        {
                            db.BillsItems.Add(new BillItemModel()
                            {
                                Cost = invoiceItem.Cost / invoiceItem.Count * userBillItem.Count,
                                Count = userBillItem.Count,
                                Name = invoiceItem.Name,
                                BillId = bill.Id
                            });
                        }
                    }
                    await db.SaveChangesAsync();
                    bill.TotalCost = db.BillsItems.Where(x => x.BillId == bill.Id).Sum(x => x.Cost);
                }
                invoice.BillsCreated = true;

                await db.SaveChangesAsync();
            }

            return Ok(ApiResponse.Success());
        }

        /// <summary>
        /// Получает список счетов, которые выставлены для текущего пользователя
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <returns></returns>
        [HttpGet("list-bills")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(BillsListResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult ListBills([FromHeader(Name = "x-session-token")][Required] string sessionToken)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == token.Id);
                if (user == null) return BadRequest(ApiResponse.Failure(Constants.UserNotFound));
                var list = db.Bills.Where(x => !x.OfflineOwner && x.OwnerId == token.UserId).ToList().Select(x => x.ToResponseModel(db, _configuration)).ToList();
                return Ok(new BillsListResponse(list));
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
