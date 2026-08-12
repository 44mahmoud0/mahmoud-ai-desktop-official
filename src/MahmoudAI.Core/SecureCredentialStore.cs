using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MahmoudAI.Security
{
    public class SecureCredentialStore
    {
        private readonly ILogger<SecureCredentialStore> _logger;

        public SecureCredentialStore(ILogger<SecureCredentialStore> logger)
        {
            _logger = logger;
        }

        public string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to protect credential using Windows DPAPI.");
                throw;
            }
        }

        public string Unprotect(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unprotect credential using Windows DPAPI.");
                throw;
            }
        }
    }
}
