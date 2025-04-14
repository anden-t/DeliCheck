using DeliCheck.Models;
using System.Security.Cryptography;
using System.Text;

namespace DeliCheck.Services
{
    public class AuthService : IAuthService
    {
        public int DefaultExpiresIn => 10000;

        public async Task<SessionTokenModel> CreateSessionTokenAsync(int userId, string device)
        {
            using (var db = new DatabaseContext())
            {
                if (!db.Users.Any(x => x.Id == userId))
                    throw new InvalidOperationException("CreateSessionTokenAsync: Пользователя с таким id не существует");

                var token = new SessionTokenModel()
                {
                    CreateTime = DateTime.UtcNow, 
                    Device = device, 
                    Disabled = false, 
                    ExpiresInHours = DefaultExpiresIn, 
                    SessionToken = $"{userId}.{GenerateToken()}"
                };

                db.SessionTokens.Add(token);
                await db.SaveChangesAsync();

                return token;
            }
        }

        public async Task<bool> RefreshSessionTokenAsync(SessionTokenModel token)
        {
            using (var db = new DatabaseContext())
            {
                var findedToken = db.SessionTokens.FirstOrDefault(x => x.Id == token.Id && x.SessionToken == token.SessionToken);
                if (findedToken == null || !findedToken.IsActive)
                    return false;

                findedToken.CreateTime = DateTime.UtcNow;
                await db.SaveChangesAsync();

                return true;
            }
        }

        public SessionTokenModel? GetSessionTokenByString(string token)
        {
            using (var db = new DatabaseContext())
            {
                var t = db.SessionTokens.FirstOrDefault(x => x.SessionToken == token);
                if (t == null || !t.IsActive)
                    return null;
                return t;
            }
        }

        public UserModel? GetUserByToken(SessionTokenModel token)
        {
            using (var db = new DatabaseContext())
            {
                var t = db.SessionTokens.FirstOrDefault(x => x.Id == token.Id && x.SessionToken == token.SessionToken);
                if (t == null || !t.IsActive)
                    return null;

                var userId = t.UserId;
                var user = db.Users.FirstOrDefault(x => x.Id == userId);
                return user;
            }
        }

        public UserModel? GetUserByStringToken(string token)
        {
            var t = GetSessionTokenByString(token);
            if (t != null)
                return GetUserByToken(t);
            else
                return null;
        }

        public async Task DisableTokenAsync(SessionTokenModel token)
        {
            using (var db = new DatabaseContext())
            {
                db.Remove(token);
                await db.SaveChangesAsync();
            }
        }

        private string GenerateToken(int lenght = 32)
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string number = "1234567890";
            var bytes = RandomNumberGenerator.GetBytes(lenght);

            var res = new StringBuilder();
            foreach (byte b in bytes)
            {
                switch (Random.Shared.Next(3))
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
                }
            }
            return res.ToString();
        }

 
    }
}
