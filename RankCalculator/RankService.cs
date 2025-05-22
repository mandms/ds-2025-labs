namespace RankCalculator
{
    public class RankService : IRankService
    {
        public double CalculateRank(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int totalChars = text.Length;
            int nonAlphabeticCount = text.Count(c => !char.IsLetter(c));

            return Math.Round((double)nonAlphabeticCount / totalChars, 3);
        }
    }
}
