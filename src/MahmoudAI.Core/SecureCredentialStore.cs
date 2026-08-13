using System;
using System.Runtime.InteropServices;
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                    return Convert.ToBase64String(encryptedBytes);
                }
                else
                {
                    // Fallback for non-Windows testing environments
                    return "simulated_enc_" + Convert.ToBase64String(plainBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to protect credential.");
                throw;
            }
        }

        public string Unprotect(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    byte[] cipherBytes = Convert.FromBase64String(cipherText);
                    byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(plainBytes);
                }
                else
                {
                    if (cipherText.StartsWith("simulated_enc_"))
                    {
                        byte[] plainBytes = Convert.FromBase64String(cipherText.Substring("simulated_enc_".Length));
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                    byte[] cipherBytes = Convert.FromBase64String(cipherText);
                    return Encoding.UTF8.GetString(cipherBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unprotect credential.");
                throw;
            }
        }
    }
}
