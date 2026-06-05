using GXUploader.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GXUploader.Helper
{
    public class CredentialManager
    {

        private static readonly string FilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "credentials.txt");

        public static UserCredential LoadCredentials()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    var defaultCreds = new UserCredential
                    {
                        Username = "sysadmin",
                        Password = "sysadmin"
                    };

                    SaveCredentials(defaultCreds);

                    return defaultCreds;
                }

                string json = File.ReadAllText(FilePath);

                return JsonSerializer.Deserialize<UserCredential>(json);
            }
            catch
            {
                return new UserCredential
                {
                    Username = "admin",
                    Password = "1234"
                };
            }
        }

        public static void SaveCredentials(UserCredential creds)
        {
            string json = JsonSerializer.Serialize(creds, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(FilePath, json);
        }

    }
}
