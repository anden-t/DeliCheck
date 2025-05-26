using Blazored.LocalStorage;
using DeliCheck.Schemas;
using DeliCheck.Schemas.Requests;
using DeliCheck.Schemas.Responses;
using DeliCheck.Web.Exceptions;
using DeliCheck.Web.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace DeliCheck.Web.Services
{
    public class DeliCheckClient
    {
        private const string XSESSIONTOKEN = "x-session-token";

        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly AlertService _alert;

        private static readonly MediaTypeHeaderValue json = new MediaTypeHeaderValue("application/json");

        public DeliCheckClient(HttpClient httpClient, ILocalStorageService localStorage, AlertService alert)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _alert = alert;
        }

        private async Task<T> ExecuteRequest<T>(HttpMethod method, string url, HttpContent? content, bool needAuth) where T : class
        {
            var message = new HttpRequestMessage(method, url);

            if (content != null)
                message.Content = content;

            if (needAuth)
            {
                var token = await _localStorage.GetItemAsync<AuthToken>("token");

                if (token == null || !token.IsValid)
                    throw new ApiException($"Unauthorized while request {url}");

                message.Headers.Add(XSESSIONTOKEN, token.Token);
            }

            HttpResponseMessage resp;

            try
            {
                resp = await _httpClient.SendAsync(message);
            }
            catch (Exception ex)
            {
                throw new InternalException($"Unknown exception {ex.GetType()} - {ex.Message}");
            }

            if (!resp.IsSuccessStatusCode)
            {
                var str = await resp.Content.ReadAsStringAsync();
                var exception = new ApiException($"Received {resp.StatusCode} status code while request {url}") { StatusCode = resp.StatusCode };


                if (str != null)
                    exception.ApiResponse = JsonSerializer.Deserialize<ApiResponse>(str) ?? new ApiResponse();

                throw exception;
            }

            try
            {
                var str = await resp.Content.ReadAsStringAsync();

                var api = JsonSerializer.Deserialize<T>(str) ?? throw new InternalException($"Received null while request {url}");

                return api;
            }
            catch (Exception ex)
            {
                throw new InternalException($"Unknown {ex.GetType()} exception - {ex.Message}");

            }
        }

        private async Task<T> ExecuteRequest<T>(HttpMethod method, string url, object? obj, bool needAuth) where T : class =>
            await ExecuteRequest<T>(method, url, obj != null ? new StringContent(JsonSerializer.Serialize(obj), json) : null, needAuth);

        private async Task<T> SendJson<T>(HttpMethod method, string url, object? obj, bool needAuth) where T : class => (await ExecuteRequest<ApiResponse<T>>(method, url, obj, needAuth)).Response;
        private async Task<T> PostJson<T>(string url, object obj, bool needAuth) where T : class => await SendJson<T>(HttpMethod.Post, url, obj, needAuth);
        private async Task PostJson(string url, object obj, bool needAuth) => await ExecuteRequest<ApiResponse>(HttpMethod.Post, url, obj, needAuth);
        private async Task<T> GetJson<T>(string url, bool needAuth) where T : class => await SendJson<T>(HttpMethod.Get, url, null, needAuth);
        private async Task GetJson(string url, bool needAuth) => await ExecuteRequest<ApiResponse>(HttpMethod.Get, url, null, needAuth);
        public async Task<bool> IsAuthenticated()
        {
            var token = await _localStorage.GetItemAsync<AuthToken>("token");

            if (token == null)
                return false;

            return token.IsValid;
        }
        public async Task<AuthToken?> GetSessionTokenAsync()
        {
            var token = await _localStorage.GetItemAsync<AuthToken>("token");

            if (token == null)
                return null;

            return token;
        }

        public async Task<LoginResponseModel> Register(string username, string password, string firstname, string lastname, string email, string phonenumber)
        {
            var body = new RegisterRequest()
            {
                Username = username,
                Password = password,
                Firstname = firstname,
                Lastname = lastname,
                Email = email,
                PhoneNumber = phonenumber,
            };

            return await PostJson<LoginResponseModel>("/auth/register", body, false);
        }
        public async Task<LoginResponseModel> RegisterAsGuest(string firstname, string lastname)
        {
            var body = new RegisterGuestRequest()
            {
                Firstname = firstname,
                Lastname = lastname,
            };

            return await PostJson<LoginResponseModel>("/auth/register-guest", body, false);
        }
        public async Task<LoginResponseModel> Login(string username, string password)
        {
            var body = new LoginRequest() { Username = username, Password = password };

            return await PostJson<LoginResponseModel>("/auth/login", body, false);
        }

        public async Task RefreshToken() => await GetJson("/auth/refresh-token", true);
        public async Task Logout() => await GetJson("/auth/logout", true);
        public async Task ClearLogin() { await _localStorage.RemoveItemAsync("token"); await _alert.InvokeAuthStatusChanged(false); }
        public async Task SaveLogin(LoginResponseModel loginResponse)
        {
            if (loginResponse.SessionToken == null)
                throw new InternalException($"Failed to save login - {nameof(loginResponse.SessionToken)} is null");

            AuthToken token = new AuthToken(loginResponse.SessionToken, loginResponse.ExpiresIn);

            await _localStorage.SetItemAsync("token", token);

            await _alert.InvokeAuthStatusChanged(true);
        }

        public async Task<FriendsListResponseModel> SearchFriends(string query) => await GetJson<FriendsListResponseModel>($"/friends/search?query={HttpUtility.UrlEncode(query)}", true);
        public async Task<ProfileResponseModel> GetProfile(int? userid = null) => await GetJson<ProfileResponseModel>($"/profile/get?userId={userid}", true);
        public async Task EditProfile(ProfileEditRequest profile) => await PostJson("/profile/edit", profile, true);
        public async Task EditProfile(string firstname, string lastname, string email, string phoneNumber, string username)
        {
            var body = new ProfileEditRequest()
            {
                Firstname = firstname,
                Lastname = lastname,
                Email = email,
                PhoneNumber = phoneNumber,
                Username = username
            };

            await EditProfile(body);
        }
        public async Task SetAvatar(string base64)
        {
            if (base64.Contains(","))
                base64 = base64.Substring(base64.IndexOf(",") + 1);

            await PostJson("/profile/set-avatar-json", new SetAvatarRequest() { AvatarBase64 = base64 }, true);
        }

        public async Task AddOnlineFriend(int userId) => await GetJson($"/friends/add?userId={userId}", true);
        public async Task AddOfflineFriend(string firstname, string lastname)
        {
            var body = new AddOfflineFriendRequest() { Firstname = firstname, Lastname = lastname };

            await PostJson("/friends/add-offline", body, true);
        }

        public async Task RemoveOfflineFriend(int friendLabelId) => await GetJson($"/friends/remove-offline?friendLabelId={friendLabelId}", true);
        public async Task RemoveOnlineFriend(int userId) => await GetJson($"/friends/remove?userId={userId}", true);

        public async Task<InvoiceResponseModel> InvoiceOcr(CropCheckResult crop, InvoiceSplitType splitType)
        {
            var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(crop.ImageData), "file", "photo.jpg" }
            };

            return (await ExecuteRequest<InvoiceResponse>(HttpMethod.Post, $"/invoices/ocr?x1={crop.X1}&x2={crop.X2}&y1={crop.Y1}&y2={crop.Y2}&split={(int)splitType}", content, true)).Response;
        }
        public async Task<InvoiceResponseModel> InvoiceQr(string qrcode, InvoiceSplitType splitType)
        {
            var body = new QrFnsRequest()
            {
                QrCodeText = qrcode
            };

            return await PostJson<InvoiceResponseModel>($"/invoices/qr?split={(int)splitType}", body, true);
        }
        public async Task<InvoiceResponseModel> GetInvoice(int id) => await GetJson<InvoiceResponseModel>($"/invoices/get?invoiceId={id}", false);
        public async Task<InvoiceResponseModel> EditInvoice(int invoiceId, string name, List<InvoiceEditItem>? items = null)
        {
            var body = new InvoiceEditRequest()
            {
                Id = invoiceId,
                Name = name,
                Items = items
            };

            return await PostJson<InvoiceResponseModel>("/invoices/edit", body, true);
        }
        public async Task EditInvoiceItem(InvoiceItem item)
        {
            var body = new EditInvoiceItemRequest()
            {
                Id = item.Id,
                Name = item.Name,
                Cost = item.Cost,
                Count = item.Count
            };

            await PostJson("/invoices/edit-item", body, true);
        }
        public async Task RemoveInvoiceItem(InvoiceItem item) => await GetJson($"/invoices/remove-item?invoiceItemId={item.Id}", true);
        public async Task<InvoiceItemResponseModel> AddInvoiceItem(int invoiceId, InvoiceItem item)
        {
            var body = new AddInvoiceItemRequest()
            {
                InvoiceId = invoiceId,
                Name = item.Name,
                Cost = item.Cost,
                Count = item.Count
            };

            return await PostJson<InvoiceItemResponseModel>("/invoices/add-item", body, true);
        }
        public async Task<InvoicesListResponseModel> ListMyInvoices() => await GetJson<InvoicesListResponseModel>("/invoices/list", true);

        public async Task FinishInvoiceEditing(int invoiceId) => await GetJson($"/invoices/finish-editing?invoiceId={invoiceId}", true);
        public async Task CreateBills(int invoiceId, List<UserBill> bills)
        {
            var body = new CreateBillsRequest()
            {
                InvoiceId = invoiceId,
                Bills = bills
            };

            await PostJson("/bills/create", body, true);
        }
        public async Task<BillsListResponseModel> GetBills(int invoiceId) => await GetJson<BillsListResponseModel>($"/bills/get?invoiceId={invoiceId}", false);
        public async Task<BillsListResponseModel> ListMyBills(int invoiceId) => await GetJson<BillsListResponseModel>($"/bills/list", true);

        public async Task<QrRedirectResponseModel> GetQrRedirect() => await GetJson<QrRedirectResponseModel>("/qr-redirect", false);

        public async Task<VkAuthResponseModel> Vk() => await GetJson<VkAuthResponseModel>("/auth/vk", false);
        public async Task<VkAuthResponseModel> Vk(string returnUrl) => await GetJson<VkAuthResponseModel>($"/auth/vk?returnUrl={HttpUtility.UrlEncode(returnUrl)}", false);
        public async Task<VkAuthResponseModel> VkConnect() => await GetJson<VkAuthResponseModel>("/auth/vk-connect", true);
        public async Task<VkAuthResponseModel> VkConnect(string returnUrl) => await GetJson<VkAuthResponseModel>($"/auth/vk-connect?returnUrl={HttpUtility.UrlEncode(returnUrl)}", true);
        public async Task<LoginResponseModel> VkCallback(string code, string state, string deviceId) 
            => await GetJson<LoginResponseModel>($"/auth/vk-callback?code={HttpUtility.UrlEncode(code)}&state={HttpUtility.UrlEncode(state)}&device_id={HttpUtility.UrlEncode(deviceId)}", false);
        public async Task VkConnectCallback(string code, string state, string deviceId)
            => await GetJson($"/auth/vk-connect-callback?code={HttpUtility.UrlEncode(code)}&state={HttpUtility.UrlEncode(state)}&device_id={HttpUtility.UrlEncode(deviceId)}", true);
    }
}
