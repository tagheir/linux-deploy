using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace azureFileWatch
{
    class Program
    {

        private static string StorageConnectionString { get; set; }
        private static string Containerfiles { get; set; }
        private static string Containerlogs { get; set; }
        private static string PartnersFolder { get; set; }
        static void Init()
        {

            var builder = new ConfigurationBuilder();
            builder.AddUserSecrets<Program>();

            var configuration = builder.Build();
            ;
            var tenantId = configuration.GetSection("tenantId")?.Value;
            KeyVaultHelper.TenantId = tenantId ?? ConfigurationManager.AppSettings["tenantId"];
            ConfigurationManager.AppSettings.Set("tenantId", "hisham");
            Console.WriteLine($"KeyVaultHelper.TenantId => {KeyVaultHelper.TenantId}");

            var clientId = configuration.GetSection("clientId")?.Value;
            KeyVaultHelper.ClientId = clientId ?? ConfigurationManager.AppSettings["clientId"];
            Console.WriteLine($"KeyVaultHelper.ClientId => {KeyVaultHelper.ClientId}");

            var clientSecret = configuration.GetSection("clientSecret")?.Value;
            KeyVaultHelper.ClientSecret = clientSecret ?? ConfigurationManager.AppSettings["clientSecret"];
            Console.WriteLine($"KeyVaultHelper.ClientSecret => {KeyVaultHelper.ClientSecret}");

            var keyVaultUrl = configuration.GetSection("keyVaultUrl")?.Value;
            KeyVaultHelper.KeyVaultUrl = keyVaultUrl ?? ConfigurationManager.AppSettings["keyVaultUrl"];
            Console.WriteLine($"KeyVaultHelper.KeyVaultUrl => {KeyVaultHelper.KeyVaultUrl}");

            StorageConnectionString = KeyVaultHelper.GetSecret("storageConnectionString");
            Console.WriteLine($"StorageConnectionString => {StorageConnectionString}");

            Containerfiles = KeyVaultHelper.GetSecret("containerfiles");
            Console.WriteLine($"Containerfiles => {Containerfiles}");


            Containerlogs = KeyVaultHelper.GetSecret("containerlogs");
            Console.WriteLine($"Containerlogs => {Containerlogs}");

            PartnersFolder = KeyVaultHelper.GetSecret("partnersFolder");
            Console.WriteLine($"PartnersFolder => {PartnersFolder}");

        }

        static void Main(string[] args)
        {
            Init();
            return;
            if (isWindows())
            {
                PartnersFolder = @"C:\Filewatcher_nah_per\Monitor\";
            }

            try
            {
                var watcher = new FileSystemWatcher
                {
                    Path = PartnersFolder,
                    IncludeSubdirectories = true
                };

                // Create a new FileSystemWatcher and set its properties.
                using (watcher)
                {

                    // Watch for changes in LastAccess and LastWrite times, and
                    // the renaming of files or directories.
                    // watcher.NotifyFilter = NotifyFilters.LastWrite;
                    // watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
                    watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    // Only watch text files.
                    //watcher.Filter = "*.txt"; 
                    // Add event handlers.
                    //watcher.Changed += OnChanged;
                    watcher.Created += OnChanged;

                    // Begin watching.
                    watcher.EnableRaisingEvents = true;

                    // Wait for the user to quit the program.
                    Console.WriteLine("Press 'q' to quit the sample.");
                    while (Console.Read() != 'q') ;
                }
            }
            catch (Exception ex)
            {
                logExceptions("NA", ex.Message);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }

        static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith(".swp")) return;
            if (e.FullPath.Contains("archive")) return;
            Console.ForegroundColor = ConsoleColor.DarkYellow; ;
            Console.WriteLine($"*** EVENT :  {e.FullPath} , {e.ChangeType}");
            Console.ResetColor();
            Upload(e.FullPath);

        }
        static void Upload(string srcPath)
        {

            string srcFullPath = srcPath;
            string partner = "";
            string filename = "";
            try
            {
                if (isWindows())
                {
                    if (!PartnersFolder.EndsWith("\\")) PartnersFolder += "\\";
                    srcPath = srcPath.Replace(PartnersFolder, "");
                    Console.WriteLine(" srcPath =" + srcPath);
                    var pairs = srcPath.Split('\\').ToList();
                    if (pairs.Count != 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" Invalid Path Name . path should contain /partername/uploads/filename");
                        Console.WriteLine(" Please check partner folder structure");
                        Console.ResetColor();
                        return;

                    }
                    if (pairs.Count >= 1)
                    {
                        partner = pairs[0];
                    }
                    if (pairs.Count >= 1)
                    {
                        filename = pairs[2];
                    }

                }
                else
                {
                    if (!PartnersFolder.EndsWith("/")) PartnersFolder += "/";
                    srcPath = srcPath.Replace(PartnersFolder, "");
                    Console.WriteLine(" srcPath =" + srcPath);
                    var pairs = srcPath.Split('/').ToList();
                    if (pairs.Count != 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" Invalid Path Name . path should contain /partername/uploads/filename");
                        Console.WriteLine(" Please check partner folder structure");
                        Console.ResetColor();
                        return;
                    }
                    if (pairs.Count >= 1)
                    {
                        partner = pairs[0];
                    }
                    if (pairs.Count >= 1)
                    {
                        filename = pairs[2];
                    }
                }


                BlobServiceClient blobServiceClient = new BlobServiceClient(StorageConnectionString);
                Console.WriteLine($"BlobServiceClient Created Successfully");

                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(Containerfiles);
                Console.WriteLine($"BlobContainerClient Created Successfully");

                string blobfullName = partner + "/uploads/" + filename;
                Console.WriteLine($"blobfullName = {blobfullName}");

                BlobClient blobClient = containerClient.GetBlobClient(blobfullName);
                Console.WriteLine($"BlobClient Created Successfully");

                Console.ForegroundColor = ConsoleColor.DarkYellow; ;
                Console.WriteLine($"*** Processing files");
                Console.WriteLine(" Source File : " + PartnersFolder);
                Console.WriteLine(" Dest blob : " + blobfullName);
                Console.WriteLine(" *************** ");


                Console.WriteLine($"*** Deleteing if blob exists ... ");
                blobClient.DeleteIfExists();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success | File Deleteing if blob exists ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.DarkYellow; ;
                Console.WriteLine($"*** Uploading to blob ");
                Console.ResetColor();
                blobClient.Upload(srcFullPath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success | File upload the blob ");
                Console.ResetColor();
                //File.Copy(path, "/datadrive/EHCMCDC/uploads" + dest);

                Console.ForegroundColor = ConsoleColor.DarkYellow; ;
                Console.WriteLine($"*** Deleteing local file ");
                File.Delete(srcFullPath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success | Deleteing local file ");
                Console.ResetColor();

                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkYellow; ;
                Console.WriteLine("Upload process completed successfully - File : " + blobfullName);
                Console.ResetColor();
                logmessage(partner, "Upload process completed successfully - file : " + blobfullName + " - partner - " + partner);
            }
            catch (Exception ex)
            {
                logExceptions(partner, ex.Message);
                Console.ForegroundColor = ConsoleColor.Red; ;
                Console.WriteLine("Failed | File Upload Azure => " + srcFullPath);
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }

        private static bool isWindows()
        {
            bool _isWindows = false;
            try
            {
                _isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }
            catch { }

            return _isWindows;
        }
        private static void logExceptions(string partner, string message)
        {
            string blobfullName = "logs-error/" + getHHSNowDateTime() + "_" + partner + "_error.txt";
            logdata(partner, message, blobfullName);
        }

        private static void logmessage(string partner, string message)
        {
            string blobfullName = DateTime.Now.Year.ToString() + "/" + getHHSNowDateTime() + "_" + partner + ".txt";
            logdata(partner, message, blobfullName);
        }
        private static void logdata(string partner, string message, string blobfullName)
        {
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(StorageConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(Containerlogs);
                BlobClient blobClient = containerClient.GetBlobClient(blobfullName);
                if (!blobClient.Exists())
                {

                    using (Stream stream = new MemoryStream(GetBytesfromString(message)))
                    {
                        blobClient.Upload(stream);
                    }
                }
            }
            catch (Exception ex)
            {

                Console.ForegroundColor = ConsoleColor.Red; ;
                Console.WriteLine("Failed to log exception - check of log container {0} exists ", Containerlogs);
                Console.WriteLine(ex);
                Console.ResetColor();
            }

        }
        private static string getHHSNowDateTime()
        { // type name = "usnyc" description = "New York, United States" alias = "America/New_York US/Eastern" />

            TimeZoneInfo easternTimeZone;
            try
            {
                easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch
            {
                easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("US/Eastern");
            }

            DateTime easternDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternTimeZone);
            Console.WriteLine(easternDateTime);
            return string.Format("{0:yyyy_MMM_dd_hhmmss}", easternDateTime);
        }

        public static DateTime getHHSDateTime()
        {
            var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime easternDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternTimeZone);
            return easternDateTime;
        }

        public static byte[] GetTestDataBytes()
        {
            string test = "Testing " + string.Format("{0:_yyyy-MM-dd_hh-mm-ss}", DateTime.Now) + ".txt";
            byte[] byteArray = Encoding.ASCII.GetBytes(test);
            return byteArray;
        }
        public static byte[] GetBytesfromString(string message)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(message);
            return byteArray;
        }
        public static void Upload_TestFile()
        {
            string partner = "DEFAS";
            string message = "No Message test";
            logExceptions(partner, message);
        }
    }
}
