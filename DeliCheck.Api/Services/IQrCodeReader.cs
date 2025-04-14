namespace DeliCheck.Services
{
    public interface IQrCodeReader
    {
        Task<string?> ReadQrCodeAsync(Stream image);
    }
}
