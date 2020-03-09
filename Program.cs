using System;

namespace petssl_downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            var downloader = new PetSslDownloader();

            // Login
            downloader.Login("email", "changeit");

            // Download Images
            //downloader.DownloadImages();

            // Download Journals
            DateTime startDate = new DateTime(2020,3,2);
            DateTime endDate = new DateTime(2015,12,1);
            //downloader.DownloadJournals(startDate, endDate);

            downloader.ComputeStatistics();
        }

    }
}
