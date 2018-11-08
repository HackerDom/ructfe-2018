using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{

    public class UserStorage : BaseStorage, IUserStorage
    {
        public UserStorage(IMongoClient mongoClient, IUUIDProvider uuidProvider) : base(mongoClient)
        {
            this.uuidProvider = uuidProvider;
            users = new ConcurrentDictionary<string, UserInfo>();
            usersCollection = GetCollection<UserInfo>(NameReslover.UserCollectionName);
            Init().GetAwaiter().GetResult();
        }

        public UserInfo AddUser(UserMeta meta)
        {
            var userInfo = new UserInfo(uuidProvider.GetUUID(meta).ToString())
            {
                Meta = meta
            };

            users[userInfo.UserId] = userInfo;

            usersCollection.InsertOneAsync(userInfo).Wait();

            return userInfo;
        }

        public UserInfo GetUser(string userId)
        {
            return users.TryGetValue(userId, out var userInfo) ? userInfo : null;
        }

        private async Task Init()
        {

            var documents = await usersCollection.Find(_ => true).ToListAsync();
            foreach (var userInfo in documents)
            {
                users[userInfo.UserId] = userInfo;
            }
        }

        private readonly IUUIDProvider uuidProvider;
        private readonly ConcurrentDictionary<string, UserInfo> users;
        private readonly IMongoCollection<UserInfo> usersCollection;
    }
}