using DeliCheck.Api.Splitting.Events;
using DeliCheck.Models;
using DeliCheck.Schemas.Responses;
using DeliCheck.Schemas.SignalR;
using DeliCheck.Schemas.SignalR.Responses;
using DeliCheck.Utils;
using Microsoft.AspNetCore.SignalR;
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
        public InvoiceSplitting(InvoiceModel invoice, List<InvoiceItemModel> items)
        {
            InvoiceId = invoice.Id;
            Connections = new Dictionary<string, int>();
            CurrentState = new InvoiceSplittingModel()
            {
                InvoiceId = invoice.Id,
                Items = items.Select(x => new SplittingItem() { Cost = x.Cost, Quantity = x.Quantity, QuantityMeasure = x.QuantityMeasure, Id = x.Id, Name = x.Name, UserParts = new Dictionary<int, int>() }).ToList(),
                Users = new List<ProfileResponseModel>(),
                FinishedUsers = new List<int>()
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

                    var profile = user.ToProfileResponseModel();

                    CurrentState.Users.Add(profile);
                    foreach (var item in CurrentState.Items)
                    {
                        item.UserParts.Add(profile.Id, 0);
                    }
                }
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
                if (!Connections.ContainsValue(userId))
                {
                    CurrentState.Users.Remove(user);

                    foreach (var item in CurrentState.Items)
                    {
                        item.UserParts.Remove(userId);
                    }

                    if (CurrentState.FinishedUsers.Contains(userId))
                        CurrentState.FinishedUsers.Remove(userId);
                }
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
            }
            StateChanged?.Invoke(this, new SplittingStateChangedEventArgs(CurrentState));
        }

        public void ChangeUserFinished(int userId, string connectionId)
        {
            lock (CurrentState)
            {
                var user = CurrentState.Users.FirstOrDefault(x => x.Id == userId);
                if (user == null)
                    Join(userId, connectionId);

                if(CurrentState.FinishedUsers.Contains(userId))
                    CurrentState.FinishedUsers.Remove(userId);
                else 
                    CurrentState.FinishedUsers.Add(userId);
            }
            StateChanged?.Invoke(this, new SplittingStateChangedEventArgs(CurrentState));
        }

        public void Finish()
        {
            lock (CurrentState)
            {
                if(CurrentState.Users.All(x => CurrentState.FinishedUsers.Contains(x.Id)))
                    CurrentState.IsFinished = true;
            }

            if (CurrentState.IsFinished)
                Finished?.Invoke(this, new SplittingStateChangedEventArgs(CurrentState));
        }
    }
}
