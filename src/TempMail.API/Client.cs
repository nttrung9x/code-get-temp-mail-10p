using CloudflareSolverRe;
using CloudflareSolverRe.CaptchaProviders;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TempMail.API.Constants;

namespace TempMail.API
{
    public class Client
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        private CookieContainer cookieContainer;

        private List<string> availableDomains;
        public List<string> AvailableDomains => availableDomains ?? (availableDomains = GetAvailableDomains());

        private readonly string _2CaptchaKey;
        private readonly ICaptchaProvider captchaProvider;

        private Change change;

        public Inbox Inbox;

        public string Email { get; set; }

        public HttpClient HttpClient { get; private set; }

        private IWebProxy proxy;

        public Client([Optional]ICaptchaProvider captchaProvider, [Optional]IWebProxy proxy)
        {
            Inbox = new Inbox(this);
            change = new Change(this);

            this.captchaProvider = captchaProvider;
            this.proxy = proxy;
        }

        /// <summary>
        /// Starts a new client session and get a new temporary email.
        /// </summary>
        public void StartNewSession()
        {
            CreateHttpClient();
            
            var document = HttpClient.GetHtmlDocument(Urls.MAIN_PAGE_URL);

            Email = ExtractEmail(document);
        }

        /// <summary>
        /// Starts a new client session and get a new temporary email.
        /// </summary>
        public async Task StartNewSessionAsync()
        {
            await Task.Run(() => CreateHttpClient());
            
            var document = await HttpClient.GetHtmlDocumentAsync(Urls.MAIN_PAGE_URL);

            Email = await Task.Run(() => ExtractEmail(document));
        }
        
        private string ExtractEmail(HtmlDocument document)
        {
            return document.GetElementbyId("mail")?.GetAttributeValue("value", null);
        }


        /// <summary>
        /// Changes the temporary email to ex: login@domain
        /// </summary>
        /// <param name="login">New temporary email login</param>
        /// <param name="domain">New temporary email domain</param>
        public string Change(string login, string domain)
        {
            Email = change.ChangeEmail(login, domain);

            Inbox.Clear();

            return Email;
        }

        /// <summary>
        /// Changes the temporary email to ex: login@domain
        /// </summary>
        /// <param name="login">New temporary email login</param>
        /// <param name="domain">New temporary email domain</param>
        public async Task<string> ChangeAsync(string login, string domain)
        {
            Email = await change.ChangeEmailAsync(login, domain);

            Inbox.Clear();

            return Email;
        }


        /// <summary>
        /// Deletes the temporary email and gets a new one.
        /// </summary>
        public bool Delete()
        {
            HttpResponseMessage response;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, Urls.DELETE_URL))
            {
                requestMessage.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                requestMessage.Headers.Referrer = new Uri(Urls.BASE_URL);
                requestMessage.Headers.Add("X-Requested-With", "XMLHttpRequest");
                response = HttpClient.Send(requestMessage);
            }

            if (response.StatusCode != HttpStatusCode.OK)
                return false;

            Email = response.Content.ReadAsJsonObject<Dictionary<string, object>>(encoding)?["mail"].ToString();

            UpdateEmailCookie();

            Inbox.Clear();

            return true;
        }

        /// <summary>
        /// Deletes the temporary email and gets a new one.
        /// </summary>
        public async Task<bool> DeleteAsync()
        {
            HttpResponseMessage response;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, Urls.DELETE_URL))
            {
                requestMessage.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                requestMessage.Headers.Referrer = new Uri(Urls.BASE_URL);
                requestMessage.Headers.Add("X-Requested-With", "XMLHttpRequest");
                response = await HttpClient.SendAsync(requestMessage);
            }

            if (response.StatusCode != HttpStatusCode.OK)
                return false;

            Email = (await Task.Run(() => response.Content.ReadAsJsonObject<Dictionary<string, object>>(encoding)))?["mail"].ToString();

            await Task.Run(() => UpdateEmailCookie());

            Inbox.Clear();

            return true;
        }


        private List<string> GetAvailableDomains()
        {
            return HttpClient.GetHtmlDocument(Urls.CHANGE_URL).GetElementbyId("domain").Descendants("option")
                .Select(s => s.GetAttributeValue("value", null)).ToList();
        }

        public Cookie GetCsrfCookie()
        {
            return cookieContainer.GetCookies(new Uri(Urls.BASE_URL))["csrf"];
        }

        private void UpdateEmailCookie()
        {
            cookieContainer.SetCookies(new Uri(Urls.BASE_URL), $"mail={Email}");
        }


        private void CreateHttpClient()
        {
            cookieContainer = new CookieContainer();

            var handler = new ClearanceHandler(captchaProvider)
            {
                InnerHandler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    Proxy = proxy
                },
                MaxTries = 5,
                ClearanceDelay = 3000
            };

            HttpClient = new HttpClient(handler);

            HttpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3");
            HttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.142 Safari/537.36");
            HttpClient.DefaultRequestHeaders.Add("Host", "temp-mail.org");
            HttpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            HttpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        }

    }
}
