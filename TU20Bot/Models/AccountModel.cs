using System.Collections.Generic;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TU20Bot.Models {
    public class AccountModel {
        private const int saltSize = 32;
        private const int hashSize = 32;
        private const int iterations = 100000;

        public const string collectionName = "user-account-pivot";

        [BsonId]
        public ObjectId id { get; set; }
        public string username { get; set; }
        public List<string> permissions { get; set; }
        
        public byte[] salt { get; set; }
        public string passwordHash { get; set; }

        public void setPassword(string password) {
            var pbkdf2DeriveBytes = new Rfc2898DeriveBytes(password, saltSize, iterations);

            passwordHash = Convert.ToBase64String(pbkdf2DeriveBytes.GetBytes(hashSize));
            salt = pbkdf2DeriveBytes.Salt;
        }

        public bool checkPassword(string password) {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var checkPass = generateHash(passwordBytes, salt, iterations, hashSize);

            return passwordHash.SequenceEqual(checkPass);
        }

        /// <summary>
        /// Given a password string, create a PBKDF2 Hash.
        /// </summary>
        private static string generateHash(byte[] password, byte[] salt, int iterations, int length) {
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations);
            return Convert.ToBase64String(deriveBytes.GetBytes(length));
        }
    }
}