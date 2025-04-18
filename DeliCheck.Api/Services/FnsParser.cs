using DeliCheck.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DeliCheck.Services
{
    public class FnsParser : IFnsParser
    {
        private string _key = "38s91f65nm";

        /// <summary>
        /// Получение ключа шифрования
        /// </summary>
        /// <returns></returns>
        public async Task UpdateKeyAsync()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "identity");
                var response = await httpClient.GetAsync("https://proverkacheka.com");
                var text = await response.Content.ReadAsStringAsync();
                var configVer = Regex.Match(text, @"<script type=""text/javascript"" src=/geng/_autobuild/version\.min\.js\?v=(.*?)></script>");
                var frontVer = Regex.Match(text, @"<script type=""text/javascript"" src=/scripts/all/js/front\.all\.min\.js\?v=(.*?)></script>");

                text = await (await httpClient.GetAsync($"https://proverkacheka.com/scripts/all/js/front.all.min.js?v={frontVer.Groups[1].Value}")).Content.ReadAsStringAsync();
                var b = Regex.Match(text, @"var a='(\w\w\w\w\w)';").Groups[1].Value;
                text = await (await httpClient.GetAsync($"https://proverkacheka.com/geng/_autobuild/version.min.js?v={configVer.Groups[1].Value}")).Content.ReadAsStringAsync();
                var a = Regex.Match(text, @"App\.cfg\.crypto='(\w\w\w\w\w)'").Groups[1].Value;
                _key = a + b;   
            }
        }

        public async Task<(InvoiceModel?, List<InvoiceItemModel>)> GetInvoiceModelAsync(string qr)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "identity");

                // Формирование token. Реверс-инжениринг https://proverkacheka.com/scripts/all/js/front.all.min.js
                int d;
                string b = qr + "3";
                for (d = 0; d < 1000; d++)
                {
                    var i = CreateMD5(b + d);
                    if (i.Split('0', StringSplitOptions.None).Length - 1 > 4) break;
                }

                var body = new StringContent($"{{ \"qrraw\": \"{qr}\", \"qr\": 3, \"token\": \"0.{d}\" }}", Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://proverkacheka.com/api/v1/check/get", body);
                // Зашифрованные данные алгоритмом AES-GCM
                var responseBody = await response.Content.ReadAsByteArrayAsync();

                // Дешифрование данных Реверс-инжениринг https://proverkacheka.com/scripts/common/lib/crypto.js
                byte[] tKey = SHA256.HashData(Encoding.UTF8.GetBytes(_key)).ToArray();
                byte[] iv = responseBody.Skip(responseBody.Length - 12).ToArray();
                byte[] tag = responseBody.Skip(responseBody.Length - 28).Take(16).ToArray();
                byte[] content = responseBody.Take(responseBody.Length - 28).ToArray();

                byte[] plainText = new byte[content.Length];
                using var aesGcm = new AesGcm(tKey, 16);
                aesGcm.Decrypt(iv, content, tag, plainText);

                var text = Encoding.UTF8.GetString(plainText);

                var json = JsonNode.Parse(text);

                var result = new InvoiceModel()
                {
                    Name = json["data"]["json"]["retailPlaceAddress"].GetValue<string>(),
                    FromFns = true,
                    OCRText = string.Empty,
                    TotalCost = json["data"]["json"]["totalSum"].GetValue<decimal>() / 100
                };

                var items = new List<InvoiceItemModel>();

                foreach (var item in json["data"]["json"]["items"].AsArray())
                {
                    items.Add(new InvoiceItemModel() 
                    {
                        Name = item["name"].GetValue<string>(),
                        Count = item["quantity"].GetValue<decimal>(),
                        Cost = item["sum"].GetValue<decimal>() / 100
                    });    
                }

                return (result, items);
            }
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes).ToLower(); // .NET 5 +
            }
        }
    }
}
