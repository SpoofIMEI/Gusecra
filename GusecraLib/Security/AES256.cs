using System.Security.Cryptography;
using System.Text;

namespace Gusecra.Security;

public static class Hash {
    /// <summary>
    /// Creates a sha256 hash of the password data.
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public static byte[] Sha256Hash(byte[] plainText) {
      using (SHA256 s = SHA256.Create()) {
        byte[] bytes = s.ComputeHash(plainText);
        return bytes;
      }
    }
}

public static class AES256
{

    /// <summary>
    /// Simple AES256 encryption. In goes plaintext, key and iv, and
    /// out comes the ciphertext bytes.
    /// </summary>
    /// <param name="plainText"></param>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    public static byte[] Encrypt(byte[] plainText, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Hash.Sha256Hash(key);
            aes.IV = Hash.Sha256Hash(iv)[..16];
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        cs.Write(plainText);
                    }
                    return ms.ToArray();
                }
            }
        }
    }

    /// <summary>
    /// Simple AES256 decryption. In goes ciphertext, key and iv, and
    /// out comes the plaintext bytes.
    /// </summary>
    /// <param name="cipherText"></param>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    public static byte[] Decrypt(byte[] cipherText, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Hash.Sha256Hash(key);
            aes.IV = Hash.Sha256Hash(iv)[..16];
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream(cipherText))
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (MemoryStream memstream = new MemoryStream())
                    {
                        cs.CopyTo(memstream);
                        byte[] data = memstream.ToArray();
                        return data;
                    }
                }
            }
        }
    }
}
  