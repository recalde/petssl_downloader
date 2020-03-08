using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace petssl_downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Run();
        }

        static void Run()
        {
            var website = new WebsiteDriver();
            website.Home();
            website.Login("email", "changeit");
            website.Client();
            var cookies = website.GetCookies();

            // for(int pageNumber = 0; pageNumber <= 24; pageNumber++)
            // {
            //     string images = website.Images(pageNumber);

            //     var doc = new HtmlDocument();
            //     doc.LoadHtml(images);

            //     var imageNodes = doc.DocumentNode.SelectNodes("//a/img");
            //     if (imageNodes != null)
            //     {
            //         foreach(var imageNode in imageNodes)
            //         {
            //             //https://hshpetcare.petssl.com/images/image-size.php/
            //             //A992B32B-9FFF-483B-AC8E-0E24B65EC18D-newab259910.jpg?width=400&height=400&cropratio=1:1&fr=true&image=/images/uploads/Content/A992B32B-9FFF-483B-AC8E-0E24B65EC18D-newab259910.jpg

            //             var imageSrc = imageNode.GetAttributeValue("src", "");
            //             if (imageSrc.StartsWith("https://hshpetcare.petssl.com/images/image-size.php/"))
            //             {
            //                 int imgLoc = 52;
            //                 int queryLoc = imageSrc.IndexOf('?');
            //                 var imageObjectId = imageSrc.Substring(imgLoc, queryLoc-imgLoc);
            //                 Console.WriteLine(imageObjectId);

            //                 website.DownloadImage(imageObjectId);
            //             }
                        
            //         }
            //     }
            // }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                IgnoreNullValues = true
            };

            DateTime startDate = new DateTime(2020,3,2);
            DateTime endDate = new DateTime(2015,12,1);
            var journalList = new List<Journal>();
            List<DateTime> dates = new List<DateTime>();

            while(startDate > endDate)
            {
                startDate = startDate.AddDays(-7);
                dates.Add(startDate);
            }

            Object listLock = new Object();   
            var foreachResult = Parallel.ForEach(dates, (date) => 
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
                                    c.User = commentNode.ChildNodes[1].ChildNodes[1].InnerHtml;
                                    c.CommentText = commentNode.ChildNodes[2].InnerHtml;
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
                                    }
                                }
                            }
                            var journalDate = DateTime.Parse(j.Date);
                            j.Date = journalDate.ToString("yyyy-MM-dd");
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
    }

    public class Journal
    {
        public string Date { get; set; }
        public string Service { get; set; }
        public string Pet { get; set; }
        public string PetSitter { get; set; }
        public string Poop { get; set; }
        public string Pee { get; set; }
        public string Meal { get; set; }
        public string OtherActions { get; set; }
        public List<Comment> Comments { get; set; }
        public List<string> Images{ get; set; }

        public override string ToString()
        {
            return string.Format("{0}-{1}-{2}-{3}-{4}-{5}-{6}", Date, Service, Pet, PetSitter, Poop, Pee, OtherActions);
        }
    }

    public class Comment
    {
        public string User { get; set; }
        public string CommentText { get; set; }
    }
}
