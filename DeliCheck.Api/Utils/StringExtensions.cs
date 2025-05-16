namespace DeliCheck.Utils
{
    public static class StringExtensions
    {
        public static string CapsToLower(this string text)
        {
            int i = 0;

            if (text.Length > 1 && char.IsLower(text[0]))
                text = text.Substring(0, 1).ToUpper() + text.Substring(1).ToLower();

            return string.Join(" ", text.Split(' ', '-').Select(x => 
            {
                if (x.Length > 1 && x.All(x => char.IsLetter(x) && char.IsUpper(x)))
                {
                    if(i++ == 0)
                    {
                        return x.Substring(0, 1).ToUpper() + x.Substring(1).ToLower(); 
                    }
                    else
                    {
                        return x.ToLower(); 
                    }
                }
                else return x; 
            }));
        }

        public static string AllToLower(this string text)
        {
            int i = 0;

            if (text.Length > 1 && char.IsLower(text[0]))
                text = text.Substring(0, 1).ToUpper() + text.Substring(1).ToLower();

            return string.Join(" ", text.Split(' ', '-').Select(x =>
            {
                if (x.Length > 1 && x.Skip(1).Any(x => char.IsLetter(x) && char.IsUpper(x)))
                {
                    if (i++ == 0)
                    {
                        return x.Substring(0, 1).ToUpper() + x.Substring(1).ToLower();
                    }
                    else
                    {
                        return x.ToLower();
                    }
                }
                else return x;
            }));
        }
    }
}
