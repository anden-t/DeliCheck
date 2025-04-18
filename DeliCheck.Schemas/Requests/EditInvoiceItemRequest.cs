namespace DeliCheck.Schemas.Requests
{
    public class EditInvoiceItemRequest
    {
        public int Id { get; set; }
        public decimal Count { get; set; }
        public decimal Cost { get; set; }
        public string Name { get; set; }
    }
}
