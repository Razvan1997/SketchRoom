using SketchRoom.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SketchRoom.Database
{
    public static class SecureStorage
    {
        private static readonly string folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SketchRoom");

        private static readonly string filePath = Path.Combine(folderPath, "user.dat");

        private static readonly byte[] key = Encoding.UTF8.GetBytes("1234567890ABCDEF"); // 16/24/32 chars
        private static readonly byte[] iv = Encoding.UTF8.GetBytes("ABCDEF1234567890");   // 16 chars

        public static void SaveUser(LocalUser user)
        {
            Directory.CreateDirectory(folderPath);
            var json = JsonSerializer.Serialize(user);
            var encrypted = EncryptString(json);
            File.WriteAllBytes(filePath, encrypted);
        }

        public static LocalUser LoadUser()
        {
            if (!File.Exists(filePath)) return null;

            var encrypted = File.ReadAllBytes(filePath);
            var json = DecryptString(encrypted);
            return JsonSerializer.Deserialize<LocalUser>(json);
        }

        private static byte[] EncryptString(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
                sw.Write(plainText);

            return ms.ToArray();
        }

        private static string DecryptString(byte[] cipherBytes)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        public static void UpdateConnectionStatus(bool connected)
        {
            var user = LoadUser();
            if (user == null) return;
            SaveUser(user);
        }  
    }
}
