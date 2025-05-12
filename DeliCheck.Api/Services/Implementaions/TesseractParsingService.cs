using DeliCheck.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DeliCheck.Services
{
    public class TesseractParsingService : IParsingService
    {
        public (InvoiceModel, List<InvoiceItemModel>) GetInvoiceModelFromText(string text)
        {
            if(text.Contains("\r"))
                text = text.Trim().Replace("=", "").Replace(")", "").Replace("(", "").Replace("\t", "    ").Replace(" пор ", "  ").Replace("\r\n\r\n", "\r\n");
            else 
                text = text.Trim().Replace("=", "").Replace(")", "").Replace("(", "").Replace("\t", "    ").Replace(" пор ", "  ").Replace("\n\n", "\n");

            Console.WriteLine(text);

            var totalCost = Regex.Matches(text, @"(.*?)\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$", RegexOptions.Multiline).Cast<Match>().Select(x => 
            {
                if (!string.IsNullOrWhiteSpace(x.Groups[1].Value) &&
                    (x.Groups[1].Value.ToLower().Contains("итог") ||
                    x.Groups[1].Value.ToLower().Contains("оплат") ||
                    x.Groups[1].Value.ToLower().Contains("всего")) &&
                    decimal.TryParse(x.Groups[2].Value.Trim().Replace(" ", "").Replace(",", ".").Replace("-", ""), CultureInfo.InvariantCulture, out decimal cost))
                    return cost;
                else return 0;
            }).LastOrDefault();

            //
            //(.*?)\s+(\d*?[\.,]?\d*?)\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$
            var items = Regex.Matches(text, @"(.*?)\n?(.*?)\s+(\d*?[\.,]?\d*?)\s*?(?:пор)?(?:шт)?(?:мл)?(?:л)?\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$", RegexOptions.Multiline).Cast<Match>().Select(x => 
            {
                if (
                    (!string.IsNullOrWhiteSpace(x.Groups[1].Value) ||
                    !string.IsNullOrWhiteSpace(x.Groups[2].Value)) &&
                    !x.Groups[1].Value.ToLower().Contains("итог") && 
                    !x.Groups[2].Value.ToLower().Contains("итог") && 
                    !x.Groups[1].Value.ToLower().Contains("оплат") && 
                    !x.Groups[2].Value.ToLower().Contains("оплат") && 
                    !x.Groups[1].Value.ToLower().Contains("всего") &&
                    !x.Groups[2].Value.ToLower().Contains("всего") &&
                    !x.Groups[1].Value.ToLower().Contains("наличны") &&
                    !x.Groups[2].Value.ToLower().Contains("наличны") &&
                    !x.Groups[1].Value.ToLower().Contains("безнал") &&
                    !x.Groups[2].Value.ToLower().Contains("безнал") &&
                    !x.Groups[1].Value.ToLower().Contains("сбербанк") &&
                    !x.Groups[2].Value.ToLower().Contains("сбербанк") &&
                    !x.Groups[1].Value.ToLower().Contains("рубли") &&
                    !x.Groups[2].Value.ToLower().Contains("рубли") &&
                    decimal.TryParse(x.Groups[3].Value.Trim().Replace(" ", "").Replace(",", "."), CultureInfo.InvariantCulture, out decimal count) &&
                    decimal.TryParse(x.Groups[4].Value.Trim().Replace(" ", "").Replace(",", ".").Replace("-", ""), CultureInfo.InvariantCulture, out decimal cost)
                    )
                {
                    if (cost != 1 && count == cost)
                        count = 1;

                    string name = "";
                    if (string.IsNullOrWhiteSpace(x.Groups[1].Value))
                    {
                        name = x.Groups[2].Value;
                    }
                    else if (!string.IsNullOrWhiteSpace(x.Groups[2].Value))
                    {
                        if (x.Groups[1].Value.ToLower().Contains(" счет") || x.Groups[1].Value.ToLower().Contains(" сумма"))
                            name = x.Groups[2].Value;
                        else if (x.Groups[2].Value.ToLower().All(x => "0123456789*xх-=#,. ".Contains(x)))
                            name = x.Groups[1].Value;
                        else
                            name = x.Groups[1].Value.Trim() + " " + x.Groups[2].Value.Trim();
                    }
                    else
                    {
                        name = x.Groups[1].Value;
                    }

                    name = Regex.Replace(name, @"^\d+(.*?)$", "$1").Trim();
                    name = name.Trim();

                    return new InvoiceItemModel()
                    {
                        Cost = cost,
                        Quantity = count,
                        Name = name,
                    };
                }
                else return null;
            }).Where(x => x != null).ToList();

            decimal itemsSum = items.Sum(x => x.Cost);

            if (totalCost == 0) totalCost = itemsSum;
            else if (totalCost > itemsSum) items.Add(new InvoiceItemModel()
            {
                Cost = totalCost - itemsSum,
                Quantity = 1,
                Name = "Позиция"
            });  

            return (new InvoiceModel() { Name = "Чек", TotalCost = totalCost, OCRText = text, CreatedTime = DateTime.UtcNow }, items);
        }
    }
}
