namespace RankTests.Tests
{
    public class ChromeRankCalculatorE2ETests : RankCalculatorE2ETests
    {
        public ChromeRankCalculatorE2ETests() : base(BrowserType.Chrome) { }
    }

    public class EdgeRankCalculatorE2ETests : RankCalculatorE2ETests
    {
        public EdgeRankCalculatorE2ETests() : base(BrowserType.Edge) { }
    }
}
