using DeliCheck.Api.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace DeliCheck.Services
{
    /// <summary>
    /// Сервис для работы с аватарами пользователя
    /// </summary>
    public class AvatarService : IAvatarService
    {
        private static readonly string _baseFolder = Path.GetFullPath("avatars");
        private static readonly string _userFolder = $"{_baseFolder}/users";
        private static readonly string _friendFolder = $"{_baseFolder}/friends";
        private static readonly string _ext = ".jpg";
        private static readonly Size _size = new Size(200, 200);

        private static int _femaleIndex = 0;
        private static int _maleIndex = 0;
        private readonly ILogger<IAvatarService> _logger;
        private static string _defaultAvatarPath => $"{_baseFolder}/default{_ext}";
        public AvatarService(ILogger<IAvatarService> logger)
        {
            _logger = logger;
            Directory.CreateDirectory(_baseFolder);
            Directory.CreateDirectory(_userFolder);
            Directory.CreateDirectory(_friendFolder);

            if (!File.Exists(_defaultAvatarPath))
            {
                _logger.LogCritical("Default avatar file not found");
            }
        }

        public string GetPathForUserAvatar(int userId) => Path.Combine(_userFolder, $"{userId}{_ext}");
        public string GetPathForFriendAvatar(int friendId) => Path.Combine(_friendFolder, $"{friendId}{_ext}");

        private async Task<bool> SaveAvatarAsync(Stream imageStream, string pathToSave, bool skipCrop = false)
        {
            try
            {
                if (!skipCrop)
                {
                    Image image = await Image.LoadAsync(imageStream);

                    if (image.Width != image.Height)
                    {
                        if (image.Width < image.Height)
                            image.Mutate(x => x.Crop(new Rectangle() { X = 0, Y = (image.Height - image.Width) / 2, Width = image.Width, Height = image.Width }));
                        else
                            image.Mutate(x => x.Crop(new Rectangle() { X = (image.Width - image.Height) / 2, Y = 0, Width = image.Height, Height = image.Height }));
                    }

                    if(image.Width != _size.Width || image.Height != _size.Height)
                        image.Mutate(x => x.Resize(_size.Width, _size.Height));

                    var path = pathToSave;

                    if (File.Exists(path))
                        File.Delete(path);

                    await image.SaveAsJpegAsync(path);

                    return true;
                }
                else
                {
                    using var fs = File.OpenWrite(pathToSave);
                    await imageStream.CopyToAsync(fs);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during convert or save avatar: {ex.GetType().Name} {ex.Message} {ex.StackTrace}");
                return false;
            }
        }
        public async Task<bool> SaveUserAvatarAsync(Stream stream, int userId, bool skipCrop = false) => await SaveAvatarAsync(stream, GetPathForUserAvatar(userId), skipCrop);
        public async Task<bool> SaveFriendAvatarAsync(Stream stream, int friendId, bool skipCrop = false) => await SaveAvatarAsync(stream, GetPathForFriendAvatar(friendId), skipCrop);

        private FileStream? GetAvatar(string path)
        {
            if (File.Exists(path))
            {
                return File.OpenRead(path);
            }
            else return null;
        }
        public FileStream? GetUserAvatar(int userId) => GetAvatar(GetPathForUserAvatar(userId));
        public FileStream? GetFriendAvatar(int friendId) => GetAvatar(GetPathForFriendAvatar(friendId));
        public async Task SetRandomUserAvatar(string firstname, int userId)
        {
            if (FemaleNames.Names.Any(x => x.Equals(firstname, StringComparison.OrdinalIgnoreCase)))
            {
                Directory.CreateDirectory("avatars/kusya");
                var max = Directory.GetFiles("avatars/kusya").Length;

                if (max > 0)
                {
                    if (_femaleIndex >= max)
                        _femaleIndex = 0;

                    await SaveUserAvatarAsync(File.OpenRead($"avatars/kusya/{_femaleIndex++}.jpg"), userId);

                    if (_femaleIndex >= max)
                        _femaleIndex = 0;
                }
            }
            else
            {
                Directory.CreateDirectory("avatars/sberkot");
                var max = Directory.GetFiles("avatars/sberkot").Length;

                if (max > 0)
                {
                    if (_maleIndex >= max)
                        _maleIndex = 0;

                    await SaveUserAvatarAsync(File.OpenRead($"avatars/sberkot/{_maleIndex++}.jpg"), userId);

                    if (_maleIndex >= max)
                        _maleIndex = 0;
                }
            }
        }
        public async Task SetRandomFriendAvatar(string firstname, int friendId)
        {
            if (FemaleNames.Names.Any(x => x.Equals(firstname, StringComparison.OrdinalIgnoreCase)))
            {
                Directory.CreateDirectory("avatars/kusya");
                var max = Directory.GetFiles("avatars/kusya").Length;

                if (max > 0)
                {
                    if (_femaleIndex >= max)
                        _femaleIndex = 0;

                    await SaveFriendAvatarAsync(File.OpenRead($"avatars/kusya/{_femaleIndex++}.jpg"), friendId);

                    if (_femaleIndex >= max)
                        _femaleIndex = 0;
                }
            }
            else
            {
                Directory.CreateDirectory("avatars/sberkot");
                var max = Directory.GetFiles("avatars/sberkot").Length;

                if (max > 0)
                {
                    if (_maleIndex >= max)
                        _maleIndex = 0;

                    await SaveFriendAvatarAsync(File.OpenRead($"avatars/sberkot/{_maleIndex++}.jpg"), friendId);

                    if (_maleIndex >= max)
                        _maleIndex = 0;
                }
            }
        }

        private bool RemoveAvatar(string path)
        {;
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            else return false;
        }
        public bool RemoveUserAvatar(int userId) => RemoveAvatar(GetPathForUserAvatar(userId));
        public bool RemoveFriendAvatar(int friendId) => RemoveAvatar(GetPathForFriendAvatar(friendId));

        public FileStream GetDefaultAvatar()
        {
            return File.OpenRead(_defaultAvatarPath);
        }


    }
}
