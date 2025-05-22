using RankTests.Configs;
using RankTests.Pages;

namespace RankTests.Tests
{
    public class RankCalculatorE2ETests : TestsBase, IDisposable
    {
        private readonly IndexPage _indexPage;
        private readonly SummaryPage _summaryPage;
        public RankCalculatorE2ETests(BrowserType browserType)
            : base(browserType)
        {
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            _indexPage = new IndexPage(driver);
            _summaryPage = new SummaryPage(driver);
            _indexPage.NavigateToIndexPage();
        }

        [Fact]
        public void SubmitTextAndCheckRank_ShouldReturnValidValue()
        {
            //Act
            _indexPage.WriteTextToTextArea(IndexTestData.Text);
            _indexPage.SubmitForm();

            //Assert
            Thread.Sleep(3100);
            var actualRank = _summaryPage.GetRank();
            Assert.Equal(SummaryTestData.ExpectedRank, actualRank);
        }

        public void Dispose()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}
