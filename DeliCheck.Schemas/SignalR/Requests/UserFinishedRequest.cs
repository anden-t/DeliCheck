using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliCheck.Schemas.SignalR.Requests
{
    /// <summary>
    /// Запрос SignalR на переключения состояния "Пользователь завершил выбор своих позиций"
    /// </summary>
    public class UserFinishedRequest : BaseSplittingRequest
    {
        /// <summary>
        /// Имя метода
        /// </summary>
        public const string MethodName = "UserFinished";
    }
}
