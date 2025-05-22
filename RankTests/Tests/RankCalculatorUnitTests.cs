using RankCalculator;

namespace RankTests.Tests
{
    public class RankCalculatorUnitTests
    {
        private readonly RankService _calculator = new();
        public static TheoryData<string, double> RankTestData => new TheoryData<string, double>
        {
            { "Hello, world!", 3.0 / 13 },   // ',' and '!'
            { "12345", 1.0 },                // All non-letters
            { "abcDEF", 0.0 },               // All letters
            { "", 0.0 },                     // Empty string
            { "😊", 1.0 },                   // Emoji
            { "     ", 1.0 },                // Only spaces
            { "New\nLine", 1.0 / 8 }         // Newline is non-letter
        };


        [Theory]
        [MemberData(nameof(RankTestData))]
        public void CalculateRank_ReturnsExpected(string input, double expected)
        {
            // Act
            double result = _calculator.CalculateRank(input);

            // Assert
            Assert.Equal(expected, result, precision: 5);
        }
    }
}