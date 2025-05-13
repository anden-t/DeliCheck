using DeliCheck.Schemas.Requests;
using DeliCheck.Schemas.Responses;
using DeliCheck.Services;
using DeliCheck.Utils;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DeliCheck.Controllers
{
    /// <summary>
    /// Контроллер, представляющий методы для профилей
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAvatarService _avatarService;
        private readonly IConfiguration _configuration;
        /// <summary>
        /// Создает контроллер, представляющий методы для профилей
        /// </summary>
        public ProfileController(IAuthService authService, IAvatarService avatarService, IConfiguration configuration)
        {
            _avatarService = avatarService;
            _authService = authService;
            _configuration = configuration;
        }

        /// <summary>
        /// Получает информацию о пользователе
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="userId">Идентификатор пользователя, информацию о котором необходимо получить</param>
        /// <returns></returns>
        [HttpGet("get")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProfileResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult GetProfile([FromHeader(Name = "x-session-token")][Required] string sessionToken, int? userId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            userId ??= token.UserId;

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == userId);
                if (user == null) return BadRequest(ApiResponse.Failure(Constants.UserNotFound));

                return Ok(new ProfileResponse(user.ToProfileResponseModel(_configuration)));
            }
        }

        /// <summary>
        /// Устанавливает аватар для пользователя
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="file">Аватар в формате JPG или PNG</param>
        /// <returns></returns>
        [HttpPost("set-avatar")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> SetAvatar([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] IFormFile file)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == token.UserId);
                if (user == null) return BadRequest(ApiResponse.Failure(Constants.UserNotFound));

                if (file.ContentType != "image/jpeg" || file.ContentType != "image/jpg" || file.ContentType != "image/png")
                    return BadRequest(ApiResponse.Failure("Принимаются файлы только типа image/jpeg, image/jpg или image/png"));
                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest(ApiResponse.Failure("Принимаются файлы размером не более 10 МБ"));

                using (var stream = file.OpenReadStream())
                {
                    if (await _avatarService.SaveUserAvatarAsync(stream, user.Id))
                    {
                        user.HasAvatar = true;
                        await db.SaveChangesAsync();
                        return Ok(ApiResponse.Success());
                    }
                    else return StatusCode(500, ApiResponse.Failure("Не удалось сохранить аватар."));
                }
            }
        }
        /// <summary>
        /// Устанавливает аватар для пользователя (файл в Json в Base64)
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="file">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("set-avatar-json")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> SetAvatar([FromHeader(Name = "x-session-token")][Required] string sessionToken, [FromBody][Required] SetAvatarRequest file)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == token.UserId);
                if (user == null) return BadRequest(ApiResponse.Failure(Constants.UserNotFound));

                using (var stream = new MemoryStream(Convert.FromBase64String(file.AvatarBase64)))
                {
                    if (await _avatarService.SaveUserAvatarAsync(stream, user.Id))
                    {
                        user.HasAvatar = true;
                        await db.SaveChangesAsync();
                        return Ok(ApiResponse.Success());
                    }
                    else return StatusCode(500, ApiResponse.Failure("Не удалось сохранить аватар."));
                }
            }
        }

        /// <summary>
        /// Удаляет аватар для пользователя
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <returns></returns>
        [HttpGet("remove-avatar")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveAvatar([FromHeader(Name = "x-session-token")][Required] string sessionToken)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == token.UserId);
                if (user == null) return BadRequest(ApiResponse.Failure(Constants.UserNotFound));

                if (_avatarService.RemoveUserAvatar(user.Id))
                {
                    user.HasAvatar = false;
                    await db.SaveChangesAsync();
                }
                return Ok(ApiResponse.Success());
            }
        }
        

        /// <summary>
        /// Изменяет данные в профиле
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("edit")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> ProfileEditAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [FromBody][Required] ProfileEditRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));
            
            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == token.UserId);
                if (user == null) return BadRequest(ApiResponse.Failure(Constants.UserNotFound));

                request.Username = request.Username?.Trim();
                request.Firstname = request.Firstname?.Trim();
                request.Lastname = request.Lastname?.Trim();
                request.Email = request.Email?.Trim();
                request.PhoneNumber = request.PhoneNumber?.Trim();

                if (!string.IsNullOrWhiteSpace(request.Firstname) && request.Firstname.Length > 20)
                    return BadRequest(ApiResponse.Failure("Неправильно введено \"Имя\""));
                if (!string.IsNullOrWhiteSpace(request.Lastname) && request.Lastname.Length > 20)
                    return BadRequest(ApiResponse.Failure("Неправильно введено \"Фамилия\""));
                if (!string.IsNullOrWhiteSpace(request.Username) && request.Username.Length < 4 || request.Username.Length > 20)
                    return BadRequest(ApiResponse.Failure("Неправильно введено \"Юзернейм\". Должен быть длиной от 4 до 20 символов"));
                if (!string.IsNullOrWhiteSpace(request.Email) && !Regex.IsMatch(request.Email, @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$"))
                    return BadRequest(ApiResponse.Failure("Неправильно введено \"Электронная почта\""));
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && (request.PhoneNumber.Length < 7 || request.PhoneNumber.Substring(1).Any(x => !char.IsDigit(x))))
                    return BadRequest(ApiResponse.Failure("Неправильно введено \"Номер телефона\""));

                if(!string.IsNullOrEmpty(request.Username))
                    user.Username = request.Username;
                if(!string.IsNullOrEmpty(request.Firstname))
                    user.Firstname = request.Firstname;
                if(!string.IsNullOrEmpty(request.Lastname))
                    user.Lastname = request.Lastname;

                user.Email = request.Email ?? string.Empty;
                user.PhoneNumber = request.PhoneNumber ?? string.Empty;

                await db.SaveChangesAsync();

                return Ok(ApiResponse.Success());
            }
        }
    }
}
