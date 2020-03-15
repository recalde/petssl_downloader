using System.Net.Http;
using System.Collections.Generic;
using System.Net;
using System;
using System.Linq;
using System.IO;

namespace petssl_downloader
{
    public class WebsiteDriver
    {
        private readonly Configuration configuration;
        private readonly HttpClient httpClient;
        private readonly CookieContainer cookieContainer;

        public WebsiteDriver(Configuration configuration)
        {
            this.configuration = configuration;
            this.cookieContainer = new CookieContainer();
            var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            };
            this.httpClient = new HttpClient(httpClientHandler);
            this.httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public Cookie[] GetCookies()
        {
            return cookieContainer.GetCookies(configuration.WebsiteUri).Cast<Cookie>().ToArray();
        }

        public void Login(string username, string password)
        {
            var url = new Uri(configuration.WebsiteUri, "login");

            Console.WriteLine("[POST] {0}", url);

            var userCookie = new Cookie("USER", username);
            cookieContainer.Add(configuration.WebsiteUri, userCookie);

            // Get Cookie PHPSESSID, AWSELB, AWSELBCORS
            var postValues = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "btn_submit", "true" }
            };

            var postContent = new FormUrlEncodedContent(postValues);

            var responseTask = httpClient.PostAsync(url, postContent);
            responseTask.Wait();
            var response = responseTask.Result;

            var readContentTask = response.Content.ReadAsStringAsync();
            readContentTask.Wait();
            var content = readContentTask.Result;
        }

        public void Home()
        {
            var url = configuration.WebsiteUri;
            Console.WriteLine("[GET] {0}", url);
            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
        }

        public void Client()
        {
            // Get Cookie -  ADMINLOGIN
            var url = new Uri(configuration.WebsiteUri, "clients");
            Console.WriteLine("[GET] {0}", url);
            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
        }

        public string Images(int pageNumber)
        {
            Uri url = null;
            if (pageNumber == 0)
            {
                url = new Uri(configuration.WebsiteUri, $"admin/images");
            }
            else
            {
                int start = pageNumber * 96;
                int end = start + 96;
                url = new Uri(configuration.WebsiteUri, $"admin/images?v=gallery&s={end}&s={start}");
            }

            Console.WriteLine("[GET] {0}", url);
            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
            var response = responseTask.Result;

            var readContentTask = response.Content.ReadAsStringAsync();
            readContentTask.Wait();
            var content = readContentTask.Result;
            return content;
        }


        public string Schedule(DateTime startDate)
        {
            var url = new Uri(configuration.WebsiteUri, $"admin/my-schedule?v=cal&l=5day&d={startDate.ToString("yyyy-MM-dd")}");
            Console.WriteLine("[GET] {0}", url);

            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
            var response = responseTask.Result;

            var readContentTask = response.Content.ReadAsStringAsync();
            readContentTask.Wait();
            var content = readContentTask.Result;
            return content;
        }

        public string Journal(string href)
        {
            var url = new Uri(configuration.WebsiteUri, href);
            Console.WriteLine("[GET] {0}", url);

            var responseTask = httpClient.GetAsync(url);
            responseTask.Wait();
            var response = responseTask.Result;

            var readContentTask = response.Content.ReadAsStringAsync();
            readContentTask.Wait();
            var content = readContentTask.Result;
            return content;
        }



        public string DownloadImage(string src)
        {
            string url = "https://s3.amazonaws.com/petssl.com/hshpetcare/images/uploads/Content/" + src;
            Console.WriteLine("[GET] {0}", url);

            string localPath = Path.Combine(configuration.ImageDirectory, src);
            if (!File.Exists(localPath))
            {
                byte[] content = TryGetUrlBytes(url);
                // Retry
                if (content == null) content = TryGetUrlBytes(url);
                if(content != null)
                {
                    File.WriteAllBytes(localPath, content);
                }   
            }

            return localPath;
        }

        public byte[] TryGetUrlBytes(string url)
        {
            try
            {
                var responseTask = httpClient.GetAsync(url);
                responseTask.Wait();
                var response = responseTask.Result;

                var readContentTask = response.Content.ReadAsByteArrayAsync();
                readContentTask.Wait();
                var content = readContentTask.Result;
                return content;
            }
            catch(Exception)
            {
                Console.WriteLine("[ERROR] {0}", url);
            }
            return null;
        }


    }
}