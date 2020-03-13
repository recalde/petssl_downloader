using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using petssl_downloader.Models;

namespace petssl_downloader
{
    public class PetSslDownloader
    {
        private readonly WebsiteDriver website;
        private readonly Configuration configuration;
        private readonly JsonSerializerOptions jsonOptions;

        public PetSslDownloader(Configuration configuration)
        {
            this.configuration = configuration;
            this.website = new WebsiteDriver(configuration);
            this.jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                IgnoreNullValues = true
            };

            // Create Output Directories
            if (!Directory.Exists(configuration.ImageDirectory))
            {
                Directory.CreateDirectory(configuration.ImageDirectory);
            }
            if (!Directory.Exists(configuration.JournalDirectory))
            {
                Directory.CreateDirectory(configuration.JournalDirectory);
            }
        }

        public void Login(string username, string password)
        {
            website.Home();
            website.Login(username, password);
            foreach (var cookie in website.GetCookies())
            {
                Console.WriteLine($"-Cookie[{cookie.Name}] = \"{cookie.Value}\"");
            }
        }

        public void DownloadImages(int numberOfPages)
        {
            var imageList = new List<string>();

            // Get the image hrefs for s3
            for (int pageNumber = 0; pageNumber <= numberOfPages; pageNumber++)
            {
                string images = website.Images(pageNumber);

                var doc = new HtmlDocument();
                doc.LoadHtml(images);

                var imageNodes = doc.DocumentNode.SelectNodes("//a/img");
                if (imageNodes != null)
                {
                    foreach (var imageNode in imageNodes)
                    {
                        var imageSrc = imageNode.GetAttributeValue("src", "");
                        if (imageSrc.Contains("/images/image-size.php/"))
                        {
                            int imgLoc = 52;
                            int queryLoc = imageSrc.IndexOf('?');
                            var imageObjectId = imageSrc.Substring(imgLoc, queryLoc - imgLoc);
                            imageList.Add(imageObjectId);
                        }

                    }
                }
            }

            // Download them in parallel
            Parallel.ForEach(imageList, new ParallelOptions { MaxDegreeOfParallelism = configuration.ImageThreads },
            (image) =>
            {
                website.DownloadImage(image);
            });
        }

        public void DownloadJournals(DateTime startDate, DateTime endDate)
        {
            // This method will log progress in case it does not complete to this file
            string logFilePath = Path.Combine(configuration.JournalDirectory, "log.json");

            List<DateTime> dates = new List<DateTime>();
            while (startDate > endDate)
            {
                startDate = startDate.AddDays(-7);
                dates.Add(startDate);
            }

            var journalList = new List<Journal>();
            Object listLock = new Object();
            var foreachResult = Parallel.ForEach(dates, new ParallelOptions { MaxDegreeOfParallelism = configuration.JournalThreads }, (date) =>
            {
                var schedule = website.Schedule(date);
                var doc = new HtmlDocument();
                doc.LoadHtml(schedule);
                var completedJournals = doc.DocumentNode.SelectNodes("//div[@class='c_i sb_Completed']");
                if (completedJournals == null) return;
                foreach (var completedJournal in completedJournals)
                {
                    string journalTimestamp = completedJournal.SelectNodes("small").FirstOrDefault().InnerHtml;
                    var viewJournalLinks = completedJournal.SelectNodes("a[starts-with(@href, '/view-journal?')]");
                    var href = viewJournalLinks?.FirstOrDefault()?.GetAttributeValue("href", "");
                    if (href != null)
                    {
                        var journal = DownloadJournal(href, journalTimestamp);
                        if (journal != null)
                        {
                            lock (listLock)
                            {
                                journalList.Add(journal);
                            }
                            string json = JsonSerializer.Serialize(journal, jsonOptions);
                            string journalPath = Path.Combine(configuration.JournalDirectory, $"{journal.Date}.json");
                            System.IO.File.WriteAllText(journalPath, json);
                            json = json + ",\n";
                            System.IO.File.AppendAllText(logFilePath, json);
                        }
                    }
                }
            });
            bool isCompleted = foreachResult.IsCompleted;

            string allJournalJson = JsonSerializer.Serialize(journalList.OrderBy(j => j.Date), jsonOptions);
            string listFilePath = Path.Combine(configuration.JournalDirectory, "list.json");
            System.IO.File.AppendAllText(listFilePath, allJournalJson);
        }

