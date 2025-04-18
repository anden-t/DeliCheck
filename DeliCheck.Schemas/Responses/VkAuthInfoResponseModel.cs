namespace DeliCheck.Schemas.Responses
{
    public class VkAuthInfoResponseModel
    {
        public int AppId { get; set; }
        public string Scope { get; set; }
        public string CodeChallenge { get; set; }
        public string State { get; set; }
        public string RedirectUrl { get; set; }
    }
}
