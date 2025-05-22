using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium;

namespace RankTests.Tests
{
    public class TestsBase
    {
        protected readonly IWebDriver driver;
        private readonly BrowserType _browserType;
        public TestsBase(BrowserType browserType)
        {
            _browserType = browserType;
            driver = CreateRemoteDriver(browserType);
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        private IWebDriver CreateRemoteDriver(BrowserType browserType)
        {
            // Адрес Selenium Grid/Standalone сервера
            var gridUrl = "http://localhost:4444/wd/hub"; // или другой адрес вашего сервера

            return browserType switch
            {
                BrowserType.Chrome => new RemoteWebDriver(new Uri(gridUrl), new ChromeOptions().ToCapabilities(), TimeSpan.FromSeconds(120)),
                BrowserType.Edge => new RemoteWebDriver(new Uri(gridUrl), new EdgeOptions().ToCapabilities(), TimeSpan.FromSeconds(120)),
                _ => throw new ArgumentOutOfRangeException(nameof(browserType), browserType, null)
            };
        }
    }

    public enum BrowserType
    {
        Chrome,
        Edge
    }
}
