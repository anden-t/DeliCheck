
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DeliCheck.Services
{
    /// <summary>
    /// Сервис для OCR распознования https://products.aspose.ai/total/ru/image-to-text/
    /// </summary>
    public class AsposeOcrService : IOcrService
    {
        /// <summary>
        /// Получить текст из изображения
        /// </summary>
        /// <param name="image">Изображение</param>
        /// <returns></returns>
        public async Task<string?> GetTextFromImageAsync(Stream image)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "identity");

                var response = await httpClient.GetAsync("https://products.aspose.ai/total/ru/image-to-text/");
                var text = await response.Content.ReadAsStringAsync();
                var verifyToken = Regex.Match(text, @"name=""__RequestVerificationToken"" type=""hidden"" value=""(.*?)""").Groups[1].Value;

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "identity");

                var imageBody = new StreamContent(image);
                imageBody.Headers.Add("Content-Type", "image/png");

                response = await httpClient.PostAsync("https://api.aspose.ai/total/image-to-text/Home/UploadDocument", new MultipartFormDataContent() { { new StringContent(verifyToken), "__RequestVerificationToken" }, { imageBody, "UploadedFile", "photo.png" } });
                text = await response.Content.ReadAsStringAsync();
                var json = JsonNode.Parse(text);
                if (json?["success"]?.GetValue<bool>() ?? false)
                {
                    var parsed = json?["text"]?.GetValue<string>();
                    return parsed;
                }
                else return null;
            }
        }
    }
}
