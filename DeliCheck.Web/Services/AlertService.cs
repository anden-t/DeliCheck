namespace DeliCheck.Web.Services
{
    public class AlertService
    {
        public AlertService() { }

        public event OnAuthStatusChanged OnAuthStatusChanged;

        public async Task InvokeAuthStatusChanged(bool authStatus)
        {
            if (OnAuthStatusChanged == null)
                return;

            await OnAuthStatusChanged.Invoke(authStatus);
        }
    }

   public delegate Task OnAuthStatusChanged(bool status);
}
