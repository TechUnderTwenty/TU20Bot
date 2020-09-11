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
