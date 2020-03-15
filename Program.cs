using System;
using System.IO;
using System.Threading.Tasks;

namespace petssl_downloader
{
    /// <summary>
    /// This example uses the discovery API to list all APIs in the discovery repository.
    /// https://developers.google.com/discovery/v1/using.
    /// <summary>
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("petssl_downloader");
            Console.WriteLine("=================");
            try
            {
                new Program().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private async Task Run()
        {
            Console.Write("username: ");
            string username = Console.ReadLine();
            Console.Write("password: ");
            string password = ReadPassword();

            string rootPath = Directory.GetCurrentDirectory();

            // Your local machine and website configuration
            var configuration = new Configuration
            {
                ImageThreads = 5,
                JournalThreads = 25,
                ImageDirectory = Path.Combine(rootPath, "images"),
                JournalDirectory = Path.Combine(rootPath, "journals"),
                WebsiteUri = new Uri("https://hshpetcare.petssl.com"),
                ImageUri = new Uri("https://s3.amazonaws.com/petssl.com/hshpetcare/images/uploads/Content/")
            };

            // Output Configuration
            Console.Write("\n\n");
            Console.WriteLine($"Images Output: {configuration.ImageDirectory}");
            Console.WriteLine($"Journals Output: {configuration.JournalDirectory}");
            Console.Write("\n");
            Console.WriteLine("Running Downloader!");
            Console.Write("\n");

            var downloader = new PetSslDownloader(configuration);

            // Login
            downloader.Login(username, password);

            // Download Images
            downloader.DownloadImages(24);

            // Download Journals
            DateTime startDate = new DateTime(2020, 3, 16);
            DateTime endDate = new DateTime(2014, 1, 1);
            downloader.DownloadJournals(startDate, endDate);

            downloader.ComputeStatistics();
        }

        private string ReadPassword()
        {
            string pass = "";
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, (pass.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);
            return pass;
        }
    }
}
