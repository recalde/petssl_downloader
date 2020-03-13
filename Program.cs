using System;

namespace petssl_downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            // Your local machine and website configuration
            var configuration = new Configuration
            {
                ImageThreads = 5,
                JournalThreads = 15,
                ImageDirectory = @"C:\git\petssl_downloader\download\",
                JournalDirectory = @"C:\git\petssl_downloader\journals\",
                WebsiteUri = new Uri("https://hshpetcare.petssl.com")
            };

            var downloader = new PetSslDownloader(configuration);

            // Login
            downloader.Login("login", "password");

            // Download Images
            //downloader.DownloadImages(24);

            // Download Journals
            DateTime startDate = new DateTime(2020, 3, 16);
            DateTime endDate = new DateTime(2015, 12, 1);
            downloader.DownloadJournals(startDate, endDate);

            downloader.ComputeStatistics();
        }

    }
}