        public Journal DownloadJournal(string journalUrl, string journalTimestamp)
        {
            string journal = website.Journal(journalUrl);
            if (journal == null || journal.Length == 0) return null;
            var journalDoc = new HtmlDocument();
            journalDoc.LoadHtml(journal);

            var stripeNodes = journalDoc.DocumentNode.SelectNodes("//div[contains(@class, 'row') and contains(@class, 'stripe')]");

            var j = new Journal();

            j.Time = journalTimestamp;

            DateTime journalDate = default(DateTime);

            foreach (var stripeNode in stripeNodes)
            {
                var labelNode = stripeNode.SelectNodes("label//strong")?.FirstOrDefault();
                var divNode = stripeNode.SelectNodes("div")?.FirstOrDefault();
                switch (labelNode.InnerText)
                {
                    case "Date":
                        if (DateTime.TryParse(divNode.InnerText, out journalDate))
                        {
                            j.Date = journalDate.ToString("yyyy-MM-dd");
                        }
                        break;
                    case "Service":
                        j.Service = divNode.InnerText;
                        break;
                    case "Pets":
                        j.Pets = divNode.InnerText;
                        break;
                    case "Pet Sitter":
                        j.PetSitter = divNode.InnerText;
                        break;
                    case "Poop":
                        j.Poop = divNode.InnerText;
                        break;
                    case "Pee":
                        j.Pee = divNode.InnerText;
                        break;
                    case "Meal":
                        j.Meal = divNode.InnerText;
                        break;
                    case "Other Actions":
                        j.OtherActions = divNode.InnerHtml.Replace("<br>", ", ");
                        break;
                    default:
                        Console.WriteLine($"UNKNOWN ELEMENT: {stripeNode.OuterHtml}");
                        break;
                }
            }

            j.Comments = new List<Comment>();
            var commentNodes = journalDoc.DocumentNode.SelectNodes("//div[@class='comments-item']");
            if (commentNodes != null)
            {
                foreach (var commentNode in commentNodes)
                {
                    var c = new Comment();
                    c.User = commentNode.SelectNodes("div//strong").FirstOrDefault()?.InnerHtml;
                    c.CommentText = commentNode.SelectNodes("div")[1].InnerHtml.Replace("<br>\n", "\n");
                    j.Comments.Add(c);
                }
            }

            j.Images = new List<string>();
            var imageNodes = journalDoc.DocumentNode.SelectNodes("//img");
            foreach (var imageNode in imageNodes)
            {
                var imageSrc = imageNode.GetAttributeValue("src", "");
                var imageWidth = imageNode.GetAttributeValue("width", "");
                // Pet Profile Picture is width="35" height="35"
                if (imageSrc.Contains("/images/image-size.php/") && imageWidth != "35")
                {
                    int imgLoc = 52;
                    int queryLoc = imageSrc.IndexOf('?');
                    var imageObjectId = imageSrc.Substring(imgLoc, queryLoc - imgLoc);
                    j.Images.Add(imageObjectId);
                    string imagePath = Path.Combine(configuration.ImageDirectory, imageObjectId);

                    if (!File.Exists(imagePath))
                    {
                        website.DownloadImage(imageObjectId);
                    }

                    if (File.Exists(imagePath))
                    {
                        File.SetLastWriteTime(imagePath, journalDate);
                        File.SetCreationTime(imagePath, journalDate);
                    }
                }
            }
            return j;
        }
        public void ComputeStatistics()
        {
            string listFilePath = Path.Combine(configuration.JournalDirectory, "list.json");
            var journalList = JsonSerializer.Deserialize<List<Journal>>(listFilePath, jsonOptions);

            var years = journalList.GroupBy(j => DateTime.Parse(j.Date).ToString("yyyy"));
            foreach (var year in years.OrderBy(y => y.Key))
            {
                var petSitters = year.GroupBy(j => j.PetSitter);
                foreach (var petSitter in petSitters.OrderByDescending(p => p.Count()))
                {
                    int walks = petSitter.Count();
                    int comments = petSitter.SelectMany(j => j.Comments).Count();
                    int images = petSitter.SelectMany(j => j.Images).Count();

                    Console.WriteLine("[{0}]\t{1}\t{2} Walks\t{3} Comments\t{4} Images", year.Key, petSitter.Key, walks, comments, images);

                }
            }
        }
    }
}