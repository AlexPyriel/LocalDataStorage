using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LocalDataStorage.Internal
{
    /// <summary>
    /// AES-256 encryption helper. Stateless. The pass phrase is salted per entry so that two writes
    /// of the same plaintext produce different ciphertexts (prevents known-plaintext inference).
    /// </summary>
    internal sealed class EncryptionService
    {
        private const int KEY_SIZE_BITS = 256;
        private const int KEY_SIZE_BYTES = KEY_SIZE_BITS / 8;
        private const int BLOCK_SIZE_BITS = 256;
        private const int SALT_SIZE_BYTES = KEY_SIZE_BYTES;
        private const int IV_SIZE_BYTES = KEY_SIZE_BYTES;
        private const int DERIVATION_ITERATIONS = 1000;
        private const int RANDOM_ENTROPY_SIZE_BYTES = 32;

        /// <summary>Length of the salt + IV prefix prepended to the ciphertext.</summary>
        public const int HEADER_SIZE_BYTES = SALT_SIZE_BYTES + IV_SIZE_BYTES;

        private readonly string _passPhrase;

        public EncryptionService(string passPhrase)
        {
            if (string.IsNullOrEmpty(passPhrase))
                throw new ArgumentException("Pass phrase must be non-empty.", nameof(passPhrase));

            _passPhrase = passPhrase;
        }

        /// <summary>Encrypts the plaintext to a self-contained blob: [salt][iv][ciphertext].</summary>
        public byte[] Encrypt(byte[] plainBytes)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                return Array.Empty<byte>();

            byte[] salt = GenerateRandomBytes();
            byte[] iv = GenerateRandomBytes();

            using Rfc2898DeriveBytes derive = new(_passPhrase, salt, DERIVATION_ITERATIONS);
            byte[] key = derive.GetBytes(KEY_SIZE_BYTES);

            using RijndaelManaged cipher = new()
            {
                BlockSize = BLOCK_SIZE_BITS,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
            };

            using ICryptoTransform encryptor = cipher.CreateEncryptor(key, iv);
            using MemoryStream buffer = new();
            using (CryptoStream crypto = new(buffer, encryptor, CryptoStreamMode.Write))
            {
                crypto.Write(plainBytes, 0, plainBytes.Length);
                crypto.FlushFinalBlock();
            }

            byte[] cipherText = buffer.ToArray();
            byte[] result = new byte[HEADER_SIZE_BYTES + cipherText.Length];
            Buffer.BlockCopy(salt, 0, result, 0, SALT_SIZE_BYTES);
            Buffer.BlockCopy(iv, 0, result, SALT_SIZE_BYTES, IV_SIZE_BYTES);
            Buffer.BlockCopy(cipherText, 0, result, HEADER_SIZE_BYTES, cipherText.Length);
            return result;
        }

        /// <summary>Decrypts the blob produced by <see cref="Encrypt"/>. Throws on malformed or wrong-key input.</summary>
        public byte[] Decrypt(byte[] cipherBlob)
        {
            if (cipherBlob == null || cipherBlob.Length <= HEADER_SIZE_BYTES)
                throw new ArgumentException("Cipher blob is shorter than the encryption header.", nameof(cipherBlob));

            byte[] salt = new byte[SALT_SIZE_BYTES];
            byte[] iv = new byte[IV_SIZE_BYTES];
            Buffer.BlockCopy(cipherBlob, 0, salt, 0, SALT_SIZE_BYTES);
            Buffer.BlockCopy(cipherBlob, SALT_SIZE_BYTES, iv, 0, IV_SIZE_BYTES);
            int payloadLength = cipherBlob.Length - HEADER_SIZE_BYTES;
            byte[] cipherText = new byte[payloadLength];
            Buffer.BlockCopy(cipherBlob, HEADER_SIZE_BYTES, cipherText, 0, payloadLength);

            using Rfc2898DeriveBytes derive = new(_passPhrase, salt, DERIVATION_ITERATIONS);
            byte[] key = derive.GetBytes(KEY_SIZE_BYTES);

            using RijndaelManaged cipher = new()
            {
                BlockSize = BLOCK_SIZE_BITS,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
            };

            using ICryptoTransform decryptor = cipher.CreateDecryptor(key, iv);
            using MemoryStream buffer = new(cipherText);
            using CryptoStream crypto = new(buffer, decryptor, CryptoStreamMode.Read);
            using MemoryStream output = new();
            crypto.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] GenerateRandomBytes()
        {
            byte[] bytes = new byte[RANDOM_ENTROPY_SIZE_BYTES];
            using RNGCryptoServiceProvider rng = new();
            rng.GetBytes(bytes);
            return bytes;
        }
    }
}
