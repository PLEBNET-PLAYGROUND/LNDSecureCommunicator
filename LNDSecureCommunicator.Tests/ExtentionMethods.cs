using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LNDSecureCommunicator.Tests
{
    public static class ExtentionMethods
    {
        //public static (byte[] data, byte[] iv) EncryptStringToAesBytes(this byte[] ClearData, byte[] Key, byte[] IV)
        //{
        //    // Check arguments.
        //    if (ClearData.Length <= 0)
        //        throw new ArgumentNullException("ClearData");
        //    if (Key == null || Key.Length <= 0)
        //        throw new ArgumentNullException("Key");
        //    byte[] encrypted;
        //    // Create an Aes object
        //    // with the specified key and IV.
        //    using (Aes aesAlg = Aes.Create())
        //    {
        //        aesAlg.Key = Key;
        //        if (IV != null) 
        //            IV = aesAlg.IV;
        //        aesAlg.Mode = CipherMode.CBC;
        //        // Create an encryptor to perform the stream transform.
        //        IV = aesAlg.IV;
        //        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        //        // Create the streams used for encryption.
        //        using (MemoryStream msEncrypt = new MemoryStream())
        //        {
        //            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        //            {
        //                csEncrypt.Write(ClearData);
        //            }
        //            encrypted = msEncrypt.ToArray();
        //        }
        //    }

        //    // Return the encrypted bytes from the memory stream.
        //    return (encrypted, IV);
        //}

        //public static byte[] DecryptStringFromBytesAes(this byte[] CipherData, byte[] Key, byte[] IV)
        //{
        //    // Check arguments.
        //    if (CipherData.Length <= 0)
        //        throw new ArgumentNullException("CipherData");
        //    if (Key == null || Key.Length <= 0)
        //        throw new ArgumentNullException("Key");
        //    if (IV == null || IV.Length <= 0)
        //        throw new ArgumentNullException("IV");

        //    // Create an Aes object
        //    // with the specified key and IV.
        //    using (Aes aesAlg = Aes.Create())
        //    {
        //        aesAlg.Key = Key;
        //        aesAlg.IV = IV;
        //        aesAlg.Mode = CipherMode.CBC;
        //        // Create a decryptor to perform the stream transform.
        //        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        //        // Create the streams used for decryption.
        //        using (MemoryStream msDecrypt = new MemoryStream(CipherData))
        //        {
        //            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
        //            {
        //                return csDecrypt.ReadFully();
        //            }
        //        }
        //    }
        //}
    }
}
