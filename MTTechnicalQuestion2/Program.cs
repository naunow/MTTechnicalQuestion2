using CsvHelper;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MTTechnicalQuestion2
{
    internal class Program
    {
        static readonly HttpClient client = new HttpClient();
        const string url = @"https://hiring-ld-takehome.herokuapp.com/transactions?";
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            logger.Debug("===== Process Start =====");

            Console.WriteLine("Enter From Date. (yyyy-MM-dd)");
            var fromDateStr = Console.ReadLine();

            Console.WriteLine("Enter To Date. (yyyy-MM-dd)");
            var toDateStr = Console.ReadLine();

            if(!DateTime.TryParse(fromDateStr, out DateTime f) || !DateTime.TryParse(toDateStr, out DateTime t))
            {
                throw new Exception("Invalid date format.");
            }

            try
            {
                var dlFileDirectory = Directory.GetCurrentDirectory() + @"\App_Data\CSV\";
                var fileName = DateTime.Now.ToString("yyyyMMdd_") + Guid.NewGuid() + ".csv";
                var fullPath = dlFileDirectory + fileName;
                logger.Debug($"File path:{fullPath}");

                var searchDate = f;

                // Execute API request for each day
                while (searchDate < t)
                {
                    await GetTransactionData(searchDate, fullPath);
                    searchDate = searchDate.AddDays(1);
                }

            }
            catch (HttpRequestException e)
            {
                logger.Error($"HttpRequestException Message :{e.Message}");
            }
            catch (Exception e)
            {
                logger.Error($"Exception Message :{e.Message} StackTrace :{e.StackTrace}");
            }

            logger.Debug("===== Process End =====");

        }

        private static async Task GetTransactionData(DateTime date, string fullPath)
        {
            var fromDate = date.ToString("yyyy-MM-dd");
            var toDate = date.AddDays(1).ToString("yyyy-MM-dd");
            var query = $"fromDate={fromDate}&toDate={toDate}";
            var uri = url + query;

            logger.Debug($"Send api request From:{fromDate} To:{toDate}");
            string responseBody = await client.GetStringAsync(uri);
            logger.Debug($"Received api response");
            logger.Trace(responseBody);

            // Serialize the response result so that it can be treated as an object
            var data = JsonSerializer.Deserialize<TransactionDataDTO>(responseBody);
            logger.Trace(JsonSerializer.Serialize(data));

            if (!data.Data.Any())
            {
                logger.Debug($"No data");
                return;
            }

            logger.Debug($"{data.Data.Count} transactions");

            // Format csv data from api result DTO
            List<TransactionDataCsv> csvData = FormatCsvData(data.Data, date);

            bool outputHeader = !File.Exists(fullPath);

            // Write to csv file
            using (var fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write))
            {
                logger.Debug($"Started writing csv data");
                var csv = new CSVHelper(csvData).WriteRecords(outputHeader);
                fs.Write(csv, 0, csv.Length);
                logger.Debug($"Finished writing csv data");
            }
        }

        private static List<TransactionDataCsv> FormatCsvData(List<TransactionDataDetailDTO> data, DateTime date)
        {
            var list = new List<TransactionDataCsv>();
            foreach (var item in data)
            {
                // API will be returned 30-0 as date when searching January 30, so adjust the month by +1
                // Bug?
                var m = int.Parse(item.Date.Split('-')[1]) + 1;
                var month = m.ToString().PadLeft(2, '0');
                var day = item.Date.Split('-')[0].PadLeft(2, '0');
                item.Date = $"{date.Year}-{month}-{day}";

                var formatData = new TransactionDataCsv
                {
                    Date = DateTime.Parse(item.Date),
                    Amount = decimal.Parse(item.Amount),
                    Description = item.Description
                };
                list.Add(formatData);
            }
            return list;
        }
    }

    class TransactionDataDTO
    {
        [JsonPropertyName("data")]
        public List<TransactionDataDetailDTO> Data { get; set; }
    }

    class TransactionDataDetailDTO
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    class TransactionDataCsv
    {
        [DisplayName("Date")]
        public DateTime Date { get; set; }

        [DisplayName("Amount")]
        public decimal Amount { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }
    }
}
