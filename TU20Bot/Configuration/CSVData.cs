using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using static TU20Bot.Configuration.CSVData;
using CsvHelper.TypeConversion;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace TU20Bot.Configuration {

    public class CSVData {

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        [BooleanTrueValues("Speaker")]
        [BooleanFalseValues("Attendee")]
        public bool isSpeaker { get; set; }

    }
}
