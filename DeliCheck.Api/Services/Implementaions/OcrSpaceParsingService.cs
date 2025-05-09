using DeliCheck.Models;
using DeliCheck.Services;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DeliCheck.Api.Services.Implementaions
{
    public class OcrSpaceParsingService : IParsingService
    {
        /// <summary>
        /// Паттерн для поиска итоговой суммы
        /// </summary>
        private const string _regexPatternTotal = @"(.*?)\s+([-=]?(\d+\s+)\d+(\.|,)\d\d)([^\d]*?)$";

        /// <summary>
        /// Паттерн для вида "Наименование | Количество | Сумма". Поддерживает перенос части названия на строку ниже или выше.
        /// </summary>
        //private const string _regexPattern1 = @"(.*?)\n?(.*?)\s+(\d*?[\.,]?\d*?)\s*?(?:порций)?(?:порц)?(?:пор)?(?:шт)?(?:мл)?(?:л)?(?:г)?(?:гр)?(?:кг)?\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$";
        private const string _regexPattern1 = @"(.*?)\n?(.*?)\s+(\d*?[\.,]?\d*?)\s*?(?:порций)?(?:порц)?(?:пор)?(?:шт)?(?:мл)?(?:л)?(?:г)?(?:гр)?(?:кг)?\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d\n]*)(\1)?(?(8)\n([А-Яа-яa-zA-Z\d ]*)\n|$)";
        /// <summary>
        /// Паттерн для вида "Наименование | Количество * Стоимость | Сумма". Поддерживает перенос части названия на строку выше
        /// </summary>
        private const string _regexPattern2 = @"(.*?)\n?(.*?)\s+(\d+[\.,]?\d*)\s*?(?:порций)?(?:порц)?(?:пор)?(?:шт)?(?:мл)?(?:л)?(?:г)?(?:гр)?(?:кг)?\s*?[*^'""#`хХxX]?\s*?((\d+\s)?\d+(\.|,)\d\d)\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$";
        /// <summary>
        /// Паттерн для вида "Количество * Наименование | Сумма"
        /// </summary>
        private const string _regexPattern3 = @"^(\d+)\s?[*^'""#`хХxX]?(.*?)\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$";

        private static readonly string[] _excludeItems =
        [
            "итог",
            "оплат",
            "всего",
            "наличны",
            "безнал",
            "сбербанк",
            "сумма"
        ];
        private const string _excludeChars = "0123456789*xх-=#,. ";
        private static readonly string[] _excludeInFirstItem = 
        {
            " счет",
            "сумма",
            "кол-во"
        };


        public (InvoiceModel, List<InvoiceItemModel>) GetInvoiceModelFromText(string text)
        {
            text = text.Trim().Replace("=", "").Replace(")", "").Replace("(", "").Replace("\t", "    ").Replace(" пор ", "  ");
            text = Regex.Replace(text, @"НДС \d+%", " ");

            if (text.Contains("\r"))
                text = text.Replace("\r\n\r\n", "\r\n");
            else
                text = text.Replace("\n\n", "\n");

            Console.WriteLine(text);

            var totalCost = Regex.Matches(text, _regexPatternTotal, RegexOptions.Multiline).Cast<Match>().Select(x =>
            {
                if (!string.IsNullOrWhiteSpace(x.Groups[1].Value) &&
                    (x.Groups[1].Value.ToLower().Contains("итог") ||
                    x.Groups[1].Value.ToLower().Contains("оплат") ||
                    x.Groups[1].Value.ToLower().Contains("всего")) &&
                    decimal.TryParse(x.Groups[2].Value.Trim().Replace(" ", "").Replace(",", ".").Replace("-", ""), CultureInfo.InvariantCulture, out decimal cost))
                    return cost;
                else return 0;
            }).LastOrDefault();

            List<InvoiceItemModel> items;
            var itemsPattern1 = Regex.Matches(text, _regexPattern1, RegexOptions.Multiline).Cast<Match>().Select(Pattern1Selector).Where(x => x != null).ToList();
            var itemsPattern2 = Regex.Matches(text, _regexPattern2, RegexOptions.Multiline).Cast<Match>().Select(Pattern2Selector).Where(x => x != null).ToList();
            var itemsPattern3 = Regex.Matches(text, _regexPattern3, RegexOptions.Multiline).Cast<Match>().Select(Pattern3Selector).Where(x => x != null).ToList();

            if (itemsPattern2.Count > 1) items = itemsPattern2;
            else if (itemsPattern3.Count > 1) items = itemsPattern3;
            else items = itemsPattern1;

            decimal itemsSum = itemsPattern1.Sum(x => x.Cost);
            totalCost = itemsSum;

            return (new InvoiceModel() { Name = "Чек", TotalCost = totalCost, OCRText = text, CreatedTime = DateTime.UtcNow }, items);
        }

        private static InvoiceItemModel? Pattern1Selector(Match x)
        {
            var name1 = x.Groups[1].Value.Trim();
            var name1Lower = name1.ToLower();
            var name2 = x.Groups[2].Value.Trim();
            var name2Lower = name2.ToLower();
            var name3 = x.Groups[9].Value.Trim();
            var name3Lower = name3.ToLower();

            var countString = x.Groups[3].Value.Trim().Replace(" ", "").Replace(",", ".");
            var costString = x.Groups[4].Value.Trim().Replace(" ", "").Replace(",", ".").Replace("-", "");

            if (
                (!string.IsNullOrWhiteSpace(name1) ||
                !string.IsNullOrWhiteSpace(name2)) &&
                !_excludeItems.Any(name1Lower.Contains) &&
                !_excludeItems.Any(name2Lower.Contains) &&
                (!string.IsNullOrWhiteSpace(countString) ? decimal.TryParse(countString, CultureInfo.InvariantCulture, out decimal count) : (count = 1) == 1) &&
                decimal.TryParse(costString, CultureInfo.InvariantCulture, out decimal cost)
                )
            {
                if ((cost != 1 && count == cost) || count == 0) count = 1;

                string name = "";
                if (string.IsNullOrWhiteSpace(name1))
                {
                    name = name2;
                    if (!string.IsNullOrWhiteSpace(name3))
                        name += name3;
                }
                else if (!string.IsNullOrWhiteSpace(name2))
                {
                    if (_excludeInFirstItem.Any(name1.Contains))
                        name = name2;
                    else if (name2Lower.All(_excludeChars.Contains))
                        name = name1;
                    else
                        name = name1 + " " + name2;
                }
                else
                {
                    name = name1;
                }

                name = Regex.Replace(name, @"^\d+(.*?)$", "$1").Trim();

                return new InvoiceItemModel()
                {
                    Cost = cost,
                    Count = count,
                    Name = name,
                };
            }
            else return null;
        }

        private static InvoiceItemModel? Pattern2Selector(Match x)
        {
            var name1 = x.Groups[1].Value.Trim();
            var name1Lower = name1.ToLower();
            var name2 = x.Groups[2].Value.Trim();
            var name2Lower = name2.ToLower();

            var countString = x.Groups[3].Value.Trim().Replace(" ", "").Replace(",", ".");
            var costString = x.Groups[7].Value.Trim().Replace(" ", "").Replace(",", ".").Replace("-", "");

            if (
                (!string.IsNullOrWhiteSpace(name1) ||
                !string.IsNullOrWhiteSpace(name2)) &&
                !_excludeItems.Any(name1Lower.Contains) &&
                !_excludeItems.Any(name2Lower.Contains) &&
                (!string.IsNullOrWhiteSpace(countString) ? decimal.TryParse(countString, CultureInfo.InvariantCulture, out decimal count) : (count = 1) == 1) &&
                decimal.TryParse(costString, CultureInfo.InvariantCulture, out decimal cost)
                )
            {
                if ((cost != 1 && count == cost) || count == 0) count = 1;

                string name = "";
                if (string.IsNullOrWhiteSpace(name1))
                {
                    name = name2;
                }
                else if (!string.IsNullOrWhiteSpace(name2))
                {
                    if (_excludeInFirstItem.Any(name1.Contains))
                        name = name2;
                    else if (name2Lower.All(_excludeChars.Contains))
                        name = name1;
                    else
                        name = name1 + " " + name2;
                }
                else
                {
                    name = name1;
                }

                name = Regex.Replace(name, @"^\d+(.*?)$", "$1").Trim();

                return new InvoiceItemModel()
                {
                    Cost = cost,
                    Count = count,
                    Name = name,
                };
            }
            else return null;
        }

        private static InvoiceItemModel? Pattern3Selector(Match x)
        {
            var name = x.Groups[3].Value.Trim();

            var countString = x.Groups[1].Value.Trim().Replace(" ", "").Replace(",", ".");
            var costString = x.Groups[4].Value.Trim().Replace(" ", "").Replace(",", ".").Replace("-", "");

            if (!string.IsNullOrWhiteSpace(name) && name.Count(x => char.IsLetter(x)) > 2 &&
                (!string.IsNullOrWhiteSpace(countString) ? decimal.TryParse(countString, CultureInfo.InvariantCulture, out decimal count) : (count = 1) == 1) &&
                decimal.TryParse(costString, CultureInfo.InvariantCulture, out decimal cost))
            {
                if ((cost != 1 && count == cost) || count == 0) count = 1;

                return new InvoiceItemModel()
                {
                    Cost = cost,
                    Count = count,
                    Name = name,
                };
            }
            else return null;
        }
    }
}
