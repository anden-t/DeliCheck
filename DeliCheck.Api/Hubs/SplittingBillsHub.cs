using DeliCheck.Api.Splitting;
using DeliCheck.Schemas;
using DeliCheck.Schemas.SignalR.Requests;
using DeliCheck.Schemas.SignalR.Responses;
using DeliCheck.Services;
using Microsoft.AspNetCore.SignalR;

namespace DeliCheck.Api.Hubs
{
    public class SplittingBillsHub : Hub
    {
        private IAuthService _authService;

        private static List<InvoiceSplitting> _splittings = new List<InvoiceSplitting>();

        public SplittingBillsHub(IAuthService authService)
        {
            _authService = authService;
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
                await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = Constants.Unauthorized });
                return;
            } 

            using (var db = new DatabaseContext())
            {
                var invoice = db.Invoices.FirstOrDefault(x => x.Id == request.InvoiceId && x.OwnerId == token.UserId && x.EditingFinished && x.SplitType == InvoiceSplitType.ByMembers && !x.BillsCreated);

                if (invoice == null) 
                {
                    await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = "Не нашел чека с таким ID" });
                    return;
                }
                else if (_splittings.Any(x => x.InvoiceId == request.InvoiceId))
                {
                    await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = "Лобби для чека с таким ID уже создано" });
                    return;
                }

                var items = db.InvoicesItems.Where(x => x.InvoiceId == request.InvoiceId).ToList();
                var splitting = new InvoiceSplitting(invoice, items);
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
                await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = Constants.Unauthorized });
                return;
            }

            using (var db = new DatabaseContext())
            {
                var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
                if(splitting == null)
                {
                    await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = "Лобби для чека с таким ID не найдено" });
                    return;
                }

                splitting.Join(token.UserId, Context.ConnectionId);
            }
        }

        [HubMethodName(LeaveRequest.MethodName)]
        public async Task Leave(LeaveRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = Constants.Unauthorized });
                return;
            }

            using (var db = new DatabaseContext())
            {
                var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
                if (splitting == null)
                {
                    await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = "Лобби для чека с таким ID не найдено" });
                    return;
                }

                splitting.Leave(token.UserId, Context.ConnectionId);
            }
        }

        [HubMethodName(SelectItemRequest.MethodName)]
        public async Task SelectItem(SelectItemRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = Constants.Unauthorized });
                return;
            }

            using (var db = new DatabaseContext())
            {
                var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
                if (splitting == null)
                {
                    await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = "Лобби для чека с таким ID не найдено" });
                    return;
                }

                splitting.SelectItem(token.UserId, request.ItemId, Context.ConnectionId);
            }
        }

        [HubMethodName(ChangeUserFinishedRequest.MethodName)]
        public async Task ChangeUserFinished(SelectItemRequest request)
        {
            var token = _authService.GetSessionTokenByString(request.SessionToken);
            if (token == null)
            {
                await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = Constants.Unauthorized });
                return;
            }

            using (var db = new DatabaseContext())
            {
                var splitting = _splittings.FirstOrDefault(x => x.InvoiceId == request.InvoiceId);
                if (splitting == null)
                {
                    await Clients.Caller.SendAsync(ErrorResponse.MethodName, new ErrorResponse() { Level = ErrorLevel.Error, Message = "Лобби для чека с таким ID не найдено" });
                    return;
                }

                splitting.ChangeUserFinished(token.UserId, Context.ConnectionId);
            }
        }

        private async void OnSplittingFinished(object? sender, Splitting.Events.SplittingStateChangedEventArgs e)
        {
            var splitting = sender as InvoiceSplitting;
            if (splitting != null)
            {
                await Clients.Clients(splitting.Connections.Keys).SendAsync(InvoiceSplittingFinishedResponse.MethodName, new InvoiceSplittingFinishedResponse() { InvoiceId = splitting.InvoiceId });

                _splittings.Remove(splitting);
            }
        }

        private async void OnSplittingStateChanged(object? sender, Splitting.Events.SplittingStateChangedEventArgs e)
        {
            var splitting = sender as InvoiceSplitting;
            if (splitting != null)
            {
                await Clients.Clients(splitting.Connections.Keys).SendAsync(ActualInvoiceSplittingResponse.MethodName, new ActualInvoiceSplittingResponse() { SplittingModel = splitting.CurrentState });
            }
        }
    }
}
