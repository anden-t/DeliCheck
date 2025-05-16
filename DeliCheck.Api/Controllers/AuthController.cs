using DeliCheck.Models;
using DeliCheck.Schemas.Requests;
using DeliCheck.Schemas.Responses;
using DeliCheck.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DeliCheck.Controllers
{
    /// <summary>
    /// Методы для авторизации
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IVkApi _vkApiService;
        private readonly IAuthProvider _authProvider;

        private static readonly Dictionary<string, string> _codeVerifiers = new Dictionary<string, string>();
        /// <summary>
        /// Создает контроллер, представляющий методы для авторизации
        /// </summary>
        public AuthController(IAuthService authService, IAuthProvider authProvider, IVkApi vkApiService)
        {
            _vkApiService = vkApiService;
            _authService = authService;
            _authProvider = authProvider;
        }

        /// <summary>
        /// Регистрирует пользователя
        /// </summary>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(LoginResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> RegisterAsync([FromBody][Required] RegisterRequest request)
        {
            request.Username = request.Username.Trim();
            request.Firstname = request.Firstname.Trim();
            request.Lastname = request.Lastname?.Trim();
            request.Email = request.Email?.Trim();
            request.PhoneNumber = request.PhoneNumber?.Trim();
            request.Password = request.Password.Trim();

            if (string.IsNullOrWhiteSpace(request.Firstname) || request.Firstname.Length > 20)
                return BadRequest(ApiResponse.Failure("Неправильно введено \"Имя\"" ));
            if (!string.IsNullOrWhiteSpace(request.Lastname) && request.Lastname.Length > 20)
                return BadRequest(ApiResponse.Failure("Неправильно введено \"Фамилия\""));
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 4 || request.Username.Length > 20)
                return BadRequest(ApiResponse.Failure("Неправильно введено \"Юзернейм\". Должен быть длиной от 4 до 20 символов" ));
            if (!string.IsNullOrWhiteSpace(request.Email) && !Regex.IsMatch(request.Email, @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$"))
                return BadRequest(ApiResponse.Failure("Неправильно введено \"Электронная почта\"" ));
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && (request.PhoneNumber.Length < 7 || request.PhoneNumber.Substring(1).Any(x => !char.IsDigit(x))))
                return BadRequest(ApiResponse.Failure("Неправильно введено \"Номер телефона\""));
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6 || request.Password.Length > 50 || request.Password.Contains(" "))
                return BadRequest(ApiResponse.Failure("Неправильно введено \"Пароль\". Должен быть длиной от 6 до 50 символов и не содержать пробелов"));

            var result = await _authProvider.RegisterAsync(request.Username, request.Email ?? string.Empty, request.PhoneNumber ?? string.Empty, request.Password, request.Firstname, request.Lastname ?? string.Empty);

            if (result.State == RegisterResultState.UserExist)
            {
                return BadRequest(ApiResponse.Failure("Пользователь с таким юзернеймом уже существует"));
            }
            else if (result.State == RegisterResultState.InvalidData)
            {
                return BadRequest(ApiResponse.Failure("Введены неверные данные"));
            }
            else
            {
                var token = await _authService.CreateSessionTokenAsync(result.User.Id, Request.Headers.UserAgent.FirstOrDefault() ?? "default");
                return Ok(new LoginResponse(new LoginResponseModel() { ExpiresIn = token.ExpiresInHours, SessionToken = token.SessionToken, UserId = result.User.Id }));
            }
        }

        /// <summary>
        /// Регистрирует пользователя как гостя (необходимо только отображаемое имя)
        /// </summary>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("register-guest")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(LoginResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> RegisterGuestAsync([FromBody][Required] RegisterGuestRequest request)
        {
            request.Firstname = request.Firstname.Trim();
            request.Lastname = request.Lastname?.Trim();

            if (string.IsNullOrWhiteSpace(request.Firstname) || request.Firstname.Length > 20)
                return BadRequest(ApiResponse.Failure("Неправильно введено \"Имя\""));
            if (!string.IsNullOrWhiteSpace(request.Lastname) && request.Lastname.Length > 20)
                return BadRequest(ApiResponse.Failure("Неправильно введено \"Фамилия\""));

            var username = _authProvider.GetGuestUsername();
            var result =  await _authProvider.RegisterAsync(username, string.Empty, string.Empty, _authProvider.GetRandomPassword(), request.Firstname, request.Lastname ?? string.Empty);
            
            if (result.State == RegisterResultState.UserExist)
            {
                return BadRequest(ApiResponse.Failure("Пользователь с таким юзернеймом уже существует"));
            }
            else if (result.State == RegisterResultState.InvalidData)
            {
                return BadRequest(ApiResponse.Failure("Введены неверные данные"));
            }
            else 
            {
                var token = await _authService.CreateSessionTokenAsync(result.User.Id, GetDevice());
                
                return Ok(new LoginResponse(new LoginResponseModel() { ExpiresIn = token.ExpiresInHours, SessionToken = token.SessionToken, UserId = result.User.Id }));
            }
        }

        /// <summary>
        /// Авторизация по имени пользователя и паролю
        /// </summary>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(LoginResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> LoginAsync([FromBody][Required] LoginRequest request)
        {
            var result = await _authProvider.LoginByUsernameAsync(request.Username ?? string.Empty, request.Password ?? string.Empty, GetDevice());

            if (result == null)
                return BadRequest(new ApiResponse() { Status = -1, Message = "Неправильное имя пользователя или пароль" });

            return Ok(new LoginResponse(new LoginResponseModel() { ExpiresIn = result.ExpiresInHours, SessionToken = result.SessionToken, UserId = result.UserId }));
        }


        /// <summary>
        /// Обновляет уже существующий токен. Должен вызываться при заходе в приложение/на сайт
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <returns></returns>
        [HttpGet("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> RefreshTokenAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            if (await _authService.RefreshSessionTokenAsync(token))
                return Ok(ApiResponse.Success());
            else
                return BadRequest(ApiResponse.Failure("Не удалось обновить токен"));
        }

        /// <summary>
        /// Меняет старый пароль на новый
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpGet("change-password")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> ChangePassword([FromHeader(Name = "x-session-token")][Required] string sessionToken, [FromBody][Required] ChangePasswordRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));
            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == token.Id);
                if (user == null) return BadRequest(ApiResponse.Failure(Constants.UserNotFound));

                bool success = await _authProvider.ChangePasswordAsync(user.Id, request.OldPassword, request.NewPassword);

                if (success)
                    return Ok(ApiResponse.Success());
                else
                    return BadRequest(ApiResponse.Failure("Неправильно введен старый пароль, попробуйте еще раз"));
            }
        }

        /// <summary>
        /// Выйти из аккаунта (сделать токен неактивным)
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <returns></returns>
        [HttpGet("logout")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> LogoutAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));
            
            using (var db = new DatabaseContext())
            {
                await _authService.DisableTokenAsync(token);

                return Ok(ApiResponse.Success());
            }
        }

        /// <summary>
        /// Возвращает ссылку для авторизации через ВК
        /// </summary>
        /// <returns>Ссылка для авторизации через ВК</returns>
        [HttpGet("vk")]
        [ProducesResponseType(typeof(VkAuthResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult Vk(string? returnUrl)
        {
            var codeVerifier = _vkApiService.GetCodeVerifier();
            var codeChallenge = _vkApiService.GetCodeChallenge(codeVerifier);
            var state = _vkApiService.GetState();
            
            if (_codeVerifiers.ContainsKey(state)) _codeVerifiers.Remove(state);
            _codeVerifiers.Add(state, codeVerifier);

            if(string.IsNullOrWhiteSpace(returnUrl))
                return Ok(new VkAuthResponse(new VkAuthResponseModel() { Url = _vkApiService.GetVkAuthUrl(codeChallenge, state) }));
            else
                return Ok(new VkAuthResponse(new VkAuthResponseModel() { Url = _vkApiService.GetVkAuthUrl(codeChallenge, state, returnUrl) }));
        }

        /// <summary>
        /// Возвращает ссылку для привязки профиля к ВК
        /// </summary>
        /// <returns>Ссылка для привязки профиля к ВК</returns>
        [HttpGet("vk-connect")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult VkConnect([FromHeader(Name = "x-session-token")] [Required] string sessionToken, string? returnUrl)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            var codeVerifier = _vkApiService.GetCodeVerifier();
            var codeChallenge = _vkApiService.GetCodeChallenge(codeVerifier);
            var state = token.SessionToken;

            if (_codeVerifiers.ContainsKey(state)) _codeVerifiers.Remove(state);
            _codeVerifiers.Add(state, codeVerifier);

            if (string.IsNullOrWhiteSpace(returnUrl))
                return Ok(new VkAuthResponse(new VkAuthResponseModel() { Url = _vkApiService.GetConnectVkAuthUrl(codeChallenge, state) }));
            else
                return Ok(new VkAuthResponse(new VkAuthResponseModel() { Url = _vkApiService.GetConnectVkAuthUrl(codeChallenge, state, returnUrl) }));
        }

        /// <summary>
        /// Callback метод для авторизации ВК. Возвращает токен сессии, если надо, регистрирует пользователя
        /// </summary>
        /// <param name="code">Параметр VK OAuth</param>
        /// <param name="state">Параметр VK OAuth</param>
        /// <param name="device_id">Параметр VK OAuth</param>
        /// <returns>Возвращает токен сессии, если надо, регистрирует пользователя</returns>
        [HttpGet("vk-callback")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(LoginResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> VkCallback(string code, string state, string device_id)
        {
            if (_codeVerifiers.TryGetValue(state, out var codeVerifier))
            {
                VkAuthorizationData vkAuthData;
                SessionTokenModel token;
                UserModel user;

                (vkAuthData, token, user) = await _authProvider.LoginVkAsync(code, device_id, codeVerifier, state, GetDevice());
                await _vkApiService.UpdateUserInfoAsync(vkAuthData);
                await _vkApiService.UpdateFriendsListAsync(vkAuthData);

                return Ok(new LoginResponse(new LoginResponseModel() { ExpiresIn = token.ExpiresInHours, SessionToken = token.SessionToken, UserId = user.Id }));
            }
            else return BadRequest(ApiResponse.Failure("Не смог найти codeVerifier для данного state. Попробуйте ещё раз."));
        }

        /// <summary>
        /// Callback метод для авторизации ВК. Привязывает профиль ВК к пользователю.
        /// </summary>
        /// <param name="code">Параметр VK OAuth</param>
        /// <param name="state">Параметр VK OAuth</param>
        /// <param name="device_id">Параметр VK OAuth</param>
        /// <returns></returns>
        [HttpGet("vk-connect-callback")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> VkConnectCallback(string code, string state, string device_id)
        {
            if (!state.Contains("."))
            {
                var dotIndex = state.Length - 32;
                state = $"{state.Substring(0, dotIndex)}.{state.Substring(dotIndex)}";
            }

            var token = _authService.GetSessionTokenByString(state);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            if (_codeVerifiers.TryGetValue(state, out var codeVerifier))
            {
                var vkAuthData = await _authProvider.ConnectVkAsync(code, device_id, codeVerifier, state, token.UserId);
                if (vkAuthData != null)
                {
                    await _vkApiService.UpdateUserInfoAsync(vkAuthData);
                    await _vkApiService.UpdateFriendsListAsync(vkAuthData);
                    return Ok(ApiResponse.Success());
                }
                else return BadRequest(ApiResponse.Failure("Не удалось авторизоваться через ВК"));
            }
            else return BadRequest(ApiResponse.Failure("Не смог найти codeVerifier для данного state. Попробуйте ещё раз."));
        }

        private string GetDevice()
        {
            return Request.Headers.UserAgent.FirstOrDefault() ?? "default";
        }
    }
}
