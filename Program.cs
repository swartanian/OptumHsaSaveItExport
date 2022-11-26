using System;
using System.Collections.Generic;
namespace OptumHsaSaveItExport
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!HandleArgs(args)) return;

            UiModel ui = UiModel.GetInstance();
            DataModel data = DataModel.GetInstance();
            try
            {
                ui.Login();
                List<string> links = ui.GetAllClaimLinks();
                int stopAfter = 10000;
                int itemNumber = 1;

                foreach (string link in links)
                {
                    if (link == null) continue;//the very last row in the table is empty

                    Console.WriteLine("processing {0} of {1}", itemNumber++, links.Count);

                    data.AddNewRecord();
                    data.AddProperty("url", link);
                    ui.GoToUrl(link);
                    ui.ProcessDetails(data);

                    if (itemNumber >= stopAfter) break;
                }

                data.WriteToCsv();
            }
            finally
            {
                ui.Quit();
            }
        }

        static bool HandleArgs(string[] args)
        {
            bool argsAreCorrect = true;
            if (args.Length > 0)
            {
                LoginType login;
                argsAreCorrect = Enum.TryParse<LoginType>(args[0].ToLower(), out login);
                Settings.Login = login;
            }
            else
            {
                Console.WriteLine("No args passed. Defaulting to {0}", Settings.Login);
            }
            if (!argsAreCorrect)
            {
                Console.WriteLine("Args are incorrect\nPass the following optional parameters" +
                    "\n manualLogin - you will be responsible for logging in and getting the browser to the optum site and HSA Save-it piggybank url" +
                    "\n basicLogin - you will be responsible for authenticating with SSO via benefits ehr and clicking Yes/No to staying logged in" +
                    "\n fullLogin - you will be responsible for only interacting with your 2FA/MFA device, everything else is handled for you");
            }
            return argsAreCorrect;

        }

    }
}
