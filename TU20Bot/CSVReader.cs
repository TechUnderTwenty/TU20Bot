using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using CsvHelper;
using CsvHelper.Configuration;

using TU20Bot.Configuration;

namespace TU20Bot {
    public static class CSVReader {
        private const string csvFilePath = "Data/ExpoData.csv";

        public static List<UserDetails> readFile() {
            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) {
                TrimOptions = TrimOptions.Trim
            });
            
            return csv.GetRecords<UserDetails>().ToList();
        }
    }
}
