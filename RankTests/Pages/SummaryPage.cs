using OpenQA.Selenium;
using System.Globalization;

namespace RankTests.Pages
{
    public class SummaryPage
    {
        private readonly IWebDriver _driver;

        public SummaryPage(IWebDriver driver)
        {
            _driver = driver;
        }

        public IWebElement Rank => _driver.FindElement(By.Id("rank"));

        public double GetRank()
        {
            return Convert.ToDouble(Rank.Text.Split(':')[1].Trim(), CultureInfo.InvariantCulture);
        }
    }
}
