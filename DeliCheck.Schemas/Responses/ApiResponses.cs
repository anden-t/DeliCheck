using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Базовый класс для всех ответов.
    /// </summary>
    public class ApiResponse
    {
        private const string _success = "Успешно";

        /// <summary>
        /// Возвращает <see cref="ApiResponse"/> с кодом -1
        /// </summary>
        /// <param name="message">сообщение, которое будет помещено в <see cref="Message"/></param>
        /// <returns></returns>
        public static ApiResponse Failure(string message) => new ApiResponse() { Message = message, Status = -1 };
        /// <summary>
        /// Возвращает <see cref="ApiResponse"/> с кодом 0
        /// </summary>
        /// <param name="message">сообщение, которое будет помещено в <see cref="Message"/></param>
        /// <returns></returns>
        public static ApiResponse Success(string message = _success) => new ApiResponse() { Message = message, Status = 0 };
        /// <summary>
        /// Создает экземпляр ответа с <see cref="Message"/> = <see cref="_success"/> и <see cref="Status"/> = <see cref="0"/>
        /// </summary>
        public ApiResponse()
        {
            Message = _success;
            Status = 0;
        }
        /// <summary>
        /// Статус операции. 0 - успешно
        /// </summary>
        [JsonPropertyName("status")]
        public int Status { get; set; } = 0;

        /// <summary>
        /// Сообщение об результате операции. На русском, можно отображать на frontend
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = _success;
    }

    /// <summary>
    /// Ответ от API
    /// </summary>
    /// <typeparam name="T">Модель результата</typeparam>
    public class ApiResponse<T> : ApiResponse where T : class
    {
        /// <summary>
        /// Тело ответа
        /// </summary>
        [JsonPropertyName("response")]
        public T Response { get; set; }

        public ApiResponse() { }
        public ApiResponse(T response) : base() { Response = response; }
    }

    public class BillsListResponse : ApiResponse<BillsListResponseModel> 
    {
        public BillsListResponse() { }
        public BillsListResponse(BillsListResponseModel response) : base(response) { }
    }
    public class FriendsListResponse : ApiResponse<FriendsListResponseModel>
    {
        public FriendsListResponse() { }
        public FriendsListResponse(FriendsListResponseModel response) : base(response) { }
    }
    public class ProfileResponse : ApiResponse<ProfileResponseModel> 
    {
        public ProfileResponse() { }
        public ProfileResponse(ProfileResponseModel response) : base(response) { }
    }
    public class LoginResponse : ApiResponse<LoginResponseModel> 
    {
        public LoginResponse() { }
        public LoginResponse(LoginResponseModel response) : base(response) { }
    }
    public class VkAuthResponse : ApiResponse<VkAuthResponseModel>
    {
        public VkAuthResponse() { }
        public VkAuthResponse(VkAuthResponseModel response) : base(response) { }
    }
    public class InvoiceResponse : ApiResponse<InvoiceResponseModel>
    {
        public InvoiceResponse() { }
        public InvoiceResponse(InvoiceResponseModel response) : base(response) { }
    }
    public class InvoicesListResponse : ApiResponse<InvoicesListResponseModel>
    {
        public InvoicesListResponse() { }
        public InvoicesListResponse(InvoicesListResponseModel response) : base(response) { }
    }

    public class InvoiceItemResponse : ApiResponse<InvoiceItemResponseModel>
    {
        public InvoiceItemResponse() { }
        public InvoiceItemResponse(InvoiceItemResponseModel response) : base(response) { }   
    }

    public class VkAuthInfoResponse : ApiResponse<VkAuthInfoResponseModel>
    {
        public VkAuthInfoResponse() { }
        public VkAuthInfoResponse(VkAuthInfoResponseModel response) : base(response) { }
    }
}
