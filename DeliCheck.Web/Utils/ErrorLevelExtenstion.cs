using DeliCheck.Schemas.SignalR.Responses;
using Radzen;

namespace DeliCheck.Web.Utils
{
    public static class ErrorLevelExtenstion
    {
        public static NotificationSeverity GetNotificationSeverity(this NotifyLevel notifyLevel)
        {
            switch (notifyLevel)
            {
                case NotifyLevel.Error: return NotificationSeverity.Error;
                case NotifyLevel.Warning: return NotificationSeverity.Warning;
                case NotifyLevel.Info: return NotificationSeverity.Info;
                case NotifyLevel.Success: return NotificationSeverity.Success;
                default: return NotificationSeverity.Error;
            }
        }
    }
}
