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
        private readonly JsonSerializerOptions jsonOptions;

        public PetSslDownloader()
        {
            website = new WebsiteDriver();

            jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                IgnoreNullValues = true
            };
        }

        public void Login(string username, string password)
        {
            website.Home();
            website.Login(username, password);
            var cookies = website.GetCookies();
            // TODO verify login cookies
        }

        public void DownloadImages()
        {
            var imageList = new List<string>();

            // Get the image hrefs for s3
            for(int pageNumber = 0; pageNumber <= 24; pageNumber++)
            {
                string images = website.Images(pageNumber);

                var doc = new HtmlDocument();
                doc.LoadHtml(images);

                var imageNodes = doc.DocumentNode.SelectNodes("//a/img");
                if (imageNodes != null)
                {
                    foreach(var imageNode in imageNodes)
                    {
                        var imageSrc = imageNode.GetAttributeValue("src", "");
                        if (imageSrc.StartsWith("https://hshpetcare.petssl.com/images/image-size.php/"))
                        {
                            int imgLoc = 52;
                            int queryLoc = imageSrc.IndexOf('?');
                            var imageObjectId = imageSrc.Substring(imgLoc, queryLoc-imgLoc);
                            imageList.Add(imageObjectId);
                        }
                        
                    }
                }
            }

            // Download them in parallel
            Parallel.ForEach(imageList, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                (image) => {
                website.DownloadImage(image);
            });
        }

        public void DownloadJournals(DateTime startDate, DateTime endDate)
        {
            List<DateTime> dates = new List<DateTime>();
            while(startDate > endDate)
            {
                startDate = startDate.AddDays(-7);
                dates.Add(startDate);
            }

            var journalList = new List<Journal>();
            Object listLock = new Object();   
            var foreachResult = Parallel.ForEach(dates, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (date) => 
            {
                var schedule = website.Schedule(date);
                var doc = new HtmlDocument();
                doc.LoadHtml(schedule);

                var linkNodes = doc.DocumentNode.SelectNodes("//a");

                if (linkNodes != null)
                {
                    foreach(var linkNode in linkNodes)
                    {
                        var href = linkNode.GetAttributeValue("href", "");
                        if (href.StartsWith("/view-journal?") && !href.EndsWith("#comments"))
                        {
                            string journal = website.Journal(href);
                            if (journal == null || journal.Length == 0) continue;
                            var journalDoc = new HtmlDocument();
                            journalDoc.LoadHtml(journal);

                            var stripeNodes = journalDoc.DocumentNode.SelectNodes("//div[contains(@class, 'row') and contains(@class, 'stripe')]");

                            var j = new Journal();
                            lock(listLock)
                            {
                                journalList.Add(j);
                            }

                            j.Date = stripeNodes[0].ChildNodes[1].ChildNodes[0].InnerHtml;
                            var journalDate = DateTime.Parse(j.Date);
                            j.Date = journalDate.ToString("yyyy-MM-dd");

                            j.Service = stripeNodes[1].ChildNodes[1].ChildNodes[0].InnerHtml;
                            j.Pet = stripeNodes[2].ChildNodes[1].ChildNodes[0].InnerHtml;
                            j.PetSitter = stripeNodes[3].ChildNodes[1].ChildNodes[0].InnerHtml;
                            
                            if (stripeNodes.Count <= 4)
                            {
                                j.Pee = "No";
                                j.Poop = "No";
                                j.OtherActions = "";
                            }
                            else if (stripeNodes.Count == 5)
                            {
                                j.Poop = "No";
                                j.Pee = stripeNodes[4].ChildNodes[1].ChildNodes[0].InnerHtml;
                                j.OtherActions = "";      
                            }
                            else if (stripeNodes.Count == 6)
                            {
                                j.Poop = "No";
                                j.Pee = stripeNodes[4].ChildNodes[1].ChildNodes[0].InnerHtml;
                                j.OtherActions = stripeNodes[5].ChildNodes[1].ChildNodes[0].InnerHtml;                            
                            }
                            else if (stripeNodes.Count == 7)
                            {
                                j.Poop = stripeNodes[4].ChildNodes[1].ChildNodes[0].InnerHtml;
                                j.Pee = stripeNodes[5].ChildNodes[1].ChildNodes[0].InnerHtml;
                                j.OtherActions = stripeNodes[6].ChildNodes[1].ChildNodes[0].InnerHtml;
                            }
                            else if (stripeNodes.Count == 8)
                            {
                                j.Poop = stripeNodes[4].ChildNodes[1].ChildNodes[0].InnerHtml;
                                j.Pee = stripeNodes[5].ChildNodes[1].ChildNodes[0].InnerHtml;
                                j.Meal = stripeNodes[6].ChildNodes[1].ChildNodes[0].InnerHtml;
                                j.OtherActions = stripeNodes[7].ChildNodes[1].ChildNodes[0].InnerHtml;
                            }
                            else
                            {
                                throw new Exception("stripe node index 8");
                            }

                            j.Comments = new List<Comment>();
                            var commentNodes = journalDoc.DocumentNode.SelectNodes("//div[@class='comments-item']");
                            if (commentNodes != null)
                            {
                                foreach(var commentNode in commentNodes)
                                {
                                    var c = new Comment();
                                    if (commentNode.ChildNodes.Count >= 2 && commentNode.ChildNodes[1].ChildNodes.Count >= 2)
                                    {
                                        c.User = commentNode.ChildNodes[1].ChildNodes[1].InnerHtml;
                                    }
                                    if (commentNode.ChildNodes.Count >= 3 && commentNode.ChildNodes[2].ChildNodes.Count >= 2)
                                    {
                                        c.CommentText = commentNode.ChildNodes[2].InnerHtml;
                                    }
                                    j.Comments.Add(c);
                                }
                            }

                            j.Images = new List<string>();
                            var imageNodes = journalDoc.DocumentNode.SelectNodes("//img");
                            if (imageNodes != null)
                            {
                                foreach(var imageNode in imageNodes)
                                {
                                    var imageSrc = imageNode.GetAttributeValue("src", "");
                                    if (imageSrc.StartsWith("https://hshpetcare.petssl.com/images/image-size.php/"))
                                    {
                                        int imgLoc = 52;
                                        int queryLoc = imageSrc.IndexOf('?');
                                        var imageObjectId = imageSrc.Substring(imgLoc, queryLoc-imgLoc);
                                        j.Images.Add(imageObjectId);
                                        string localPath = @"C:\git\petssl-downloader\download\" + imageObjectId;
                                        if (File.Exists(localPath))
                                        {
                                            File.SetLastWriteTime(localPath, journalDate);
                                            File.SetCreationTime(localPath, journalDate);
                                        }
                                    }


                                }
                            }
                            string json = JsonSerializer.Serialize(j, jsonOptions);
                            string journalPath = @"C:\git\petssl-downloader\journals\" + j.Date + ".json";
                            System.IO.File.WriteAllText(journalPath, json);
                            json = json + ",\n";
                            System.IO.File.AppendAllText(@"C:\git\petssl-downloader\journals\all.json", json);

                        }
                    }
                }
            });
            bool isCompleted = foreachResult.IsCompleted;

            string allJournalJson = JsonSerializer.Serialize(journalList.OrderBy(j => j.Date), jsonOptions);
            System.IO.File.AppendAllText(@"C:\git\petssl-downloader\journals\list.json", allJournalJson);


        }

        public void ComputeStatistics()
        {
            var journalsJson = File.ReadAllText(@"C:\git\petssl-downloader\journals\list.json");
            var journalList = JsonSerializer.Deserialize<List<Journal>>(journalsJson, jsonOptions);

            var years = journalList.GroupBy(j => DateTime.Parse(j.Date).ToString("yyyy"));
            foreach(var year in years.OrderBy(y =>y.Key))
            {
                var petSitters = year.GroupBy(j => j.PetSitter);
                foreach(var petSitter in petSitters.OrderByDescending(p => p.Count()))
                {
                    int walks = petSitter.Count();
                    int comments = petSitter.SelectMany(j=>j.Comments).Count();
                    int images = petSitter.SelectMany(j=>j.Images).Count();

                    Console.WriteLine("[{0}]\t{1}\t{2} Walks\t{3} Comments\t{4} Images", year.Key, petSitter.Key, walks, comments, images);
                    
                }
            }
        }
    }
}
