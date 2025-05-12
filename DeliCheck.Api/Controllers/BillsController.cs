using DeliCheck.Controllers;
using DeliCheck.Models;
using DeliCheck.Schemas.Requests;
using DeliCheck.Schemas.Responses;
using DeliCheck.Services;
using DeliCheck.Utils;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DeliCheck.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly ILogger<BillsController> _logger;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public BillsController(IAuthService authService, IConfiguration configuration, ILogger<BillsController> logger)
        {
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Получить счета для чека
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="invoiceId">Идентификатор чека</param>
        /// <returns></returns>
        [HttpGet("get")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(BillsListResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult GetBillsForInvoice([Required] int invoiceId)
        {
            using (var db = new DatabaseContext())
            {
                var bills = db.Bills.Where(x => x.InvoiceId == invoiceId).ToList().Select(x => x.ToResponseModel(db, _configuration)).Where(x => x != null).ToList();
                return Ok(new BillsListResponse(new BillsListResponseModel() { Bills = bills }));
            }
        }

        /// <summary>
        /// Пометить счет оплаченным
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="billId">Идентификатор счета</param>
        /// <returns></returns>
        [HttpGet("pay")]
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
        /// Создать счета для пользователей (т. е. разделить чек между пользователями)
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> CreateBillsForInvoiceAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] CreateBillsRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.InvoiceId && x.OwnerId == token.UserId && x.EditingFinished && !x.BillsCreated);
                if (invoice == null)
                    return BadRequest(ApiResponse.Failure("Не удалось найти подходящий чек с таким ID"));

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
                        if (invoiceItem != null)
                        {
                            db.BillsItems.Add(new BillItemModel()
                            {
                                Cost = invoiceItem.Cost / invoiceItem.Quantity * userBillItem.Quantity,
                                Quantity = userBillItem.Quantity,
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
        [HttpGet("list")]
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
                var bills = db.Bills.Where(x => !x.OfflineOwner && x.OwnerId == token.UserId).ToList().Select(x => x.ToResponseModel(db, _configuration)).ToList();
                return Ok(new BillsListResponse(new BillsListResponseModel() { Bills = bills }));
            }
        }
    }
}
