using DeliCheck.Schemas.SignalR;

namespace DeliCheck.Api.Splitting.Events
{
    public class SplittingFinishedEventArgs : EventArgs
    {
        public InvoiceSplittingModel CurrentState { get; private set; }
        public SplittingFinishedEventArgs(InvoiceSplittingModel state)
        {
            CurrentState = state;
        }
    }
}
