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
                int stopAfter = int.MaxValue; //modify this to a smaller number for debug or trial runs
                int itemNumber = 1;

                foreach (string link in links)
                {
                    Console.WriteLine("processing {0} of {1}", itemNumber, links.Count);
                    if (link == null)
                    {
                        Console.WriteLine("Info: The very last row of the records table was empty - this is typical when there is a 'show more' button", itemNumber++, links.Count);
                        continue;
                    }

                    data.AddNewRecord();
                    data.AddProperty("url", link);
                    ui.GoToUrl(link);
                    ui.ProcessDetails(data);

                    itemNumber++;
                    if (itemNumber > stopAfter) break;
                }

                data.WriteToCsv();
            }
            finally
            {
                ui.Quit();
            }
            Console.WriteLine("Finished processing all records. You will find your output in the same directory as the binary.\nPress any key to exit.");
            Console.ReadLine();
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
