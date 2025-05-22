using OpenQA.Selenium;
using RankTests.Configs;

namespace RankTests.Pages
{
    public class IndexPage
    {
        private readonly IWebDriver _driver;

        public IndexPage(IWebDriver driver)
        {
            _driver = driver;
        }

        public IWebElement TextArea => _driver.FindElement(By.Id("text"));
        public IWebElement SubmitButton => _driver.FindElement(By.Id("submit"));

        public void SubmitForm()
        {
            SubmitButton.Click();
        }

        public void WriteTextToTextArea(string text)
        {
            TextArea.SendKeys(text);
        }

        public void NavigateToIndexPage()
        {
            _driver.Navigate().GoToUrl(IndexTestData.LoginUrl);
        }
    }
}
