using System;

namespace petssl_downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("username: ");
            string username = Console.ReadLine();
            Console.Write("password: ");
            string password = ReadPassword();

            // Your local machine and website configuration
            var configuration = new Configuration
            {
                ImageThreads = 5,
                JournalThreads = 25,
                ImageDirectory = @"C:\git\petssl-downloader\download\",
                JournalDirectory = @"C:\git\petssl-downloader\journals\",
                WebsiteUri = new Uri("https://hshpetcare.petssl.com")
            };

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

        static string ReadPassword()
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
                    else if(key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);
            return pass;
        }

    }
}
