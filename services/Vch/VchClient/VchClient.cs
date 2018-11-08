using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Vch.Core.Helpers;
using Vch.Core.Meta;

namespace VchUtils
{
    public class VchClient
    {
        public VchClient(Uri apiBaseUri)
        {
            this.apiBaseUri = new Uri($"{apiBaseUri}api/board/");
            httpClient = new HttpClient();
        }

        public async Task<UserInfo> RegisterUser(UserMeta meta)
        {
            var request =
                new HttpRequestMessage(HttpMethod.Post, new Uri($"{apiBaseUri}user"))
                {
                    Content = new StringContent(meta.ToJson()),
                };
            var response = await httpClient.SendAsync(request);

            response.EnsureSucces("Can't register user");
            return (await response.Content.ReadAsStringAsync()).FromJSON<UserInfo>();
        }


        public async Task<Message> PostMessage(string userId, string key)
        {
            var request =
                new HttpRequestMessage(HttpMethod.Post, new Uri($"{apiBaseUri}message/post/{userId}"))
                {
                    Content = new StringContent(key)
                };
            var response = await httpClient.SendAsync(request);

            response.EnsureSucces("Can't post message");

            return (await response.Content.ReadAsStringAsync()).FromJSON<Message>();
        }

        public async Task<IEnumerable<Message>> GetUserMessages(UInt64 userId)
        {
	        var request =
		        new HttpRequestMessage(HttpMethod.Get, new Uri($"{apiBaseUri}messages/{userId}"));
            var response = await httpClient.SendAsync(request);
            response.EnsureSucces("Can't get user messages");

            return (await response.Content.ReadAsStringAsync()).FromJSON<IEnumerable<Message>>();
        }

        public async Task<IEnumerable<IMessage>> GetAll()
        {
            var request =
                new HttpRequestMessage(HttpMethod.Get, new Uri($"{apiBaseUri}messages"));
            var response = await httpClient.SendAsync(request);
            response.EnsureSucces("Can't get all messages");

            return (await response.Content.ReadAsStringAsync()).FromJSON<IEnumerable<IMessage>>();
        }

        private readonly Uri apiBaseUri;
        private readonly HttpClient httpClient;

    }
}
