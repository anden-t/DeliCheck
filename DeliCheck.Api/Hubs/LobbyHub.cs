using DeliCheck.Api.Splitting;
using DeliCheck.Schemas;
using DeliCheck.Schemas.SignalR.Requests;
using DeliCheck.Schemas.SignalR.Responses;
using DeliCheck.Services;
using Microsoft.AspNetCore.SignalR;

namespace DeliCheck.Api.Hubs
{
    public class LobbyHub : Hub
    {
        private IAuthService _authService;

        private static List<InvoiceSplitting> _splittings = new List<InvoiceSplitting>();
        private IConfiguration _configuration;
        private IHubContext<LobbyHub> _hubContext;
        private ILogger<LobbyHub> _logger;

        public LobbyHub(IAuthService authService, IConfiguration configuration, IHubContext<LobbyHub> hubContext, ILogger<LobbyHub> logger)
        {
            _logger = logger;
            _hubContext = hubContext;
            _authService = authService;
            _configuration = configuration;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _splittings.ForEach(x => x.Leave(Context.ConnectionId));
        }

        [HubMethodName(CreateRequest.MethodName)]
        public async Task Create(CreateRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Detail = Constants.Unauthorized });
                return;
            } 

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.InvoiceId && x.OwnerId == token.UserId && x.EditingFinished && x.SplitType == InvoiceSplitType.ByMembers && !x.BillsCreated);

                InvoiceSplitting? existedSplitting;

                if (invoice == null) 
                {
                    await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Detail = "Не нашел чека с таким ID" });
                    return;
                }
                else if ((existedSplitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId)) != null)
                {
                    await Clients.Caller.SendAsync(ActualInvoiceSplittingResponse.MethodName, new ActualInvoiceSplittingResponse() { SplittingModel = existedSplitting.CurrentState });
                    return;
                }

                var items = db.InvoicesItems.Where(x => x.InvoiceId == request.InvoiceId).ToList();
                var splitting = new InvoiceSplitting(invoice, items, _configuration);
                splitting.StateChanged += OnSplittingStateChanged;
                splitting.Finished += OnSplittingFinished;
                _splittings.Add(splitting);
                
                await Clients.Caller.SendAsync(ActualInvoiceSplittingResponse.MethodName, new ActualInvoiceSplittingResponse() { SplittingModel = splitting.CurrentState });
            }
        }

        [HubMethodName(JoinRequest.MethodName)]
        public async Task Join(JoinRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = Constants.Unauthorized });
                return;
            }

            var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
            if(splitting == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = "Лобби для чека с таким ID не найдено" });
                return;
            }

            splitting.Join(token.UserId, Context.ConnectionId);
            await Clients.Caller.SendAsync(ActualInvoiceSplittingResponse.MethodName, new ActualInvoiceSplittingResponse() { SplittingModel = splitting.CurrentState });
        }

        [HubMethodName(LeaveRequest.MethodName)]
        public async Task Leave(LeaveRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = Constants.Unauthorized });
                return;
            }

            var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
            if (splitting == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = "Лобби для чека с таким ID не найдено" });
                return;
            }

            splitting.Leave(token.UserId, Context.ConnectionId);
        }

        [HubMethodName(SelectItemRequest.MethodName)]
        public async Task SelectItem(SelectItemRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = Constants.Unauthorized });
                return;
            }

            var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
            if (splitting == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = "Лобби для чека с таким ID не найдено" });
                return;
            }

            splitting.SelectItem(token.UserId, request.ItemId, Context.ConnectionId);
        }

        [HubMethodName(UserFinishedRequest.MethodName)]
        public async Task UserFinished(UserFinishedRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = Constants.Unauthorized });
                return;
            }

            var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
            if (splitting == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = "Лобби для чека с таким ID не найдено" });
                return;
            }

            splitting.UserFinished(token.UserId, Context.ConnectionId);
        }

        [HubMethodName(UserNotFinishedRequest.MethodName)]
        public async Task UserNotFinished(UserNotFinishedRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = Constants.Unauthorized });
                return;
            }

            var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
            if (splitting == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = "Лобби для чека с таким ID не найдено" });
                return;
            }

            splitting.UserNotFinished(token.UserId, Context.ConnectionId);
        }

        [HubMethodName(FinishRequest.MethodName)]
        public async Task Finish(FinishRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = Constants.Unauthorized });
                return;
            }

            var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
            if (splitting == null)
            {
                await Clients.Caller.SendAsync(NotifyResponse.MethodName, new NotifyResponse() { Level = NotifyLevel.Error, Summary = "Ошибка", Detail = "Лобби для чека с таким ID не найдено" });
                return;
            }

            await splitting.FinishAsync();
        }


        private async void OnSplittingFinished(object? sender, Splitting.Events.SplittingStateChangedEventArgs e)
        {
            try
            {
                var splitting = sender as InvoiceSplitting;
                if (splitting != null)
                {
                    await _hubContext.Clients.Clients(splitting.Connections.Keys).SendAsync(InvoiceSplittingFinishedResponse.MethodName, new InvoiceSplittingFinishedResponse() { InvoiceId = splitting.InvoiceId });

                    _splittings.Remove(splitting);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during OnSplittingStateChanged()");
            }
        }

        private async void OnSplittingStateChanged(object? sender, Splitting.Events.SplittingStateChangedEventArgs e)
        {
            try
            {
                var splitting = sender as InvoiceSplitting;
                if (splitting != null)
                {
                    await _hubContext.Clients.Clients(splitting.Connections.Keys).SendAsync(ActualInvoiceSplittingResponse.MethodName, new ActualInvoiceSplittingResponse() { SplittingModel = splitting.CurrentState });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during OnSplittingStateChanged()");
            }
        }
    }
}
