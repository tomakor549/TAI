﻿using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace TaiMvc.Models
{
    public class FileEncryptionOperations
    {
        [DllImport("KERNEL32.DLL", EntryPoint = "RtlZeroMemory")]
        public static extern bool ZeroMemory(IntPtr Destination, int Length);
        public static byte[] GenerateSalt()
        {
            byte[] data = new byte[32];
            using (RandomNumberGenerator randomNumber = RandomNumberGenerator.Create())
            {
                randomNumber.GetBytes(data);
            }
            return data;
        }

        public static void FileEncrypt(string filePath, string password)
        {
            byte[] salt = GenerateSalt();
            byte[] passwords = Encoding.UTF8.GetBytes(password);
            Aes AES = Aes.Create();
            AES.KeySize = 256;//aes 256 bit encryption c#
            AES.BlockSize = 128;//aes 128 bit encryption c#
            AES.Padding = PaddingMode.PKCS7;
            var key = new Rfc2898DeriveBytes(passwords, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Mode = CipherMode.CFB;
            using (FileStream fsCrypt = new FileStream(filePath + ".aes", FileMode.Create))//bazowo 4096 bajtów na rozmiar buffera
            {
                fsCrypt.Write(salt, 0, salt.Length);
                using (CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (FileStream fsIn = new FileStream(filePath, FileMode.Open))
                    {
                        byte[] buffer = new byte[1048576];
                        int read;
                        while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            cs.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }

        public static void FileDecrypt(string inputFilePath, string outputFilePath, string password)
        {
            byte[] passwords = Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[32];
            using (FileStream fsCrypt = new(inputFilePath, FileMode.Open))
            {
                fsCrypt.Read(salt, 0, salt.Length);
                Aes AES = Aes.Create();
                AES.KeySize = 256;//aes 256 bit encryption c#
                AES.BlockSize = 128;//aes 128 bit encryption c#
                var key = new Rfc2898DeriveBytes(passwords, salt, 50000);
                AES.Key = key.GetBytes(AES.KeySize / 8);
                AES.IV = key.GetBytes(AES.BlockSize / 8);
                AES.Padding = PaddingMode.PKCS7;
                AES.Mode = CipherMode.CFB;
                using (CryptoStream cryptoStream = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (FileStream fsOut = new FileStream(outputFilePath, FileMode.Create))
                    {
                        int read;
                        byte[] buffer = new byte[1048576];
                        while ((read = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fsOut.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }
    }
}