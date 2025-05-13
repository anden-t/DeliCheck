namespace DeliCheck.Schemas
{
    /// <summary>
    /// Тип количества позиции (отображаемый, не влияет на логику)
    /// </summary>
    public enum ItemQuantityMeasure : int
    {
        /// <summary>
        /// Штуки, порции (по умолчанию)
        /// </summary>
        Piece = 0,
        /// <summary>
        /// Грамм
        /// </summary>
        Gram = 10,
        /// <summary>
        /// Килограмм
        /// </summary>
        Kilogram = 11,
        /// <summary>
        /// Тонна
        /// </summary>
        Tons = 12,
        /// <summary>
        /// Сантиметр
        /// </summary>
        Centimeter = 20,
        /// <summary>
        /// Дециметр
        /// </summary>
        Decimeter = 21,
        /// <summary>
        /// Метр
        /// </summary>
        Meter = 22,
        /// <summary>
        /// Квадратный сантиметр
        /// </summary>
        SquareCentimeter = 30,
        /// <summary>
        /// Квадратный дециметр
        /// </summary>
        SquareDecimeter = 31,
        /// <summary>
        /// Квадратный метр
        /// </summary>
        SquareMeter = 32,
        /// <summary>
        /// Миллилитр
        /// </summary>
        Milliliter = 40,
        /// <summary>
        /// Литр
        /// </summary>
        Liter = 41,
        /// <summary>
        /// Кубический метр
        /// </summary>
        CubicMeter = 42,
        /// <summary>
        /// Киловатт/час
        /// </summary>
        KilowattHour = 50,
        /// <summary>
        /// Гигакалория
        /// </summary>
        Gigacalorie = 51,
        /// <summary>
        /// Сутки (день)
        /// </summary>
        Day = 70,
        /// <summary>
        /// Час
        /// </summary>
        Hour = 71,
        /// <summary>
        /// Минута
        /// </summary>
        Minute = 72,
        /// <summary>
        /// Секунда
        /// </summary>
        Second = 73,
        /// <summary>
        /// Килобайт
        /// </summary>
        Kilobyte = 80,
        /// <summary>
        /// Мегабайт
        /// </summary>
        Megabyte = 81,
        /// <summary>
        /// Гигабайт
        /// </summary>
        Gigabyte = 82,
        /// <summary>
        /// Терабайт
        /// </summary>
        Terabyte = 83
    }

    public static class ItemQuantityMeasureExtension
    {
        /// <summary>
        /// Возвращает строку меру количества в сокращенном виде (шт, кг, м и т.д.)
        /// </summary>
        /// <param name="measure">Мера количества</param>
        /// <returns></returns>
        public static string ToShortString(this ItemQuantityMeasure measure)
        {
            switch (measure)
            {
                case ItemQuantityMeasure.Piece: return "ед";
                //case ItemQuantityMeasure.Piece: return "порц";
                case ItemQuantityMeasure.Gram: return "гр";
                case ItemQuantityMeasure.Kilogram: return "кг";
                case ItemQuantityMeasure.Tons: return "т";
                case ItemQuantityMeasure.Centimeter: return "см";
                case ItemQuantityMeasure.Decimeter: return "дм";
                case ItemQuantityMeasure.Meter: return "м";
                case ItemQuantityMeasure.SquareCentimeter: return "см2";
                case ItemQuantityMeasure.SquareDecimeter: return "дм2";
                case ItemQuantityMeasure.SquareMeter: return "м2";
                case ItemQuantityMeasure.Milliliter: return "мл";
                case ItemQuantityMeasure.Liter: return "л";
                case ItemQuantityMeasure.CubicMeter: return "м3";
                case ItemQuantityMeasure.KilowattHour: return "кв/ч";
                case ItemQuantityMeasure.Gigacalorie: return "гк";
                case ItemQuantityMeasure.Day: return "д";
                case ItemQuantityMeasure.Hour: return "ч";
                case ItemQuantityMeasure.Minute: return "мин";
                case ItemQuantityMeasure.Second: return "сек";
                case ItemQuantityMeasure.Kilobyte: return "кб";
                case ItemQuantityMeasure.Megabyte: return "мб";
                case ItemQuantityMeasure.Gigabyte: return "гб";
                case ItemQuantityMeasure.Terabyte: return "тб";
                default: return "шт";
            }
        }

        /// <summary>
        /// Возвращает строку меру количества в родительском падеже (целое слово)
        /// </summary>
        /// <param name="measure">Мера количества</param>
        /// <returns></returns>
        public static string ToLongString(this ItemQuantityMeasure measure)
        {
            switch (measure)
            {
                //case ItemQuantityMeasure.Piece: return "единиц";
                case ItemQuantityMeasure.Piece: return "порций";
                case ItemQuantityMeasure.Gram: return "грамм";
                case ItemQuantityMeasure.Kilogram: return "килограмм";
                case ItemQuantityMeasure.Tons: return "тонн";
                case ItemQuantityMeasure.Centimeter: return "санитиметров";
                case ItemQuantityMeasure.Decimeter: return "дециметров";
                case ItemQuantityMeasure.Meter: return "метров";
                case ItemQuantityMeasure.SquareCentimeter: return "см2";
                case ItemQuantityMeasure.SquareDecimeter: return "дм2";
                case ItemQuantityMeasure.SquareMeter: return "м2";
                case ItemQuantityMeasure.Milliliter: return "миллилитров";
                case ItemQuantityMeasure.Liter: return "литров";
                case ItemQuantityMeasure.CubicMeter: return "м3";
                case ItemQuantityMeasure.KilowattHour: return "кв/ч";
                case ItemQuantityMeasure.Gigacalorie: return "гк";
                case ItemQuantityMeasure.Day: return "дней";
                case ItemQuantityMeasure.Hour: return "часов";
                case ItemQuantityMeasure.Minute: return "минут";
                case ItemQuantityMeasure.Second: return "секунд";
                case ItemQuantityMeasure.Kilobyte: return "килобайт";
                case ItemQuantityMeasure.Megabyte: return "мегабайт";
                case ItemQuantityMeasure.Gigabyte: return "гигабайт";
                case ItemQuantityMeasure.Terabyte: return "терабайт";
                default: return "штук";
            }
        }

        /// <summary>
        /// Возвращает строку меру количества в родительском падеже (целое слово) c заглавной буквы
        /// </summary>
        /// <param name="measure">Мера количества</param>
        /// <returns></returns>
        public static string ToLongStringTitle(this ItemQuantityMeasure measure)
        {
            var s = ToLongString(measure);
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }
    }
}
