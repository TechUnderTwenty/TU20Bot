using System.Collections.Generic;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TU20Bot.Models {
    public class AccountModel {
        public const int HASH_SIZE = 24;
        private const int ITERATIONS = 100000;

        public const string collectionName = "user-account-pivot";

        [BsonId]
        public ObjectId id { get; set; }
        public string username { get; set; }
        public List<string> permissions { get; set; }
        public byte[] salt { get; set; }
        public string passwordHash { get; set; }

        public void setPassword(string password) {
            var salt = generateSalt(HASH_SIZE);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            this.passwordHash = generateHash(passwordBytes, salt, ITERATIONS, HASH_SIZE);
            this.salt = salt;
        }

        public bool checkPassword(string password) {
            var salt = this.salt;
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var checkPass = generateHash(passwordBytes, salt, ITERATIONS, HASH_SIZE);

            return this.passwordHash.SequenceEqual(checkPass);
        }

        private static byte[] generateSalt(int length) {
            var bytes = new byte[length];

            using (var rng = new RNGCryptoServiceProvider()) {
                rng.GetBytes(bytes);
            }

            return bytes;
        }

        /// <summary>
        /// Given a password string, create a PBKDF2 Hash
        /// </summary>
        private static string generateHash(byte[] password, byte[] salt, int iterations, int length) {
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations);
            return Convert.ToBase64String(deriveBytes.GetBytes(length));
        }
    }
}