using DeliCheck.Models;
using DeliCheck.Schemas.Requests;
using DeliCheck.Schemas.Responses;
using DeliCheck.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DeliCheck.Controllers
{
    /// <summary>
    /// Контроллер для взамодействия со списком друзей
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class FriendsController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IVkApi _vkApi;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Создает контроллер, представляющий методы для списка друзей
        /// </summary>
        public FriendsController(IAuthService authService, IVkApi vkApi, IConfiguration configuration)
        {
            _authService = authService;
            _vkApi = vkApi;
            _configuration = configuration;
        }

        /// <summary>
        /// Добавляет зарегистрированного пользователя в друзья
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="userId">Идентификатор пользователя, которого необходимо добавить в друзья</param>
        /// <returns></returns>
        [HttpGet("add")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> AddFriendAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] int userId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == userId);
                if (user != null)
                {
                    db.Friends.Add(new FriendModel()
                    {
                        OwnerId = token.UserId,
                        FriendId = user.Id
                    });

                    await db.SaveChangesAsync();

                    return Ok(ApiResponse.Success());
                }
                else return BadRequest(ApiResponse.Failure(Constants.UserNotFound));
            }
        }

        /// <summary>
        /// Удаялет зарегистрированного пользователя из друзей
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="userId">Идентификатор пользователя, которого необходимо удалить из друзей</param>
        /// <returns></returns>
        [HttpGet("remove")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveFriendAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] int userId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == userId);
                if (user != null)
                {
                    var friend = db.Friends.FirstOrDefault(x => x.OwnerId == token.UserId && x.FriendId == user.Id);

                    if(friend != null)
                    {
                        db.Friends.Remove(friend);
                        await db.SaveChangesAsync();
                        return Ok(ApiResponse.Success());
                    }
                    else return BadRequest(ApiResponse.Failure("Друг с таким userId не найден"));

                }
                else return BadRequest(ApiResponse.Failure(Constants.UserNotFound));
            }
        }

        /// <summary>
        /// Добавляет незарегистрированного пользователя в друзья
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="request">Тело запроса</param>
        /// <returns></returns>
        [HttpPost("add-offline")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> AddOfflineFriendAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [FromBody][Required] AddOfflineFriendRequest request)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            request.Firstname ??= string.Empty;
            request.Lastname ??= string.Empty;

            request.Firstname = request.Firstname.Trim();
            request.Lastname = request.Lastname.Trim();

            if (string.IsNullOrEmpty(request.Firstname) && string.IsNullOrEmpty(request.Lastname))
                return BadRequest(ApiResponse.Failure("Фамилия и имя не должны быть одновременно пустые"));

            if (request.Firstname.Length > 20 || request.Lastname.Length > 20)
                return BadRequest(ApiResponse.Failure("Длина имени или фамилии не должна превышать 20 символов"));

            using (var db = new DatabaseContext())
            {
                db.OfflineFriends.Add(new OfflineFriendModel()
                {
                    OwnerId = token.UserId,
                    HasAvatar = false,
                    VkId = null,
                    Firstname = request.Firstname,
                    Lastname = request.Lastname
                });

                await db.SaveChangesAsync();

                return Ok(ApiResponse.Success());
            }
        }

        /// <summary>
        /// Удаялет незарегистрированного пользователя из друзей
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="friendLabelId">Идентификатор записи о друге, которую необходимо удалить</param>
        /// <returns></returns>
        [HttpGet("remove-offline")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveOffineFriendAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken, [Required] int friendLabelId)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var friend = db.OfflineFriends.FirstOrDefault(x => x.OwnerId == token.UserId && x.Id == friendLabelId);

                if (friend != null)
                {
                    db.OfflineFriends.Remove(friend);
                    await db.SaveChangesAsync();
                    return Ok(ApiResponse.Success());
                }
                else return BadRequest(ApiResponse.Failure("Друг с таким userId не найден"));
            }
        }

        /// <summary>
        /// Импортирует список друзей из ВК
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <returns></returns>
        [HttpGet("import-vk")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.OK)]
        public async Task<IActionResult> ImportVkFriendsAsync([FromHeader(Name = "x-session-token")][Required] string sessionToken)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == token.UserId);

                if (user == null || user.VkId == null) return BadRequest(ApiResponse.Failure("Не найден привязанный профиль ВК"));

                var vkAuthData = db.VkAuth.OrderBy(e => e.CreateTime).LastOrDefault(x => x.UserId == token.UserId);
                if (vkAuthData == null) return BadRequest(ApiResponse.Failure("Не найден привязанный профиль ВК"));

                await _vkApi.UpdateFriendsListAsync(vkAuthData);
            }

            return Ok(ApiResponse.Success());
        }

        /// <summary>
        /// Поиск пользователей (включая незарегистрированных друзей) по строке (учитывается имя и фамилия)
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(FriendsListResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult SearchFriends([FromHeader(Name = "x-session-token")][Required] string sessionToken, string? query)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            query ??= string.Empty;

            using (var db = new DatabaseContext())
            {
                return Ok(new FriendsListResponse(new FriendsListResponseModel() { Friends = SearchUsers(db, token.UserId, query), OnlyOnline = false, UserId = token.UserId }));
            }
        }


        /// <summary>
        /// Получает список друзей пользователя
        /// </summary>
        /// <param name="sessionToken">Токен сессии</param>
        /// <param name="userId">Идентификатор пользователя. Если null, возвращает список своих друзей</param>
        /// <param name="onlyOnline">True, если необходимо получить список только онлайн друзей</param>
        /// <returns></returns>
        [HttpGet("list")]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), (int)System.Net.HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(FriendsListResponse), (int)System.Net.HttpStatusCode.OK)]
        public IActionResult GetFriendsList([FromHeader(Name = "x-session-token")][Required] string sessionToken, int? userId, bool onlyOnline = false)
        {
            var token = _authService.GetSessionTokenByString(sessionToken);
            if (token == null) return Unauthorized(ApiResponse.Failure(Constants.Unauthorized));

            if(userId == null)
                userId = token.UserId;

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == userId);

                if (user != null) return Ok(new FriendsListResponse(new FriendsListResponseModel() { Friends = GetFriends(db, user.Id, onlyOnline), UserId = user.Id, OnlyOnline = onlyOnline }));
                else return BadRequest(ApiResponse.Failure(Constants.UserNotFound));
            }
        }

        private List<FriendResponseModel> SearchUsers(DatabaseContext db, int userId, string query)
        {
            if (query == string.Empty) return GetFriends(db, userId, false);

            query = query.ToLower();
            var domain = _configuration["Domain"];
            var users = db.Users.ToList().Where(x => x.Firstname.ToLower().StartsWith(query) || x.Lastname.ToLower().StartsWith(query) || $"{x.Firstname} {x.Lastname}".ToLower().StartsWith(query)).ToList();
            var offlineFriends = db.OfflineFriends.ToList().Where(x => x.OwnerId == userId && (x.Firstname.ToLower().StartsWith(query) || x.Lastname.ToLower().StartsWith(query) || $"{x.Firstname} {x.Lastname}".ToLower().StartsWith(query))).ToList();

            var list = new List<FriendResponseModel>(users.Count + offlineFriends.Count);

            foreach (var user in users)
            {
                if(user.Id != userId)
                {
                    var fUser = db.Friends.FirstOrDefault(x => x.OwnerId == userId && x.FriendId == user.Id);
                    list.Add(new FriendResponseModel()
                    {
                        Firstname = user.Firstname,
                        Lastname = user.Lastname,
                        FriendLabelId = fUser?.Id,
                        UserId = user.Id,
                        AvatarUrl = user.HasAvatar ? $"https://{domain}/avatars/user?userId={user.Id}" : $"https://{domain}/avatars/default",
                        HasAvatar = user.HasAvatar,
                        HasProfile = true,
                        HasVk = user.VkId != null,
                        VkId = user.VkId
                    }); ;
                }
            }

            foreach (var offlineFriend in offlineFriends)
            {
                list.Add(new FriendResponseModel()
                {
                    Firstname = offlineFriend.Firstname,
                    Lastname = offlineFriend.Lastname,
                    FriendLabelId = offlineFriend.Id,
                    HasProfile = false,
                    AvatarUrl = offlineFriend.HasAvatar ? $"https://{domain}/avatars/friend?friendLabelId={offlineFriend.Id}" : $"https://{domain}/avatars/default",
                    HasAvatar = offlineFriend.HasAvatar,
                    HasVk = offlineFriend.VkId != null,
                    VkId = offlineFriend.VkId
                });
            }

            return list.OrderByDescending(x => x.FriendLabelId ?? -1).ToList();
        }

        private List<FriendResponseModel> GetFriends(DatabaseContext db, int userId, bool onlyOnline)
        {
            var onlineFriends = db.Friends.Where(x => x.OwnerId == userId).ToList();
            var offlineFriends = db.OfflineFriends.Where(x => x.OwnerId == userId).ToList();
            var domain = _configuration["Domain"];

            var list = new List<FriendResponseModel>(onlineFriends.Count + offlineFriends.Count);

            foreach (var onlineFriend in onlineFriends)
            {
                var fUser = db.Users.FirstOrDefault(x => x.Id == onlineFriend.FriendId);
                if (fUser != null)
                {
                    list.Add(new FriendResponseModel()
                    {
                        Firstname = fUser.Firstname,
                        Lastname = fUser.Lastname,
                        FriendLabelId = onlineFriend.Id,
                        UserId = fUser.Id,
                        HasAvatar = fUser.HasAvatar,
                        AvatarUrl = fUser.HasAvatar ? $"https://{domain}/avatars/user?userId={fUser.Id}" : $"https://{domain}/avatars/default",
                        HasProfile = true,
                        HasVk = fUser.VkId != null,
                        VkId = fUser.VkId
                    }) ;
                }
            }

            if (!onlyOnline)
            {
                foreach (var offlineFriend in offlineFriends)
                {
                    list.Add(new FriendResponseModel()
                    {
                        Firstname = offlineFriend.Firstname,
                        Lastname = offlineFriend.Lastname,
                        FriendLabelId = offlineFriend.Id,
                        HasProfile = false,
                        HasAvatar = offlineFriend.HasAvatar,
                        HasVk = offlineFriend.VkId != null,
                        AvatarUrl = offlineFriend.HasAvatar ? $"https://{domain}/avatars/friend?friendLabelId={offlineFriend.Id}" : $"https://{domain}/avatars/default",
                        VkId = offlineFriend.VkId
                    });
                }
            }

            list = list.OrderByDescending(x => $"{x.Firstname} {x.Lastname}").ToList();
            return list;
        }
    }
}
