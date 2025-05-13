using DeliCheck.Api.Splitting.Events;
using DeliCheck.Models;
using DeliCheck.Schemas.Requests;
using DeliCheck.Schemas.Responses;
using DeliCheck.Schemas.SignalR;
using DeliCheck.Schemas.SignalR.Responses;
using DeliCheck.Utils;
using Microsoft.AspNetCore.SignalR;
using ZXing;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DeliCheck.Api.Splitting
{
    public class InvoiceSplitting
    {
        public InvoiceSplittingModel CurrentState { get; private set; }
        public int InvoiceId { get; private set; }

        public Dictionary<string, int> Connections { get; private set; }

        public event EventHandler<SplittingStateChangedEventArgs> StateChanged;
        public event EventHandler<SplittingStateChangedEventArgs> Finished;

        private int _invoiceOwnerId;
        private IConfiguration _configuration;
        public InvoiceSplitting(InvoiceModel invoice, List<InvoiceItemModel> items, IConfiguration configuration)
        {
            _invoiceOwnerId = invoice.OwnerId;
            InvoiceId = invoice.Id;
            _configuration = configuration;
            Connections = new Dictionary<string, int>();
            CurrentState = new InvoiceSplittingModel()
            {
                InvoiceId = invoice.Id,
                Items = items.Select(x => new SplittingItem() { Cost = x.Cost, Quantity = x.Quantity, QuantityMeasure = x.QuantityMeasure, Id = x.Id, Name = x.Name, UserParts = new Dictionary<int, int>() }).ToList(),
                Users = new List<ProfileResponseModel>(),
                FinishedUsers = new List<int>(),
                UsersSum = new Dictionary<int, int>()
            };
        }

        public void Join(int userId, string connectionId)
        {
            lock (CurrentState)
            {
                using (var db = new DatabaseContext())
                {
                    var user = db.Users.FirstOrDefault(x => x.Id == userId);

                    if (user == null)
                        return;
                    
                    Connections.Add(connectionId, userId);

                    if (CurrentState.Users.Any(x => x.Id == user.Id))
                        return;

                    var profile = user.ToProfileResponseModel(_configuration);

                    CurrentState.Users.Add(profile);
                    foreach (var item in CurrentState.Items)
                    {
                        item.UserParts.Add(profile.Id, 0);
                    }
                }
                UpdateSums();
            }
            StateChanged?.Invoke(this, new SplittingStateChangedEventArgs(CurrentState));
        }

        public void Leave(int userId, string connectionId)
        {
            lock (CurrentState)
            {
                var user = CurrentState.Users.FirstOrDefault(x => x.Id == userId);
                if (user == null)
                    return;

                Connections.Remove(connectionId);
                if (!Connections.ContainsValue(userId) && !CurrentState.FinishedUsers.Contains(userId) && userId != _invoiceOwnerId)
                {
                    CurrentState.Users.Remove(user);

                    foreach (var item in CurrentState.Items)
                    {
                        item.UserParts.Remove(userId);
                    }

                    if (CurrentState.FinishedUsers.Contains(userId))
                        CurrentState.FinishedUsers.Remove(userId);
                }
                UpdateSums();
            }
            StateChanged?.Invoke(this, new SplittingStateChangedEventArgs(CurrentState));
        }

        public void Leave(string connectionId)
        {
            if(Connections.TryGetValue(connectionId, out var userId))
            {
                Leave(userId, connectionId);
            }
        }

        public void SelectItem(int userId, int itemId, string connectionId)
        {
            lock (CurrentState)
            {
                var user = CurrentState.Users.FirstOrDefault(x => x.Id == userId);
                if (user == null)
                    Join(userId, connectionId);

                var item = CurrentState.Items.FirstOrDefault(x => x.Id == itemId);
                if (item == null)
                    return;

                if(item.UserParts.TryGetValue(userId, out var part))
                {
                    if(item.Quantity % 1 == 0 && item.Quantity != 1)
                    {
                        var max = item.Quantity;
                        var sum = item.UserParts.Where(x => x.Key != userId).Sum(x => x.Value);
                        if (++part + sum > max) item.UserParts[userId] = 0;
                        else item.UserParts[userId]++;
                    }
                    else
                    {
                        if (part == 1) item.UserParts[userId] = 0;
                        else item.UserParts[userId] = 1;
                    }
                }
                UpdateSums();
            }
            StateChanged?.Invoke(this, new SplittingStateChangedEventArgs(CurrentState));
        }

        public void UserFinished(int userId, string connectionId)
        {
            lock (CurrentState)
            {
                var user = CurrentState.Users.FirstOrDefault(x => x.Id == userId);
                if (user == null)
                    Join(userId, connectionId);

                if(!CurrentState.FinishedUsers.Contains(userId))
                    CurrentState.FinishedUsers.Add(userId);
            }
            StateChanged?.Invoke(this, new SplittingStateChangedEventArgs(CurrentState));
        }

        public void UserNotFinished(int userId, string connectionId)
        {
            lock (CurrentState)
            {
                var user = CurrentState.Users.FirstOrDefault(x => x.Id == userId);
                if (user == null)
                    Join(userId, connectionId);

                if (CurrentState.FinishedUsers.Contains(userId))
                    CurrentState.FinishedUsers.Remove(userId);
            }
            StateChanged?.Invoke(this, new SplittingStateChangedEventArgs(CurrentState));
        }

        public async Task FinishAsync()
        {
            if(CurrentState.Users.All(x => CurrentState.FinishedUsers.Contains(x.Id)))
                CurrentState.IsFinished = true;

            if (CurrentState.IsFinished)
            {
                var result = GetBills();
                using (var db = new DatabaseContext())
                {
                    var invoice = db.Invoices.FirstOrDefault(x => x.Id == InvoiceId);
                    foreach (var userBill in result.Bills)
                    {
                        var bill = new BillModel()
                        {
                            InvoiceId = result.InvoiceId,
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
                    if(invoice != null)
                        invoice.BillsCreated = true;

                    await db.SaveChangesAsync();
                }
                Finished?.Invoke(this, new SplittingStateChangedEventArgs(CurrentState));
            }
        }

        private void UpdateSums()
        {
            CurrentState.UsersSum.Clear();
            foreach (var user in CurrentState.Users)
            {
                decimal sum = 0;
                foreach (var item in CurrentState.Items.Where(x => x.UserParts.ContainsKey(user.Id) && x.UserParts[user.Id] > 0))
                {
                    decimal userPart;

                    if (item.Quantity == 1 || item.Quantity % 1 != 0)
                    {
                        var allParts = item.UserParts.Values.Sum();
                        userPart = Math.Round((item.Quantity / allParts) * item.UserParts[user.Id], 2);
                    }
                    else
                    {
                        userPart = item.UserParts[user.Id];
                    }

                    sum += userPart * item.Cost;
                }

                CurrentState.UsersSum.Add(user.Id, (int)Math.Round(sum));
            }
        }

        private CreateBillsRequest GetBills()
        {
            List<UserBill> bills = new List<UserBill>();
            foreach (var user in CurrentState.Users)
            {
                var bill = new UserBill();

                bill.OfflineOwner = false;
                bill.OwnerId = user.Id;
                bill.Items = new List<UserBillItem>();

                foreach (var item in CurrentState.Items.Where(x => x.UserParts.ContainsKey(user.Id) && x.UserParts[user.Id] > 0))
                {
                    decimal userPart;

                    if (item.Quantity == 1 || item.Quantity % 1 != 0)
                    {
                        var allParts = item.UserParts.Values.Sum();
                        userPart = Math.Round((item.Quantity / allParts) * item.UserParts[user.Id], 2);
                    }
                    else
                    {
                        userPart = item.UserParts[user.Id];
                    }

                    bill.Items.Add(new UserBillItem() { ItemId = item.Id, Quantity = userPart });
                }

                bills.Add(bill);
            }

            return new CreateBillsRequest() { Bills = bills, InvoiceId = InvoiceId };
        }
    }
}
