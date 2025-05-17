using DeliCheck.Models;
using DeliCheck.Schemas;
using DeliCheck.Services;
using DeliCheck.Utils;
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
        /// Паттерн для вида "Наименование | Количество | Сумма (000.00)". Поддерживает перенос части названия на строку ниже или выше. 
        /// </summary>
        //private const string _regexPattern1 = @"(.*?)\n?(.*?)\s+(\d*?[\.,]?\d*?)\s*?(?:порций)?(?:порц)?(?:пор)?(?:шт)?(?:мл)?(?:л)?(?:г)?(?:гр)?(?:кг)?\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$";
        //private const string _regexPattern1 = @"(.*?)\n?(.*?)\s+(\d*?[\.,]?\d*?)\s*?(?:порций)?(?:порц)?(?:пор)?(?:шт)?(?:мл)?(?:л)?(?:г)?(?:гр)?(?:кг)?\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d\n]*)(\1)?(?(8)\n([А-Яа-яa-zA-Z\d ]*)\n|$)";
        private const string _regexPattern1 = @"(.*?)(?:\r\n?|\n)?(.*?)[*^'""#`хХxX\s]+(\d*?[\.,]?\d*?)\s*?((?:(?:п|n)орций)?(?:(?:п|n)ор(?:ц|у))?(?:(?:п|n)(?:о|o)(?:р|p))?(?:(?:e|е)д)?(?:шт)?(?:мл)?(?:л)?(?:(?:г|r))?(?:(?:г|r)(?:р|p))?(?:к(?:r|г))?)[:.,\-=\s]+([-]?(\d+\s)?\d+\s?(\.|,)\s?\d\d)([^\d\n\r]*?)(\1)?(?(9)(?:\r\n?|\n)([А-Яа-яa-zA-Z\d ]*)(?:\r\n?|\n)|(?:\r\n?|\n))";
        /// <summary>
        /// Паттерн для вида "Наименование | Количество * Стоимость | Сумма (000.00)". Поддерживает перенос части названия на строку выше
        /// </summary>
        private const string _regexPattern2 = @"(.*?)(?:\r\n?|\n)?(.*?)\s+(\d+[\.,]?\d*)\s*?(?:(?:п|n)орций)?(?:(?:п|n)ор(?:ц|у))?(?:(?:п|n)(?:о|o)(?:р|p))?(?:(?:e|е)д)?(?:шт)?(?:мл)?(?:л)?(?:(?:г|r))?(?:(?:г|r)(?:р|p))?(?:к(?:r|г))?[*^'""#`хХxX ]+((\d+\s)?\d+(\.|,)\d\d)\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$";
        //private const string _regexPattern2 = @"(.*?)(?:\r\n?|\n)?(.*?)\s+(\d+[\.,]?\d*)\s*?(?:порций)?(?:порц)?(?:пор)?(?:шт)?(?:мл)?(?:л)?(?:г)?(?:гр)?(?:кг)?\s*?[*^'""#`хХxX]?\s*?((\d+\s)?\d+(\.|,)\d\d)\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$";
        /// <summary>
        /// Паттерн для вида "Количество * Наименование | Сумма (000.00)"
        /// </summary>
        private const string _regexPattern3 = @"^(\d+)\s?[*^'""#`хХxX]?(.*?)\s+([-=]?(\d+\s)?\d+(\.|,)\d\d)([^\d]*?)$";

        /// <summary>
        /// Паттерн для вида "Наименование | Сумма (000)"
        /// </summary>
        private const string _regexPattern4 = @"^(.*?)[\s]+(\d+)\s*?(?:ру(?:б|6)?)?(?:р)?\.?$";

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
            text += "\r\n";

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

            var itemsPattern1 = Regex.Matches(text, _regexPattern1, RegexOptions.Multiline | RegexOptions.IgnoreCase).Cast<Match>().Select(Pattern1Selector).Where(x => x != null).ToList();
            var itemsPattern2 = Regex.Matches(text, _regexPattern2, RegexOptions.Multiline | RegexOptions.IgnoreCase).Cast<Match>().Select(Pattern2Selector).Where(x => x != null).ToList();
            var itemsPattern3 = Regex.Matches(text, _regexPattern3, RegexOptions.Multiline | RegexOptions.IgnoreCase).Cast<Match>().Select(Pattern3Selector).Where(x => x != null).ToList();
            var itemsPattern4 = Regex.Matches(text, _regexPattern4, RegexOptions.Multiline | RegexOptions.IgnoreCase).Cast<Match>().Select(Pattern4Selector).Where(x => x != null).ToList();

            Console.WriteLine($"Pattern 1: {itemsPattern1.Count} matches");
            Console.WriteLine($"Pattern 2: {itemsPattern2.Count} matches");
            Console.WriteLine($"Pattern 3: {itemsPattern3.Count} matches");
            Console.WriteLine($"Pattern 4: {itemsPattern4.Count} matches");

            if (itemsPattern2.Count > 1 && !itemsPattern2.Any(x => x.Cost == 1)) items = itemsPattern2;
            else if (itemsPattern3.Count > 1) items = itemsPattern3;
            else if (itemsPattern1.Count > 1) items = itemsPattern1;
            else items = itemsPattern4;

                decimal itemsSum = itemsPattern1.Sum(x => x.Cost);
            totalCost = itemsSum;

            return (new InvoiceModel() { Name = "Чек", TotalCost = totalCost, OcrText = text, CreatedTime = DateTime.UtcNow }, items);
        }

        private static InvoiceItemModel? Pattern1Selector(Match x)
        {
            var name1 = x.Groups[1].Value.Trim();
            var name1Lower = name1.ToLower();
            var name2 = x.Groups[2].Value.Trim();
            var name2Lower = name2.ToLower();
            var name3 = x.Groups[10].Value.Trim();
            var name3Lower = name3.ToLower();
            var measure = x.Groups[4].Value.Trim();

            var countString = x.Groups[3].Value.Trim().Replace(" ", "").Replace(",", ".");
            var costString = x.Groups[5].Value.Trim().Replace(" ", "").Replace(",", ".").Replace("-", "");

            if (
                (!string.IsNullOrWhiteSpace(name1) ||
                !string.IsNullOrWhiteSpace(name2)) &&
                !_excludeItems.Any(name1Lower.Contains) &&
                !_excludeItems.Any(name2Lower.Contains) &&
                (!string.IsNullOrWhiteSpace(countString) ? decimal.TryParse(countString, CultureInfo.InvariantCulture, out decimal count) : (count = 1) == 1) &&
                decimal.TryParse(costString, CultureInfo.InvariantCulture, out decimal cost) && cost > 1
                )
            {
                if ((cost != 1 && count == cost) || count == 0) count = 1;
                if (cost % count == 0 && count * 5 >= cost) count = cost / count;

                var quantityMeasure = ItemQuantityMeasure.Piece;

                if (!string.IsNullOrWhiteSpace(measure))
                {
                    if (Regex.IsMatch(measure, "^к(r|г)$", RegexOptions.IgnoreCase)) quantityMeasure = ItemQuantityMeasure.Kilogram;
                    else if (Regex.IsMatch(measure, "^г|r$", RegexOptions.IgnoreCase) && count > 10) { quantityMeasure = ItemQuantityMeasure.Kilogram; count /= 1000; } 
                    else if (Regex.IsMatch(measure, "^г|r$", RegexOptions.IgnoreCase) && count <= 10) quantityMeasure = ItemQuantityMeasure.Gram; 
                    else if (Regex.IsMatch(measure, "^л$", RegexOptions.IgnoreCase) && count <= 10) quantityMeasure = ItemQuantityMeasure.Liter;
                    else if (Regex.IsMatch(measure, "^мл$", RegexOptions.IgnoreCase) && count > 10) { quantityMeasure = ItemQuantityMeasure.Liter; count /= 1000; }
                    else if (Regex.IsMatch(measure, "^мл$", RegexOptions.IgnoreCase) && count <= 10) quantityMeasure = ItemQuantityMeasure.Milliliter;
                }

                if (count > 30) count = 1;

                string name = "";
                if (string.IsNullOrWhiteSpace(name1))
                {
                    name = name2;

                    if (!string.IsNullOrWhiteSpace(name3) && !name3Lower.All(_excludeChars.Contains))
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

                    if (!string.IsNullOrWhiteSpace(name3) && !name3Lower.All(_excludeChars.Contains))
                        name += name3;
                }
                else
                {
                    name = name1;
                }

                name = Regex.Replace(name, @"^\d+(.*?)$", "$1").Trim();

                return new InvoiceItemModel()
                {
                    Cost = cost,
                    Quantity = count,
                    Name = name.AllToLower(),
                    QuantityMeasure = quantityMeasure
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
            var oneCostString = x.Groups[4].Value.Trim().Replace(" ", "").Replace(",", ".");
            var costString = x.Groups[7].Value.Trim().Replace(" ", "").Replace(",", ".").Replace("-", "");

            if (
                (!string.IsNullOrWhiteSpace(name1) ||
                !string.IsNullOrWhiteSpace(name2)) &&
                !_excludeItems.Any(name1Lower.Contains) &&
                !_excludeItems.Any(name2Lower.Contains) &&
                (!string.IsNullOrWhiteSpace(countString) ? decimal.TryParse(countString, CultureInfo.InvariantCulture, out decimal count) : (count = 1) == 1) &&
                decimal.TryParse(costString, CultureInfo.InvariantCulture, out decimal cost) && cost > 1 &&
                decimal.TryParse(oneCostString, CultureInfo.InvariantCulture, out decimal oneCost)
                )
            {
                if ((cost != 1 && count == cost) || count == 0) count = 1;
                if (cost > 30 && oneCost < 15) return null;

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
                    Quantity = count,
                    Name = name.AllToLower(),
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
                decimal.TryParse(costString, CultureInfo.InvariantCulture, out decimal cost) && cost > 1)
            {
                if ((cost != 1 && count == cost) || count == 0) count = 1;

                return new InvoiceItemModel()
                {
                    Cost = cost,
                    Quantity = count,
                    Name = name.AllToLower(),
                };
            }
            else return null;
        }

        private static InvoiceItemModel? Pattern4Selector(Match x)
        {
            var name = x.Groups[1].Value.Trim();
            var costString = x.Groups[2].Value.Trim().Replace(" ", "").Replace(",", ".").Replace("-", "");

            if (!string.IsNullOrWhiteSpace(name) && name.Count(char.IsLetter) > 2 &&
                !_excludeItems.Any(name.Contains) &&
                decimal.TryParse(costString, CultureInfo.InvariantCulture, out decimal cost) && cost > 1)
            {
                decimal quantity = 1;

                var potencialCounts = Regex.Matches(name, @"\d+(\.|\,)?\d*?").Cast<Match>().Where(x => decimal.TryParse(x.Value, out decimal a) && a > 0).OrderByDescending(x => decimal.Parse(x.Value)).ToList();

                foreach (var c in potencialCounts)
                {
                    var count = decimal.Parse(c.Value);
                    if(cost % count == 0 && count * 5 >= cost)
                    {
                        name = name.Substring(0, c.Index).Trim();
                        quantity = cost / count;
                        break;
                    }
                    else if (count < 10 && cost % count == 0 && count % 1 == 0)
                    {
                        name = name.Substring(0, c.Index).Trim();
                        quantity = count;
                        break;
                    }
                }

                return new InvoiceItemModel()
                {
                    Cost = cost,
                    Quantity = quantity,
                    Name = name.AllToLower(),
                };
            }
            else return null;
        }
    }
}
