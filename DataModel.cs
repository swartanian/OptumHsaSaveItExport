using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OptumHsaSaveItExport
{
    class DataModel
    {
        private List<Dictionary<string, string>> records = new List<Dictionary<string, string>>();
        private Dictionary<string, int> fieldInfo = new Dictionary<string, int>();
        private Dictionary<string, string> currentRecord;

        private DataModel() { }
        public static DataModel GetInstance()
        {
            return new DataModel();
        }

        public void AddNewRecord()
        {
            currentRecord = new Dictionary<string, string>();
            records.Add(currentRecord);
        }

        public void AddProperty(string field, string value)
        {
            if (currentRecord == null) throw new InvalidOperationException("AddNewRecord must be called first");

            string cleanField = field.CleanName();
            string cleanValue = value.CleanValue();
            currentRecord[cleanField] = cleanValue; // add or overwrite

            //update field statistics
            if (fieldInfo.ContainsKey(cleanField))
            {
                fieldInfo[cleanField]++;
            }
            else
            {
                fieldInfo[cleanField] = 1;
            }
        }
        
        public string GetProperty(string field)
        {
            string ret;
            bool present = currentRecord.TryGetValue(field.CleanName(), out ret);
            return present ? ret : "";
        }

        public void WriteToCsv()
        {
            using (var writer = new StreamWriter(@"OptumHsaSaveIt_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv"))
            {
                //write field headers
                foreach (string field in fieldInfo.Keys)
                {
                    writer.WriteWithComma(field);
                }
                writer.Write("NumberOfFields");
                writer.WriteLine();

                //write field statistics
                foreach (int field in fieldInfo.Values)
                {
                    writer.WriteWithComma(field.ToString());
                }
                writer.Write("n/a"); //field statistics for NumberOfFields are per-row
                writer.WriteLine();

                foreach (var record in records)
                {
                    foreach (var key in fieldInfo.Keys)
                    {
                        string value = record.ContainsKey(key) ? record[key] : "";
                        writer.WriteWithComma(value);
                    }
                    writer.Write(record.Keys.Count); //number of fields in the record
                    writer.WriteLine();
                }
            }
        }
    }

    public static class WriterExtension
    {
        public static void WriteWithComma(this StreamWriter sw, string value)
        {
            sw.Write("\"{0}\",", value);
        }
    }

    public static class StringExtension
    { 
        //Converts a string to TitleCase and removes internal spaces
        //"FOO BAR BAZ" becomes "FooBarBaz"
        public static string CleanName(this string s) =>
            CultureInfo.InvariantCulture.TextInfo
                .ToTitleCase(s.ToLowerInvariant())
                .Replace(" ", "");     

        public static string CleanValue(this string s) =>
            s.Replace("\r\n", "|");
    }

}
