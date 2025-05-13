namespace DeliCheck.Api.Models
{
    public class OcrResult
    {
        public bool Completed => !string.IsNullOrWhiteSpace(Text);
        public string OcrEngine { get; set; }
        public string? Text { get; set; }
    }
}
