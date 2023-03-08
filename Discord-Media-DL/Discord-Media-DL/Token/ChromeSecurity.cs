using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Discord_Media_DL.Token
{
    public static class ChromeSecurity
    {
        #region Private
        private static string Decrypt(byte[] encryptedBytes, byte[] key, byte[] iv)
        {
            GcmBlockCipher cipher = new(new AesEngine());
            AeadParameters parameters = new(new KeyParameter(key), 128, iv, null);

            cipher.Init(false, parameters);

            var plainBytesBuffer = new byte[cipher.GetOutputSize(encryptedBytes.Length)];
            cipher.DoFinal(plainBytesBuffer, cipher.ProcessBytes(encryptedBytes, 0, encryptedBytes.Length, plainBytesBuffer, 0));

            return Encoding.UTF8.GetString(plainBytesBuffer).TrimEnd("\r\n\0".ToCharArray());
        }

        private static void Prepare(byte[] encryptedData, out byte[] nonce, out byte[] ciphertextTag)
        {
            // The first 3 bytes are "v10" as a way to identify the encryption?
            // the first 12 bytes after that v10 are the "nonce" or the "iv"
            // any bytes after that is the encrypted ciphertext 

            nonce = new byte[12];
            ciphertextTag = new byte[encryptedData.Length - 3 - nonce.Length];

            Array.Copy(encryptedData, 3, nonce, 0, nonce.Length);
            Array.Copy(encryptedData, 3 + nonce.Length, ciphertextTag, 0, ciphertextTag.Length);
        }
        #endregion

        /// <summary>
        /// Decrypts the Master Key stored in the "Local State" file.
        /// </summary>
        /// <param name="localStatePath"></param>
        /// <returns></returns>
        public static byte[] GetMasterKey(string localStatePath)
        {
            try
            {
                // This uses the same encryption system as chrome.
                // By searching anything related to "chrome" and "decryption", 
                // you can find multiple articles explaining their process.

                dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(localStatePath));
                string raw_encrypted_key = json.os_crypt.encrypted_key;

                var raw_decoded_encrypted_key = Convert.FromBase64String(raw_encrypted_key);

                // we skip 5 bytes as this equals to the string "DAPI".
                // DAPI is the Data Protection API of Windows. 
                // I think they write it there to identify if its encrypted using DAPI
                // You can only decrypt it on the User's Device as it uses randomly generated data 
                // which is local to the user's device or user-account. 
                var decoded_encrypted_key = raw_decoded_encrypted_key.Skip(5).ToArray();

                return ProtectedData.Unprotect(decoded_encrypted_key, null, DataProtectionScope.CurrentUser);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return new byte[0];
        }

        /// <summary>
        /// Decrypts any value encrypted by the Chrome-Encryption-System.
        /// </summary>
        /// <param name="encryptedToken"></param>
        /// <param name="masterKey"></param>
        /// <returns></returns>
        public static string DecryptValue(byte[] encryptedToken, byte[] masterKey)
        {
            if (encryptedToken.Length == 0 || encryptedToken.Length == 0)
                return null;

            // As with the master key, this is basically what chrome does with passwords & cookies

            try
            {

                Prepare(encryptedToken, out var nonce, out var ciphertextTag);

                return Decrypt(ciphertextTag, masterKey, nonce);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return null;
        }
    }
}
