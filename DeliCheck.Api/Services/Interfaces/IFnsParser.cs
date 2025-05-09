using DeliCheck.Models;

namespace DeliCheck.Services
{
    public interface IFnsParser
    {
        Task<(InvoiceModel?, List<InvoiceItemModel>)> GetInvoiceModelAsync(string qr);
        Task UpdateKeyAsync();
    }
}
