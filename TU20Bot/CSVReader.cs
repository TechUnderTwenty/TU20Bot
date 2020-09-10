using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TU20Bot.Configuration;

namespace TU20Bot {
    public class CSVReader {

        private Config config;
        private readonly string csvFilePath = "Data/ExpoData.csv";

        public CSVReader(Config config) {
            this.config = config;
        }

        public List<CSVData> readFile() {

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) {
                TrimOptions = TrimOptions.Trim
            });
            var records = csv.GetRecords<CSVData>();
            
            return csv.GetRecords<CSVData>().ToList();

        }

    }
}
