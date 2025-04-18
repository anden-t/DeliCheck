namespace DeliCheck.Services
{
    public interface IImagePreprocessingService
    {
        Task<string> PreprocessImageAsync(Stream originalImage, int x1, int y1, int x2, int y2);
    }
}
