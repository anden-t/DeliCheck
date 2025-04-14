using DeliCheck.Models;
using DeliCheck.Schemas.Requests;
using DeliCheck.Schemas.Responses;
using DeliCheck.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DeliCheck.Controllers
{
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

        public InvoicesController(IImagePreprocessingService preprocessingService, IOcrService ocrService, IParsingService parsingService, IQrCodeReader qrCodeReader, IFnsParser fnsParser, IAuthService authService, ILogger<InvoicesController> logger)
        {
            _authService = authService;
            _preprocessingService = preprocessingService;
            _ocrService = ocrService;
            _parsingService = parsingService;
            _qrCodeReader = qrCodeReader;
            _fnsParser = fnsParser;
            _logger = logger;
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
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(InvoiceResponseModel), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> OcrAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, int x1, int y1, int x2, int y2, IFormFile file)
        {
            if (x1 > x2 || y1 > y2 || y1 < 0 || x1 < 0) return BadRequest(ResponseBase.Failure("Неправильные координаты точек"));

            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ResponseBase.Failure(Constants.Unauthorized));

            InvoiceModel? model = null;
            List<InvoiceItemModel>? items = null;
            bool parsed = false;

            try
            {
                using var fs = file.OpenReadStream();
                using var preprocessed = await _preprocessingService.PreprocessImageAsync(fs, x1, y1, x2, y2);

                try
                {
                    var qr = await _qrCodeReader.ReadQrCodeAsync(preprocessed);
                    if (qr != null)
                    {
                        await _fnsParser.UpdateKeyAsync();
                        (model, items) = await _fnsParser.GetInvoiceModelAsync(qr);
                        if(model != null && items != null) parsed = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Ошибка при распознавании QR-кода или получении данных с ФНС: {ex.GetType().Name} {ex.Message} {ex.StackTrace} {ex.InnerException?.GetType()?.Name ?? ""} {ex.InnerException?.Message ?? ""}");
                }

                if (!parsed)
                {
                    preprocessed.Position = 0;
                    var text = await _ocrService.GetTextFromImageAsync(preprocessed);
                    if (string.IsNullOrWhiteSpace(text))
                        return BadRequest(ResponseBase.Failure("Не удалось распознать чек. Попробуйте сфотографировать еще раз!"));

                    (model, items) = _parsingService.GetInvoiceModelFromText(text);
                }

                if(model != null && items != null)
                {
                    using (var db = new DatabaseContext())
                    {
                        model.OwnerId = token.UserId;
                        db.Invoices.Add(model);
                        await db.SaveChangesAsync();

                        foreach (var item in items)
                        {
                            item.InvoiceId = model.Id;
                            db.InvoicesItems.Add(item);
                        }

                        await db.SaveChangesAsync();
                        return Ok(new InvoiceResponse() { Invoice = InvoiceResponseModel.FromModel(model, db) });
                    }
                }
                else return BadRequest(ResponseBase.Failure("Не удалось распознать чек. Попробуйте сфотографировать еще раз!"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при распознавании чека: {ex.GetType().Name} {ex.Message} {ex.StackTrace} {ex.InnerException?.GetType()?.Name ?? ""} {ex.InnerException?.Message ?? ""}");
                return StatusCode(500, ResponseBase.Failure("Не удалось распознать чек. Попробуйте сфотографировать еще раз!"));
            }
        }

        /// <summary>
        /// Получает информацию о чеке по invoiceId
        /// </summary>
        /// <param name="invoiceId">Идентификатор чека</param>
        /// <returns></returns>
        [HttpGet("get")]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(InvoiceResponseModel), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult GetInvoice([FromHeader(Name = "x-session-token")][Required] string sessionToken, int invoiceId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ResponseBase.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == invoiceId);
                if (invoice == null)
                    return BadRequest(ResponseBase.Failure("Не удалось найти чек с таким ID"));

                return Ok(new InvoiceResponse() { Invoice = InvoiceResponseModel.FromModel(invoice, db) });
            }
        }

        /// <summary>
        /// Изменить чек. Если какой-либо параметр не надо изменять, присвойте null
        /// </summary>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("edit")]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(InvoiceResponseModel), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> EditInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, InvoiceEditRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ResponseBase.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.Id && x.OwnerId == token.UserId && !x.BillsCreated);
                if (invoice == null)
                    return BadRequest(ResponseBase.Failure("Не удалось найти чек с таким ID"));

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

                return Ok(new InvoiceResponse() { Invoice = InvoiceResponseModel.FromModel(invoice, db) });
            }
        }

        /// <summary>
        /// Удаляет чек из списка чеков пользователя
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="invoiceId">Идентификатор чека</param>
        /// <returns></returns>
        [HttpGet("remove")]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, int invoiceId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ResponseBase.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == invoiceId);
                if (invoice == null)
                    return BadRequest(ResponseBase.Failure("Не удалось найти чек с таким ID"));

                db.Invoices.Remove(invoice);

                foreach (var item in db.InvoicesItems.Where(x => x.InvoiceId == invoice.Id))
                    db.InvoicesItems.Remove(item);

                await db.SaveChangesAsync();
                return Ok(ResponseBase.Success());
            }
        }

        /// <summary>
        /// Получить счета для чека
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="invoiceId">Идентификатор чека</param>
        /// <returns></returns>
        [HttpGet("get-bills")]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(BillsListResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult GetBillsForInvoice([FromHeader(Name = "x-session-token")][Required] string sessionToken, int invoiceId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ResponseBase.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var bills = db.Bills.Where(x => x.InvoiceId == invoiceId).ToList().Select(x => BillResponseModel.FromModel(x, db)).Where(x => x != null).ToList();
                return Ok(new BillsListResponse() { Bills = bills });
            }
        }

        /// <summary>
        /// Пометить счет оплаченным
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="billId">Идентификатор счета</param>
        /// <returns></returns>
        [HttpGet("pay-bill")]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> PayBillAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, int billId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ResponseBase.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var bill = db.Bills.FirstOrDefault(x => x.Id == billId && !x.Payed);

                if (bill == null) return BadRequest(ResponseBase.Failure("Не удалось найти счет с данным ID"));

                if (bill.OfflineOwner ?
                    (db.OfflineFriends.FirstOrDefault(x => x.Id == bill.OwnerId)?.OwnerId != token.UserId) :
                    (bill.OwnerId != token.UserId)) return StatusCode(403, ResponseBase.Failure("Вы не являетесь владельцем счета"));

                bill.Payed = true;
                await db.SaveChangesAsync();
                return Ok(ResponseBase.Success());
            }
        }

        /// <summary>
        /// Создать счета для пользователей (т. е. разделить чек между пользователями)
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("create-bills")]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(InvoiceResponseModel), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> CreateBillsForInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, CreateBillsRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ResponseBase.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.InvoiceId && x.OwnerId == token.UserId && !x.BillsCreated);
                if (invoice == null)
                    return BadRequest(ResponseBase.Failure("Не удалось найти чек с таким ID"));

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

            return Ok(ResponseBase.Success());
        }

        /// <summary>
        /// Получает список счетов, которые выставлены для текущего пользователя
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <returns></returns>
        [HttpGet("list-bills")]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(BillsListResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult ListBills([FromHeader(Name = "x-session-token")][Required] string sessionToken)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ResponseBase.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == token.Id);
                if (user == null) return BadRequest(ResponseBase.Failure(Constants.UserNotFound));
                var list = db.Bills.Where(x => !x.OfflineOwner && x.OwnerId == token.UserId).ToList().Select(x => BillResponseModel.FromModel(x, db)).ToList();
                return Ok(new BillsListResponse() { Bills = list });
            }
        }

        /// <summary>
        /// Получает список чеков, которые загрузил текущий пользователь
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <returns></returns>
        [HttpGet("list")]
        [ProducesResponseType(typeof(ResponseBase), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(InvoiceResponseModel), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult ListInvoices([FromHeader(Name = "x-session-token")][Required] string sessionToken)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ResponseBase.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var list = db.Invoices.Where(x => x.OwnerId == token.UserId).ToList().Select(x => InvoiceResponseModel.FromModel(x, db)).ToList();
                return Ok(new InvoicesListResponseModel() { Invoices = list });
            }
        }

    }
}
