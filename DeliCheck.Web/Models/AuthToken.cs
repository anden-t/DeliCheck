using System.Text.Json.Serialization;

namespace DeliCheck.Web.Models
{
    public class AuthToken
    {
        public AuthToken(string token, int expiresIn)
        {
            Token = token;
            ExpiresIn = expiresIn;
            LastRefreshUTC = DateTime.UtcNow;
        }

        public DateTime LastRefreshUTC { get; }

        public string Token { get; }

        public int ExpiresIn { get; }

        [JsonIgnore]
        public bool IsValid => LastRefreshUTC + TimeSpan.FromHours(ExpiresIn) >= DateTime.UtcNow;
    }
}
