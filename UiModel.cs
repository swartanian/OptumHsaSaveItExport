using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OptumHsaSaveItExport
{
    class UiModel
    {
        public static UiModel GetInstance()
        {
            return new UiModel();
        }

        private EdgeDriver driver;
        private IJavaScriptExecutor jsDriver;
        private bool highlightElements = false;

        private UiModel()
        {
            driver = new EdgeDriver();
            jsDriver = (IJavaScriptExecutor)driver;
        }

        public void GoToUrl(string url)
        {
            driver.Url = url;
        }

        private void Highlight(IWebElement element)
        {
            if (highlightElements)
            {
                jsDriver.ExecuteScript("arguments[0].style.border='2px solid red'", element);
            }
        }

        private IWebElement FindId(string Id)
        {
            IWebElement ret = driver.FindElement(By.Id(Id));
            Highlight(ret);
            return ret;
        }


        public void ProcessDetails(DataModel data)
        {
            try
            {
                //show all details
                bool systemClaim = false; // either systemClaim (most claims) or self-submitted which has to be handled differently
                var showAll = driver.FindElements(By.Id("allServiceInfoLink"));
                if (showAll.Count > 0)
                {
                    showAll[0].Click();
                    systemClaim = true;
                }
                data.AddProperty("Meta Claim Type", systemClaim ? "system generated claim" : "manually entered claim");

                string claimFormName = systemClaim ? "healthPlanClaim" : "claimCenter";
                var claimForm = FindId(claimFormName);

                //service info section
                var serviceInfoSection = claimForm.FindElements(By.CssSelector("div.row"))[0]; // the first section doesn't have any further identifiers
                AddFieldDetails(serviceInfoSection, data);

                if (systemClaim)
                {
                    //Claim ID
                    var claimIdElement = claimForm.FindElement(By.CssSelector("h1"));
                    string claimId = claimIdElement.Text.Split('#')[1];
                    data.AddProperty("Claim Id", claimId);

                    //Additional Service Details
                    var additionalServiceDetails = claimForm.FindElement(By.CssSelector("#allServiceInfo"));
                    AddFieldDetails(additionalServiceDetails, data);
                }

                //sometimes there is an extra "paid from" section
                var allClaimRows = claimForm.FindElements(By.CssSelector("div.row"));
                if (allClaimRows.Count == 7)
                {
                    int indexOfPaidFromExtraRow = systemClaim ? 2 : 1;
                    var paidFromExtraRowText = allClaimRows[indexOfPaidFromExtraRow].FindElement(By.CssSelector("div.form-group"));
                    data.AddProperty("Paid From Personal Funds", paidFromExtraRowText.Text);
                }

                //Payment Details
                var paymentDetails = claimForm.FindElement(By.CssSelector("div.row.payment-details"));
                AddFieldDetails(paymentDetails, data);

                string myNotes = driver.FindElement(By.Id("viewMyNotes")).Text;
                data.AddProperty("Notes", myNotes);

                //customer Service Notes - this appears for most but not all records
                var customerServiceNotes = driver.FindElements(By.Id("csNotesPane"));
                if (customerServiceNotes.Count > 0)
                {
                    data.AddProperty("Customer Service Notes", customerServiceNotes[0].Text);
                }

                //documentation
                var docs = driver.FindElement(By.CssSelector("div.col-xs-12.no-table"));
                //Highlight(docs);
                var docItems = docs.FindElements(By.CssSelector("#documents li a"));
                if (docItems.Count > 0)
                {
                    data.AddProperty("Uploaded Documents", docItems.Count.ToString());

                    //[yyyymmdd claim date]_ [hsa id]_[claim id]_[Person name]_[Doctor/Provider]_[amount in dollars_cents].[extension]. 
                    string fileName = string.Format("{0}_{1}_{2}_{3}_{4}_"
                        , DateTime.Parse(data.GetProperty("Date Of Service")).ToString("yyyyMMdd")
                        , data.GetProperty("Claim Id")
                        , data.GetProperty("Health Plan Claim Id")
                        , data.GetProperty("Service For")
                        , data.GetProperty("vendor/Provider")
                        );
                    fileName = Regex.Replace(fileName, @"[\/?:*""><|]+", "", RegexOptions.Compiled);

                    docItems[0].Click(); //it doesn't matter which one to click on, they all go to the same page
                    HandleDocuments(fileName, claimFormName);
                }
                else  //check for docs that cannot be downloaded right now. 
                {
                    if (docs.Text != "")
                    {
                        string errorText = "Documents were not downloaded - message: " + docs.Text;
                        data.AddProperty("Error", errorText);
                        Console.WriteLine(errorText);

                        //click the link to "... Request Archived Documentation"
                        var requestArchivedLink = docs.FindElement(By.CssSelector("a"));
                        requestArchivedLink.Click();
                    }
                }
            }
            catch (Exception e)
            {
                data.AddProperty("Error", e.Message);
            }        
        }

        private void HandleDocuments(string fileName, string claimFormName)
        {
            int docCount = GetOnlyDocumentLinks(claimFormName).Count;
            for (int i = 0; i < docCount; i++)
            {
                var docLink = GetOnlyDocumentLinks(claimFormName)[i]; //the page reloads on each click, so we can't foreach the aTags collection but instead have to index into it and rebuild the document link collection after each click
                docLink.Click();

                //Image
                var images = driver.FindElements(By.CssSelector(string.Format("#{0} img", claimFormName)));
                if (images.Count > 0)
                {
                    string url = images[0].GetAttribute("src");
                    GetFile(url, fileName, i, ".png");
                }

                //PDF
                var pdfs = driver.FindElements(By.CssSelector(string.Format("#{0} iframe", claimFormName)));
                if (pdfs.Count > 0)
                {
                    string url = pdfs[0].GetAttribute("src");
                    GetFile(url, fileName, i, ".pdf");
                    //todo: pdfs
                }
            }
        }

        private Collection<IWebElement> GetOnlyDocumentLinks(string claimFormName)
        {
            Collection<IWebElement> ret = new Collection<IWebElement>();

            var aTags = driver.FindElements(By.CssSelector(string.Format("#{0} a", claimFormName)));
 
            foreach (var tag in aTags)
            {
                if (tag.Text.ToLower().StartsWith("page"))
                {
                    ret.Add(tag);
                }
            }
            return ret;
        }

        private void GetFile(string url, string fileSnippet, int i, string extension)
        {
            Console.WriteLine(url);
            Console.WriteLine("Downloading file {0}...", fileSnippet);
            string fileName = fileSnippet + i + extension;

            StringBuilder cookieBuilder = new StringBuilder();
            foreach (var c in driver.Manage().Cookies.AllCookies)
            {
                cookieBuilder.Append(c.Name + "=" + c.Value + ",");
            }
            cookieBuilder.Remove(cookieBuilder.Length - 1, 1); //remove last ";", not sure if necessary
            string cookies = cookieBuilder.ToString();

            var request = WebRequest.CreateHttp(url);
            request.Method = WebRequestMethods.Http.Get;
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.SetCookies(new Uri(url), cookies);

            var response = request.GetResponse();
            var rstream = response.GetResponseStream();

            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;

            using (FileStream fs = File.Create(fileName))
            {
                while ((bytesRead = rstream.Read(buffer, 0, bufferSize)) != 0)
                {
                    fs.Write(buffer, 0, bytesRead);
                }
            }

        }

        internal void Quit()
        {
            driver.Quit();
        }

        private static void AddFieldDetails(IWebElement serviceInfoSection, DataModel data)
        {
            string fieldName;
            string value;
            var serviceItems = serviceInfoSection.FindElements(By.CssSelector("div.form-group"));
            foreach (var item in serviceItems)
            {
                var sections = item.FindElements(By.CssSelector("*"));

                //special casing just for flattening payment methods into one text record
                if (sections[0].TagName == "label" || sections[0].Text == "NOT PAID")
                {
                    fieldName = "Accounts Paid From";
                    value = item.Text;
                }
                else //all other cases
                {
                    fieldName = sections[0].Text;
                    value = sections.Count > 1 ? sections[1].Text : "(no value)";
                }
                data.AddProperty(fieldName, value);
            }
        }


        public List<string> GetAllClaimLinks()
        {
            var showMore = driver.FindElements(By.LinkText("Show More Records"));
            if (showMore.Count > 0)
            {
                showMore[0].Click();
            }

            var linkElements = driver.FindElements(By.CssSelector("#transactionsTable a"));
            List<string> links = new List<string>();

            foreach (var linkElement in linkElements)
            {
                links.Add(linkElement.GetAttribute("href"));
            }
            return links;
        }

        public void Login()
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));

            string line = new string('=', 85);
            Console.WriteLine(line + "\nACTION NEEDED: You have 120 seconds to login.");
            if (Settings.Login == LoginType.manuallogin)
            {
                Console.WriteLine(" Please login and navigate to Optum's HSA save-it account" +
                    "\n NOTE: Your URL should end in 'piggybank'" +
                    "\n" + line);
            }

            if (Settings.Login >= LoginType.basiclogin)
            {
                // driver.Url aka link - [https://aka.ms/BCP/FSA_HSA]
                driver.Url = "https://myapps.microsoft.com/signin/HRIT-Premera-Prod/d1b6f28d-e277-48bf-8871-35a37e6de488?tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47&relaystate=https%3A%2F%2Fmember.premera.com%2FYmlsbHBheQ%3D%3D";
                
                wait.Until(ExpectedConditions.UrlContains("https://www.fundingpremerawa.com/"));
                //Go to save-it
                driver.Url = "https://www.fundingpremerawa.com/portal/CC/cdhportal/cdhaccount/piggybank";
            }

            wait.Until(ExpectedConditions.UrlContains("https://www.fundingpremerawa.com/portal/CC/cdhportal/cdhaccount/piggybank"));
        }

    }
}
