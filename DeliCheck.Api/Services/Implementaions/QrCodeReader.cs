
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.RegularExpressions;
using ZXing;

namespace DeliCheck.Services
{
    public class QrCodeReader : IQrCodeReader
    {
        public async Task<string?> ReadQrCodeAsync(Stream imageStream)
        {
            var image = await Image.LoadAsync<Rgba32>(imageStream);
            
            var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>();
            var result = reader.DecodeMultiple<Rgba32>(image);
            
            if(result != null)
                foreach ( var item in result )
                    if(item != null)
                        if (item.BarcodeFormat == BarcodeFormat.QR_CODE && !string.IsNullOrWhiteSpace(item.Text) && Regex.IsMatch(item.Text, @"fn=\d+"))
                            return item.Text;
            return null;
        }
    }
}
