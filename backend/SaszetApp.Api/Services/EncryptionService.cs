using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SaszetApp.Api.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        public EncryptionService(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var keyString = configuration["ENCRYPTION_KEY"];
            if (string.IsNullOrEmpty(keyString))
            {
                throw new InvalidOperationException("ENCRYPTION_KEY environment variable is not set.");
            }

            // Ensure key is 32 bytes for AES-256
            if (keyString.Length != 32)
            {
                // If it's not 32 chars, we can pad or hash it. Hashing is safer.
                using var sha256 = SHA256.Create();
                _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            }
            else
            {
                _key = Encoding.UTF8.GetBytes(keyString);
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using var aesAlg = Aes.Create();
            aesAlg.Key = _key;
            aesAlg.GenerateIV();

            using var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using var msEncrypt = new MemoryStream();
            
            // Write IV to the beginning of the stream
            msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            var fullCipher = Convert.FromBase64String(cipherText);

            using var aesAlg = Aes.Create();
            aesAlg.Key = _key;

            // Extract IV from the first 16 bytes
            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, 16);
            aesAlg.IV = iv;

            using var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using var msDecrypt = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
    }
}
