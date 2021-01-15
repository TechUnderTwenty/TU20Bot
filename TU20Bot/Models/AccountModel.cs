using System.Collections.Generic;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TU20Bot.Models {
    public class AccountModel {
        public const int SALT_SIZE = 32;
        public const int HASH_SIZE = 32;
        private const int ITERATIONS = 100000;

        public const string collectionName = "user-account-pivot";

        [BsonId]
        public ObjectId id { get; set; }
        public string username { get; set; }
        public List<string> permissions { get; set; }
        public byte[] salt { get; set; }
        public string passwordHash { get; set; }

        public void setPassword(string password) {
            var pbkdf2DeriveBytes = new Rfc2898DeriveBytes(password, SALT_SIZE, ITERATIONS);

            this.passwordHash = Convert.ToBase64String(pbkdf2DeriveBytes.GetBytes(HASH_SIZE));
            this.salt = pbkdf2DeriveBytes.Salt;
        }

        public bool checkPassword(string password) {
            var salt = this.salt;
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var checkPass = generateHash(passwordBytes, salt, ITERATIONS, HASH_SIZE);

            return this.passwordHash.SequenceEqual(checkPass);
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