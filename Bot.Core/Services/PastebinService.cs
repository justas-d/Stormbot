using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Services
{
    public class PastebinService : IService
    {
        public enum PasteBinExpiration
        {
            Never,
            TenMinutes,
            OneHour,
            OneDay,
            OneMonth
        }

        public class PasteBinApiException : Exception
        {
            public PasteBinApiException(string message)
                : base(message)
            {
            }
        }

        public class PasteBinEntry
        {
            public string Title { get; set; }
            public string Text { get; set; }
            public string Format { get; set; }
            public bool Private { get; set; }
            public PasteBinExpiration Expiration { get; set; }
        }

        private static class ApiParameters
        {
            public const string DevKey = "api_dev_key";
            public const string UserKey = "api_user_key";
            public const string Option = "api_option";
            public const string UserName = "api_user_name";
            public const string UserPassword = "api_user_password";
            public const string PasteCode = "api_paste_code";
            public const string PasteName = "api_paste_name";
            public const string PastePrivate = "api_paste_private";
            public const string PasteFormat = "api_paste_format";
            public const string PasteExpireDate = "api_paste_expire_date";
        }

        private const string ApiPostUrl = "http://pastebin.com/api/api_post.php";
        private const string ApiLoginUrl = "http://pastebin.com/api/api_login.php";
        private string _apiUserKey;
        public bool IsLoggedIn => !string.IsNullOrEmpty(_apiUserKey);
        private HttpClient HttpClient => new HttpClient();

        void IService.Install(DiscordClient client)
        {
        }

        public async Task Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            FormUrlEncodedContent request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(ApiParameters.DevKey, Constants.PastebinApiKey),
                new KeyValuePair<string, string>(ApiParameters.UserName, username),
                new KeyValuePair<string, string>(ApiParameters.UserPassword, password)
            });

            HttpResponseMessage responce = await HttpClient.PostAsync(ApiLoginUrl, request);
            _apiUserKey = await ParseResponce(responce);
        }

        public async Task<string> Paste(PasteBinEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrEmpty(entry.Text))
                throw new ArgumentException("The paste text must be set", nameof(entry));

            IList<KeyValuePair<string, string>> content = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(ApiParameters.DevKey, Constants.PastebinApiKey),
                new KeyValuePair<string, string>(ApiParameters.Option, "paste"),
                new KeyValuePair<string, string>(ApiParameters.PasteCode, entry.Text)
            };

            SetIfNotEmpty(content, ApiParameters.PasteName, entry.Title);
            SetIfNotEmpty(content, ApiParameters.PasteFormat, entry.Format);
            SetIfNotEmpty(content, ApiParameters.PastePrivate, entry.Private ? "1" : "0");
            SetIfNotEmpty(content, ApiParameters.PasteExpireDate, FormatExpireDate(entry.Expiration));
            SetIfNotEmpty(content, ApiParameters.UserKey, _apiUserKey);

            FormUrlEncodedContent request = new FormUrlEncodedContent(content);

            HttpResponseMessage responce = await HttpClient.PostAsync(ApiPostUrl, request);
            return await ParseResponce(responce);
        }

        private async Task<string> ParseResponce(HttpResponseMessage responce)
        {
            string respString = await responce.Content.ReadAsStringAsync();

            if (respString.StartsWith("Bad API request"))
                throw new PasteBinApiException(respString);

            return respString;
        }

        private static string FormatExpireDate(PasteBinExpiration expiration)
        {
            switch (expiration)
            {
                case PasteBinExpiration.Never:
                    return "N";
                case PasteBinExpiration.TenMinutes:
                    return "10M";
                case PasteBinExpiration.OneHour:
                    return "1H";
                case PasteBinExpiration.OneDay:
                    return "1D";
                case PasteBinExpiration.OneMonth:
                    return "1M";
                default:
                    throw new ArgumentException("Invalid expiration date");
            }
        }

        private static void SetIfNotEmpty(IList<KeyValuePair<string, string>> content, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
                content.Add(new KeyValuePair<string, string>(name, value));
        }
    }
}