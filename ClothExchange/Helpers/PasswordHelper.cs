using System.Security.Cryptography;
using System.Text;

namespace ClothExchange.Helpers
{
    public static class PasswordHelper
    {
        // We never store the real password, only its SHA256 hash.
        public static string Hash(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToHexString(bytes);
            }
        }
    }
}
