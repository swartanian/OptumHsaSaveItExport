using System;
using System.Collections.Generic;
namespace OptumHsaSaveItExport
{
    class Program
    {
        static void Main(string[] args)
        {
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

    }
}
