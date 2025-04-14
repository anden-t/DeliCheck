using DeliCheck.Schemas.Responses;
using DeliCheck.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DeliCheck.Controllers
{
    /// <summary>
    /// Контроллер аватаров
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class AvatarsController : ControllerBase
    {
        private readonly IAvatarService _avatarService;

        /// <summary>
        /// Создает контроллер аватаров
        /// </summary>
        /// <param name="avatarService"></param>
        public AvatarsController(IAvatarService avatarService)
        {
            _avatarService = avatarService;
        }

        /// <summary>
        /// Получает аватар зарегистрированного пользователя. Формат: JPG, 200x200
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns></returns>
        [HttpGet("user")]
        [ProducesResponseType(typeof(FileResult), (int)System.Net.HttpStatusCode.OK)]
        public FileResult GetAvatarForUser([FromQuery] [Required]int userId)
        {
            var fs =_avatarService.GetUserAvatar(userId);
            if(fs == null)
                fs = _avatarService.GetDefaultAvatar();

            return File(fs, "image/jpeg");
        }

        /// <summary>
        /// Получает аватар незарегистрированного пользователя (друга). Формат: JPG, 200x200
        /// </summary>
        /// <param name="friendLabelId">Идентификатор записи о друге</param>
        /// <returns></returns>
        [HttpGet("friend")]
        [ProducesResponseType(typeof(FileResult), (int)System.Net.HttpStatusCode.OK)]
        public FileResult GetAvatarForFriend([FromQuery] [Required] int friendLabelId)
        {
            var fs = _avatarService.GetFriendAvatar(friendLabelId);
            if (fs == null)
                fs = _avatarService.GetDefaultAvatar();

            return File(fs, "image/jpeg");
        }
    }
}
