using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DecoraCsycles.HelperClasses
{
    public class Encryption
    {
        public void EncryptFile(string inputFile, string outputFile)
        {
            try
            {
                string password = @"!?FZ???5"; // Your Key Here
                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(password);

                string cryptFile = outputFile;
                using (FileStream fsCrypt = new FileStream(cryptFile, FileMode.Create))
                {
                    RijndaelManaged RMCrypto = new RijndaelManaged();

                    using (CryptoStream cs = new CryptoStream(fsCrypt, RMCrypto.CreateEncryptor(key, key), CryptoStreamMode.Write))
                    {
                        using (FileStream fsIn = new FileStream(inputFile, FileMode.Open))
                        {
                            int data;
                            while ((data = fsIn.ReadByte()) != -1)
                                cs.WriteByte((byte)data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
        public bool DecryptFile(string inputFile, string outputFile)
        {
            try
            {
                string password = @"!?FZ???5"; // Your Key Here

                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(password);

                FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                CryptoStream cs = new CryptoStream(fsCrypt,
                    RMCrypto.CreateDecryptor(key, key),
                    CryptoStreamMode.Read);

                // Error directory is not created when temp/decora is opened in explorer so need to check and create even though it is written in app.cs also 
                var dir = Path.GetDirectoryName(outputFile);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                ////

                FileStream fsOut = new FileStream(outputFile, FileMode.Create);

                int data;
                while ((data = cs.ReadByte()) != -1)
                    fsOut.WriteByte((byte)data);

                fsOut.Close();
                cs.Close();
                fsCrypt.Close();
                return true;
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine(ex);
                return false;


            }
        }


      
        public static MemoryStream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        public static string EncryptStringToBytes(string plainText)
        {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                return string.Empty;

            string password = @"!?FZ???5"; // Your Key Here
            UnicodeEncoding UE = new UnicodeEncoding();
            byte[] key = UE.GetBytes(password);

            byte[] encrypted;
            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = key;
                rijAlg.IV = key;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (MemoryStream fsIn = GenerateStreamFromString(plainText))
                        {
                            int data;
                            while ((data = fsIn.ReadByte()) != -1)
                                csEncrypt.WriteByte((byte)data);
                        }
                    }

                    encrypted = msEncrypt.ToArray();
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        public static string DecryptBytesToString(string encryptstring)
        {
            byte[] encryptData = Convert.FromBase64String(encryptstring);
            // Check arguments. 
            if (encryptData == null || encryptData.Length <= 0)
                return string.Empty;

            string output;
            string password = @"!?FZ???5"; // Your Key Here
            UnicodeEncoding UE = new UnicodeEncoding();
            byte[] key = UE.GetBytes(password);


            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = key;
                rijAlg.IV = key;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream(encryptData))
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            int data;
                            while ((data = csEncrypt.ReadByte()) != -1)
                                ms.WriteByte((byte)data);

                            output = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                        }

                    }
                }
            }
            return output;
        }
    }
}
