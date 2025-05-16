using DeliCheck.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Web;
using System.Net.Sockets;
using System.Net;
using System.Reflection;

namespace DeliCheck.Services
{
    public class VkApi : IVkApi
    {
        public readonly string AppId;
        public readonly string RedirectUri;
        public readonly string RedirectConnectUri;
        public readonly string Scope;

        private readonly IAvatarService _avatarService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VkApi> _logger;

        public VkApi(IAvatarService avatarService, IConfiguration configuration, ILogger<VkApi> logger)
        {
            _logger = logger;
            _avatarService = avatarService;
            _configuration = configuration;
            AppId = _configuration["VkAppId"];
            RedirectUri = _configuration["VkRedirectUri"];
            RedirectConnectUri = _configuration["VkRedirectConnectUri"];
            Scope = _configuration["VkScope"];
        }
        public string GetCodeChallenge(string codeVerifier) => Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier))).Split('=')[0].Replace('+', '-').Replace('/', '_');
        public string GetCodeVerifier()
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string number = "1234567890";
            var bytes = RandomNumberGenerator.GetBytes(100);

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

        public string GetState() => Guid.NewGuid().ToString();
        public string GetVkAuthUrl(string codeChallenge, string state, string returnUrl) => $"https://id.vk.com/authorize?response_type=code&client_id={HttpUtility.UrlEncode(AppId)}&state={HttpUtility.UrlEncode(state)}&code_challenge={codeChallenge}&code_challenge_method=S256&scope={HttpUtility.UrlEncode(Scope)}&redirect_uri={RedirectUri + $"?return_to={HttpUtility.UrlEncode(returnUrl)}"}";
        public string GetVkAuthUrl(string codeChallenge, string state) => $"https://id.vk.com/authorize?response_type=code&client_id={HttpUtility.UrlEncode(AppId)}&state={HttpUtility.UrlEncode(state)}&code_challenge={codeChallenge}&code_challenge_method=S256&scope={HttpUtility.UrlEncode(Scope)}&redirect_uri={HttpUtility.UrlEncode(RedirectUri)}";
        public string GetConnectVkAuthUrl(string codeChallenge, string state, string returnUrl) => $"https://id.vk.com/authorize?response_type=code&client_id={HttpUtility.UrlEncode(AppId)}&state={HttpUtility.UrlEncode(state)}&code_challenge={codeChallenge}&code_challenge_method=S256&scope={HttpUtility.UrlEncode(Scope)}&redirect_uri={RedirectConnectUri + $"?return_to={HttpUtility.UrlEncode(returnUrl)}"}";
        public string GetConnectVkAuthUrl(string codeChallenge, string state) => $"https://id.vk.com/authorize?response_type=code&client_id={HttpUtility.UrlEncode(AppId)}&state={HttpUtility.UrlEncode(state)}&code_challenge={codeChallenge}&code_challenge_method=S256&scope={HttpUtility.UrlEncode(Scope)}&redirect_uri={HttpUtility.UrlEncode(RedirectConnectUri)}";

        public async Task<VkAuthorizationData> Login(string authorizationCode, string deviceId, string codeVerifier, string state)
        {
            using var client = new HttpClient(GetHttpHandler());
            
            var response = await client.PostAsync("https://id.vk.com/oauth2/auth", new StringContent(
                $"grant_type=authorization_code&" +
                $"code_verifier={codeVerifier}&" +
                $"state={state}&" +
                $"device_id={deviceId}&" +
                $"client_id={AppId}&" +
                $"code={authorizationCode}&" +
                $"redirect_uri={RedirectUri}",
                Encoding.UTF8, "application/x-www-form-urlencoded"));

            var text = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(text);
            if (json?["error"] == null)
            {
                var data = JsonSerializer.Deserialize<VkAuthorizationData>(text);
                if (data != null)
                {
                    data.CreateTime = DateTime.UtcNow;
                    data.DeviceId = deviceId;

                    return data;
                }
                else throw new InvalidOperationException($"Авторизация ВК не прошла успешно: data is null.");
            }
            else throw new InvalidOperationException($"Авторизация ВК не прошла успешно: {json["error"]} - {json["error_description"]}.");
        }

        public async Task<VkAuthorizationData> RefreshTokenAsync(VkAuthorizationData authData)
        {
            if ((DateTime.UtcNow - authData.CreateTime).TotalSeconds < authData.ExpiresIn) return authData;

            using var client = new HttpClient(GetHttpHandler());
            var response = await client.PostAsync("https://id.vk.com/oauth2/auth", new StringContent(
                $"grant_type=refresh_token&" +
                $"refresh_token={authData.RefreshToken}&" +
                $"state={authData.State}&" +
                $"device_id={authData.DeviceId}&" +
                $"client_id={AppId}&" +
                $"scope={Scope}",
                Encoding.UTF8, "application/x-www-form-urlencoded"));
            var text = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(text);
            if (json?["error"] == null)
            {
                var data = JsonSerializer.Deserialize<VkAuthorizationData>(text);
                if (data != null)
                {
                    data.CreateTime = DateTime.UtcNow;
                    data.DeviceId = authData.DeviceId;
                    data.UserId = authData.UserId;
                    
                    using (var db = new DatabaseContext())
                    {
                        db.VkAuth.Add(data);
                        await db.SaveChangesAsync();
                    }

                    return data;
                }
                else throw new InvalidOperationException($"Авторизация ВК не прошла успешно: data is null.");
            }
            else throw new InvalidOperationException($"Авторизация ВК не прошла успешно: {json["error"]} - {json["error_description"]}.");
        }

        public async Task UpdateFriendsListAsync(VkAuthorizationData authData)
        {
            authData = await RefreshTokenAsync(authData);

            using var client = new HttpClient(GetHttpHandler());
            var response = await client.GetAsync($"https://api.vk.com/method/friends.get?access_token={authData.AccessToken}&order=name&count=1000&fields=first_name,last_name,photo_200_orig&user_id={authData.VkUserId}&v=5.199");
            var text = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(text);

            var items = json?["response"]?["items"]?.AsArray();
            if(items != null)
            {
                using (var db = new DatabaseContext())
                {
                    foreach (var item in items)
                    {
                        try
                        {
                            var vkId = item?["id"]?.GetValue<long>();
                            var user = db.Users.FirstOrDefault(x => x.VkId == vkId);
                            var offlineFriend = db.OfflineFriends.FirstOrDefault(x => x.VkId == vkId);

                            if (user != null)
                            {
                                if (offlineFriend != null)
                                {
                                    db.OfflineFriends.Remove(offlineFriend);
                                }

                                var friend = db.Friends.FirstOrDefault(x => x.OwnerId == authData.UserId && x.FriendId == user.Id);

                                if (friend == null)
                                    db.Friends.Add(new FriendModel()
                                    {
                                        FriendId = user.Id,
                                        OwnerId = authData.UserId
                                    });
                            }
                            else
                            {
                                if (offlineFriend == null)
                                {
                                    var avatarUrl = item?["photo_200_orig"]?.GetValue<string>();
                                    var newFriend = new OfflineFriendModel()
                                    {
                                        Firstname = item?["first_name"]?.GetValue<string>() ?? string.Empty,
                                        Lastname = item?["last_name"]?.GetValue<string>() ?? string.Empty,
                                        VkId = vkId,
                                        OwnerId = authData.UserId,
                                        HasAvatar = avatarUrl != null
                                    };
                                    db.OfflineFriends.Add(newFriend);
                                    await db.SaveChangesAsync();
                                    if (avatarUrl != null)
                                    {
                                        using var stream = new MemoryStream(await client.GetByteArrayAsync(avatarUrl));
                                        await _avatarService.SaveFriendAvatarAsync(stream, newFriend.Id);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Не удалось добавить друга: {ex.GetType().Name} {ex.Message} {ex.InnerException?.GetType()?.Name ?? ""} {ex.InnerException?.Message ?? ""}");
                        }

                    }
                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task UpdateUserInfoAsync(VkAuthorizationData authData, bool setAvatar = true)
        {
            authData = await RefreshTokenAsync(authData);

            using var client = new HttpClient(GetHttpHandler());
            var response = await client.PostAsync("https://id.vk.com/oauth2/user_info", new StringContent(
                $"client_id={AppId}&" +
                $"access_token={authData.AccessToken}",
                Encoding.UTF8, "application/x-www-form-urlencoded"));
            var text = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(text);

            var firstname = json["user"]["first_name"].GetValue<string>();
            var lastname = json["user"]["last_name"].GetValue<string>();
            var avatar = json["user"]["avatar"].GetValue<string>();

            using (var db = new DatabaseContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Id == authData.UserId);
                if (user == null) return;

                user.Firstname = firstname;
                user.Lastname = lastname;

                if (setAvatar)
                {
                    response = await client.GetAsync(avatar);
                    await _avatarService.SaveUserAvatarAsync(await response.Content.ReadAsStreamAsync(), user.Id);
                    user.HasAvatar = true;
                }

                await db.SaveChangesAsync();
            }
        }

        private HttpClientHandler GetHttpHandler()
        {
            var handler = new HttpClientHandler();
            var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
            socketsHandler.ConnectCallback = async (context, token) =>
            {
                var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Bind(new IPEndPoint(IPAddress.Parse(_configuration["BindIP"]), 0));
                await s.ConnectAsync(context.DnsEndPoint, token);
                s.NoDelay = true;
                return new NetworkStream(s, ownsSocket: true);
            };

            return handler;
        }

        protected static object GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            var field = typeof(HttpClientHandler).GetField("_underlyingHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(handler);
        }
    }
}
