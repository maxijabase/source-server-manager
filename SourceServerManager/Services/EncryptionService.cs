using System;
using System.Security.Cryptography;
using System.Text;

namespace SourceServerManager.Services;

public class EncryptionService
{
    public static string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(
                plainTextBytes,
                null, // No additional entropy
                DataProtectionScope.CurrentUser // Encrypt for current user only
            );
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to encrypt string: {ex.Message}", ex);
        }
    }

    public static string DecryptString(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null, // No additional entropy
                DataProtectionScope.CurrentUser // Decrypt for current user only
            );
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to decrypt string: {ex.Message}", ex);
        }
    }

    public static bool IsEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            // Try to decode as Base64 - if it fails, it's likely plain text
            byte[] bytes = Convert.FromBase64String(value);
            
            // If it's valid Base64, try to decrypt it
            // If decryption succeeds, it's encrypted
            ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return true;
        }
        catch
        {
            // If Base64 decode or decryption fails, it's plain text
            return false;
        }
    }
}
