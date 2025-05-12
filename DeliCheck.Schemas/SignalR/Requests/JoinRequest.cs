namespace DeliCheck.Schemas.SignalR.Requests
{
    /// <summary>
    /// Запрос SignalR на подключение к лобби
    /// </summary>
    public class JoinRequest : BaseSplittingRequest
    {
        /// <summary>
        /// Имя метода
        /// </summary>
        public const string MethodName = "Join";
    }
}
