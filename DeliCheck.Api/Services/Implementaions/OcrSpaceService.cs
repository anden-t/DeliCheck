using System.Text.Json.Nodes;

namespace DeliCheck.Services
{
    /// <summary>
    /// OCR сервис https://ocr.space
    /// </summary>
    public class OcrSpaceService : IOcrService
    {
        private string _apiKey = "donotstealthiskey_ip1";
        public async Task<string?> GetTextFromImageAsync(string imagePath)
        {
            using (var httpClient = new HttpClient())
            {
                if (new FileInfo(imagePath).Length > 5 * 1024 * 1024) return null; 

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "identity");
                httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
                httpClient.DefaultRequestHeaders.Add("Origin", "https://ocr.space");
                httpClient.DefaultRequestHeaders.Add("Referer", "https://ocr.space/");

                var content = new MultipartFormDataContent
                {
                    { new ByteArrayContent(File.ReadAllBytes(imagePath)) { }, "file", "image.jpg" },
                    { new StringContent("rus"), "language" },
                    { new StringContent("true"), "isOverlayRequired" },
                    { new StringContent(".Auto"), "FileType" },
                    { new StringContent("false"), "IsCreateSearchablePDF" },
                    { new StringContent("true"), "isSearchablePdfHideTextLayer" },
                    { new StringContent("true"), "detectOrientation" },
                    { new StringContent("true"), "isTable" },
                    { new StringContent("true"), "scale" },
                    { new StringContent("5"), "OCREngine" },
                    { new StringContent("false"), "detectCheckbox" },
                    { new StringContent("0"), "checkboxTemplate" }
                };

                var response = await httpClient.PostAsync("https://api8.ocr.space/parse/image", content);
                var text = await response.Content.ReadAsStringAsync();
                var json = JsonNode.Parse(text);

                if (json?["ParsedResults"]?[0]?["FileParseExitCode"]?.GetValue<int?>() == 1)
                {
                    return json["ParsedResults"]?[0]?["ParsedText"]?.GetValue<string?>();
                }
                else return null;
            }
        }
    }
}
