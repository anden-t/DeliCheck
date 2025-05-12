namespace DeliCheck.Utils
{
    public static class StringExtensions
    {
        public static string CapsToLower(this string text)
        {
            int i = 0;
            return string.Join(" ", text.Split(' ').Select(x => 
            {
                if (x.Length > 2 && x.All(x => char.IsLetter(x) && char.IsUpper(x)))
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
    }
}
