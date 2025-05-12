using DeliCheck.Schemas.SignalR;

namespace DeliCheck.Api.Splitting.Events
{
    public class SplittingStateChangedEventArgs : EventArgs
    {
        public InvoiceSplittingModel CurrentState { get; private set; }
        public SplittingStateChangedEventArgs(InvoiceSplittingModel state)
        {
            CurrentState = state;
        }
    }
}
