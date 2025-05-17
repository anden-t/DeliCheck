using DeliCheck.Api.Utils;
using DeliCheck.Models;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DeliCheck.Services
{
    /// <summary>
    /// Провайдер авторизации для прямой авторизации
    /// </summary>
    public class AuthProvider : IAuthProvider
    {
        private readonly IAuthService _authService;
        private readonly IVkApi _vkApi;
        private readonly IAvatarService _avatarService;
        /// <summary>
        /// Создает провайдер авторизации для прямой авторизации
        /// </summary>
        /// <param name="authService">Сервис авторизации</param>
        /// <param name="vkApi">Сервис авторизации</param>
        public AuthProvider(IAuthService authService, IVkApi vkApi, IAvatarService avatarService)
        {
            _authService = authService;
            _vkApi = vkApi;
            _avatarService = avatarService;
        }

        /// <summary>
        /// Входит в аккаунт, создавая новый <see cref="SessionTokenModel"/>
        /// </summary>
        /// <param name="username">Юзернейм, <see cref="UserModel.Username"/></param>
        /// <param name="password">Пароль</param>
        /// <param name="device">Устройство входа</param>
        /// <returns>Токен сессии</returns>
        public async Task<SessionTokenModel?> LoginByUsernameAsync(string username, string password, string device) => await LoginByAsync(username, password, device, (x) => x.Username == username);
        /// <summary>
        /// Входит в аккаунт, создавая новый <see cref="SessionTokenModel"/>
        /// </summary>
        /// <param name="email">Эл. почта, <see cref="UserModel.Email"/></param>
        /// <param name="password">Пароль</param>
        /// <param name="device">Устройство входа</param>
        /// <returns>Токен сессии</returns>
        public async Task<SessionTokenModel?> LoginByEmailAsync(string email, string password, string device) => await LoginByAsync(email, password, device, (x) => x.Email == email);
        /// <summary>
        /// Входит в аккаунт, создавая новый <see cref="SessionTokenModel"/>
        /// </summary>
        /// <param name="phoneNumber">Номер телефона, <see cref="UserModel.PhoneNumber"/></param>
        /// <param name="password">Пароль</param>
        /// <param name="device">Устройство входа</param>
        /// <returns>Токен сессии</returns>
        public async Task<SessionTokenModel?> LoginByPhoneNumberAsync(string phoneNumber, string password, string device) => await LoginByAsync(phoneNumber, password, device, (x) => x.PhoneNumber == phoneNumber);

        private async Task<SessionTokenModel?> LoginByAsync(string login, string password, string device, Func<UserModel, bool> predicate)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                return null;

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(predicate);
                if (user == null) return null;

                var passwordHash = GetHash(password);
                if (user.PasswordHash != passwordHash) return null;

                var sessionToken = await _authService.CreateSessionTokenAsync(user.Id, device);
                return sessionToken;
            }
        }

        /// <summary>
        /// Регистрация пользователя
        /// </summary>
        /// <param name="username">Юзернейм</param>
        /// <param name="email">Электронная почта</param>
        /// <param name="phoneNumber">Номер телефона</param>
        /// <param name="password">Пароль</param>
        /// <param name="firstName">Имя</param>
        /// <param name="lastName">Фамилия</param>
        /// <returns></returns>
        public async Task<RegisterResult> RegisterAsync(string username, string email, string phoneNumber, string password, string firstName, string lastName, bool randomAvatar = true, long? vkId = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || password.Length < 6) return RegisterResult.InvalidData;

            using (var db = new DatabaseContext())
            {
                if (db.Users.Any(x =>
                    x.Username == username ||
                    !string.IsNullOrEmpty(email) && x.Email == email ||
                    !string.IsNullOrEmpty(phoneNumber) && x.PhoneNumber == phoneNumber)) return RegisterResult.UserExist;

                var user = new UserModel()
                {
                    Username = username,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Firstname = firstName,
                    Lastname = lastName,
                    PasswordHash = GetHash(password),
                    VkId = vkId,
                    HasAvatar = false
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();

                if (randomAvatar)
                {
                    user.HasAvatar = true;

                    await _avatarService.SetRandomUserAvatar(firstName, user.Id);
                    await db.SaveChangesAsync();
                }

                return new RegisterResult(user);
            }
        }

        public async Task<(VkAuthorizationData, SessionTokenModel, UserModel)> LoginVkAsync(string authorizationCode, string deviceId, string codeVerifier, string state, string device)
        {
            var data = await _vkApi.Login(authorizationCode, deviceId, codeVerifier, state);
            using (var db = new DatabaseContext())
            {
                SessionTokenModel token;
                var user = db.Users.FirstOrDefault(x => x.VkId == data.VkUserId);
                if (user != null)
                {
                    token = await _authService.CreateSessionTokenAsync(user.Id, device);
                }
                else
                {
                    var reg = await RegisterAsync(GetGuestUsername(), string.Empty, string.Empty, GetRandomPassword(), "firstname", "lastname", false, data.VkUserId);
                    user = reg.User;

                    if (user != null) token = await _authService.CreateSessionTokenAsync(user.Id, device);
                    else throw new InvalidOperationException("Не смог зарегистрировать пользователя");
                }

                data.UserId = user.Id;
                db.VkAuth.Add(data);
                await db.SaveChangesAsync();

                return (data, token, user);
            }
        }

        public async Task<VkAuthorizationData?> ConnectVkAsync(string authorizationCode, string deviceId, string codeVerifier, string state, int userId)
        {
            var data = await _vkApi.Login(authorizationCode, deviceId, codeVerifier, state);
            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == userId);
                if (user != null)
                {
                    user.VkId = data.VkUserId;
                    data.UserId = user.Id;
                    db.VkAuth.Add(data);
                    await db.SaveChangesAsync();
                    return data;
                }
                else return null;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string newPassword, string oldPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6 || newPassword.Length > 50)
                return false;

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == userId);
                if (user == null) return false;

                if (GetHash(oldPassword) != user.PasswordHash)
                    return false;

                user.PasswordHash = GetHash(newPassword);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public string GetGuestUsername()
        {
            using (var db = new DatabaseContext())
            {
                if (db.Users.Count() == 0)
                    return "guest_0";
                return $"guest_{db.Users.Select(x => x.Id).Max() + 1}";
            }
        }

        public string GetRandomPassword()
        {
            const int lenght = 10;
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string number = "1234567890";
            const string special = "!@#$%^&*_-=+";
            var bytes = RandomNumberGenerator.GetBytes(lenght);

            var res = new StringBuilder();
            foreach (byte b in bytes)
            {
                switch (Random.Shared.Next(4))
                {
                    case 0:
                        res.Append(lower[b % lower.Count()]);
                        break;
                    case 1:
                        res.Append(upper[b % upper.Count()]);
                        break;
                    case 2:
                        res.Append(number[b % number.Count()]);
                        break;
                    case 3:
                        res.Append(special[b % special.Count()]);
                        break;
                }
            }
            return res.ToString();
        }

        private string GetHash(string str) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(str)));
    }
}
