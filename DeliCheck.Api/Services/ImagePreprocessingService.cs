using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace DeliCheck.Services
{
    public class ImagePreprocessingService : IImagePreprocessingService
    {
        private const float _contrastAmount = 1.1f;

        private static int _imgIndex = 0;

        public async Task<string> PreprocessImageAsync(Stream originalImage, int x1, int y1, int x2, int y2)
        {
            Image image = Image.Load(originalImage);

            var x = x1;
            var y = y1;
            var width = x2 - x1;
            var height = y2 - y1;

            if (width == 0 || height == 0) 
            {
                x = 0; y = 0; width = image.Width; height = image.Height;
            }

            image.Mutate(i => i.Crop(new Rectangle() { X = x, Y = y, Width = width, Height = height }));

            string path = GetPath();
            await image.SaveAsPngAsync(path);
            return path;
        }

        private string GetPath()
        {
            Directory.CreateDirectory("InvoicesImages");
            Interlocked.Increment(ref _imgIndex);

            return Path.GetFullPath($"InvoicesImages/{_imgIndex}.png");
        }
    }
}
