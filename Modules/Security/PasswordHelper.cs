using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HelpDeskTicketingSystem.Security
{
    internal class PasswordHelper
    {
        public static string Hash(string plainTextPassword)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(plainTextPassword);
            byte[] hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        public static bool Verify(string plainTextPassword, string storedHash)
        {
            string attemptedHash = Hash(plainTextPassword);
            return attemptedHash == storedHash;
        }
    }
}
