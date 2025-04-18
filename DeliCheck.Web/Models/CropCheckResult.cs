namespace DeliCheck.Web.Models
{
    public class CropCheckResult
    {
        public byte[] ImageData { get; set; }
        
        public int X1 { get; set; }
        public int X2 { get; set; }
        public int Y1 { get; set; }
        public int Y2 { get; set; }
    }
}
