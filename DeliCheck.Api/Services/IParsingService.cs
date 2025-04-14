using DeliCheck.Models;

namespace DeliCheck.Services
{
    public interface IParsingService
    {
        (InvoiceModel, List<InvoiceItemModel>) GetInvoiceModelFromText(string text);
    }
}
