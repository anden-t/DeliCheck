using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace DeliCheck.Services
{
    public class ImagePreprocessingService : IImagePreprocessingService
    {
        private const float _contrastAmount = 1.2f;
        public async Task<Stream> PreprocessImageAsync(Stream originalImage, int x1, int y1, int x2, int y2)
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

            image.Mutate(i => i.Crop(new Rectangle() { X = x, Y = y, Width = width, Height = height }).Contrast(_contrastAmount));
            var ms = new MemoryStream();
            await image.SaveAsPngAsync(ms);
            await image.SaveAsPngAsync("1.png");
            ms.Position = 0;
            return ms;
        }
    }
}
