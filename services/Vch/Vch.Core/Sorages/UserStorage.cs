using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{
	public class UserStorage : BaseMongoStorage, IUserStorage
    {
        public UserStorage(IMongoClient mongoClient, IUUIDProvider uuidProvider) : base(mongoClient)
        {
            this.uuidProvider = uuidProvider;
            users = new ConcurrentDictionary<string, UserInfo>();
            usersCollection = GetOrCreateCollection<UserInfo>(NameReslover.UsersCollectionName);
            Init().Wait();
        }

        public async Task<UserInfo> AddUser(UserMeta userMeta)
        {
            var userInfo = new UserInfo((await uuidProvider.GetUUID(userMeta)).ToString())
            {
                Meta = userMeta
            };

            await usersCollection.InsertOneAsync(userInfo);
	        users[userInfo.UserId] = userInfo;

			return userInfo;
        }

        public UserInfo FindUser(string userId)
        {
            return users.TryGetValue(userId, out var userInfo) ? userInfo : null;
        }

        private async Task Init()
        {
			var documents = await usersCollection.Find(_ => true).ToListAsync();
            foreach (var userInfo in documents)
                users[userInfo.UserId] = userInfo;
        }

        private readonly IUUIDProvider uuidProvider;
        private readonly ConcurrentDictionary<string, UserInfo> users;
        private readonly IMongoCollection<UserInfo> usersCollection;
    }
}