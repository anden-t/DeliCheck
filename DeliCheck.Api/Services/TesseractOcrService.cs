
using System.Diagnostics;
using System.Text;

namespace DeliCheck.Services
{
    /// <summary>
    /// Wrapper для Tesseract
    /// </summary>
    public class TesseractOcrService : IOcrService
    {
        private readonly IConfiguration _configuration;
        public TesseractOcrService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Получить текст из изображения
        /// </summary>
        /// <param name="imagePath">Путь к изображению</param>
        /// <returns></returns>
        public async Task<string?> GetTextFromImageAsync(string imagePath)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                WorkingDirectory = Path.GetDirectoryName(_configuration["TesseractPath"]),
                FileName = _configuration["TesseractPath"],
                Arguments = $"\"{imagePath}\" - -l rus --psm 6",
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process.Start();
            process.WaitForExit();
            return process.StandardOutput.ReadToEnd();
        }
    }
}
