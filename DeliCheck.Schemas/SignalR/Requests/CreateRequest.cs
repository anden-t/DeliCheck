using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliCheck.Schemas.SignalR.Requests
{
    /// <summary>
    /// Запрос SignalR на создание лобби
    /// </summary>
    public class CreateRequest : BaseSplittingRequest
    {
        /// <summary>
        /// Имя метода
        /// </summary>
        public const string MethodName = "Create";
    }
}
