namespace DeliCheck.Schemas.Requests
{
    public class AddInvoiceItemRequest
    {
        public int InvoiceId { get; set; }
        public decimal Count { get; set; }
        public decimal Cost { get; set; }
        public string Name { get; set; }
    }
}
