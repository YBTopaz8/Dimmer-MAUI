using System.Security.Cryptography;

namespace Dimmer.Utils;

public class PasswordEncryptionService
{
    private const string EncryptionKeyAlias = "MyPasswordEncryptionKey"; // Key to retrieve the AES key from SecureStorage
    private const int KeyBitSize = 256; // AES 256
    private const int NonceBitSize = 96; // 12 bytes, recommended for AES-GCM
    private const int TagBitSize = 128;  // 16 bytes, common for AES-GCM

    private byte[] _encryptionKey;

    // --- Key Management ---
    private async Task<byte[]> GetOrCreateEncryptionKeyAsync()
    {
        if (_encryptionKey != null)
        {
            return _encryptionKey;
        }

        string? keyBase64 = await SecureStorage.GetAsync(EncryptionKeyAlias);
        if (!string.IsNullOrEmpty(keyBase64))
        {
            _encryptionKey = Convert.FromBase64String(keyBase64);
            return _encryptionKey;
        }
        else
        {
            // Generate a new key
            _encryptionKey = new byte[KeyBitSize / 8];
            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(_encryptionKey);
            }
            await SecureStorage.SetAsync(EncryptionKeyAlias, Convert.ToBase64String(_encryptionKey));
            return _encryptionKey;
        }
    }

    // --- Encryption ---
    public async Task<string?> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return null;
        }

        try
        {
            byte[] key = await GetOrCreateEncryptionKeyAsync();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] nonce = new byte[NonceBitSize / 8];
            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(nonce); // Generate a random nonce for each encryption
            }

            byte[] cipherText = new byte[plainBytes.Length];
            byte[] tag = new byte[TagBitSize / 8];

            using (var aesGcm = new AesGcm(key))
            {
                aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);
            }

            // Combine nonce, ciphertext, and tag for storage
            // Format: nonce (12 bytes) + ciphertext (variable) + tag (16 bytes)
            byte[] encryptedDataWithNonceAndTag = new byte[nonce.Length + cipherText.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, encryptedDataWithNonceAndTag, 0, nonce.Length);
            Buffer.BlockCopy(cipherText, 0, encryptedDataWithNonceAndTag, nonce.Length, cipherText.Length);
            Buffer.BlockCopy(tag, 0, encryptedDataWithNonceAndTag, nonce.Length + cipherText.Length, tag.Length);

            return Convert.ToBase64String(encryptedDataWithNonceAndTag);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Encryption failed: {ex.Message}");
            // Handle or log exception appropriately
            return null;
        }
    }

    // --- Decryption ---
    public async Task<string?> DecryptAsync(string? encryptedBase64Text)
    {
        if (string.IsNullOrEmpty(encryptedBase64Text))
        {
            return null;
        }

        try
        {
            byte[] key = await GetOrCreateEncryptionKeyAsync();
            byte[] encryptedDataWithNonceAndTag = Convert.FromBase64String(encryptedBase64Text);

            int nonceSize = NonceBitSize / 8;
            int tagSize = TagBitSize / 8;

            if (encryptedDataWithNonceAndTag.Length < nonceSize + tagSize)
            {
                throw new ArgumentException("Encrypted data is too short to contain nonce and tag.");
            }

            byte[] nonce = new byte[nonceSize];
            Buffer.BlockCopy(encryptedDataWithNonceAndTag, 0, nonce, 0, nonceSize);

            byte[] tag = new byte[tagSize];
            Buffer.BlockCopy(encryptedDataWithNonceAndTag, encryptedDataWithNonceAndTag.Length - tagSize, tag, 0, tagSize);

            byte[] cipherText = new byte[encryptedDataWithNonceAndTag.Length - nonceSize - tagSize];
            Buffer.BlockCopy(encryptedDataWithNonceAndTag, nonceSize, cipherText, 0, cipherText.Length);

            byte[] decryptedBytes = new byte[cipherText.Length];

            using (var aesGcm = new AesGcm(key))
            {
                aesGcm.Decrypt(nonce, cipherText, tag, decryptedBytes);
            }

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (CryptographicException ex) // Specifically catch this for decryption failures (e.g., bad key, tampered data)
        {
            Debug.WriteLine($"Decryption failed (CryptographicException): {ex.Message}");
            // This often means the key is wrong or the data was tampered with.
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Decryption failed: {ex.Message}");
            // Handle or log exception appropriately
            return null;
        }
    }

    // Optional: Method to clear the stored encryption key (e.g., on app uninstall or specific user action)
    public static void RemoveEncryptionKey()
    {
        SecureStorage.Remove(EncryptionKeyAlias);
    }
}