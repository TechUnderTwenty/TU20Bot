using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TU20Bot;
using TU20Bot.Commands;
using TU20Bot.Configuration;
using Discord;
using System.Linq;
using System.Collections.Generic;
using CsvHelper;
using System.Globalization;

namespace BotTest {
    [TestClass]
    public class EmailVerificationTest {

        private static EmailVerification _emailVerification;
        private static EmailChecker _emailChecker;
        private static Config _config;
        private static Client _client;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) {

            // Initializing all the required classes
            _config = new Config();
            _client = new Client(_config);

            _emailVerification = new EmailVerification();
            _emailChecker = new EmailChecker(_config, _client);
        }


        [TestMethod]
        public void CheckCompareMethod() {
            var records = new List<CSVData> {
                new CSVData { FirstName = "john1", LastName = "Doe1", Email = "johndoe@tu20.com", isSpeaker = true },
            };

            Assert.IsNotNull(_emailVerification.emailCompare("johndoe@tu20.com", records));

            Assert.IsNull(_emailVerification.emailCompare("johndoe@nothing.com", records));
        }

        [TestMethod]
        public void CheckNotInListEmail() {
            var records = new List<CSVData> { };
            // Running the method with an email not in the list 
            Assert.IsNull(_emailVerification.emailCompare("johndoe@examplemail.com", records));

            // Adding the same unavailable email to the csv file and dictionary
            _emailVerification.saveUnverifiedEmail(_config.userEmailId, 1, "johndoe@examplemail.com");
            records.Add(new CSVData { FirstName = "john", LastName = "Doe", Email = "johndoe@examplemail.com", isSpeaker = true });

            // Checking to see if the email is present in the list
            // This will be run on separate thread when program is running
            var result = _emailChecker.checkEmailInCsvList(records);

            // If the function is working properly, it will return the persons info
            Assert.IsNotNull(result.userData);
        }
    }
}