using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MongoDB.Driver;
using Vch.Core.Meta;

namespace Vch.Core.Sorages
{

    public class UserStorage : BaseStorage, IUserStorage
    {
        public UserStorage(IMongoClient mongoClient, IUUIDProvider uuidProvider) : base(mongoClient)
        {
            this.uuidProvider = uuidProvider;
            users = new ConcurrentDictionary<UInt64, UserInfo>();
            usersCollection = GetCollection<UserInfo>(NameReslover.UserCollectionName);

            Init().GetAwaiter().GetResult();
        }

        public UserInfo AddUser(UserMeta meta)
        {
            var userInfo = new UserInfo(uuidProvider.GetUUID(meta));

            users[userInfo.Id] = userInfo;
            usersCollection.InsertOne(userInfo);
            return userInfo;
        }

        public UserInfo GetUser(UInt64 userId)
        {
            return users.TryGetValue(userId, out var userInfo) ? userInfo : null;
        }

        private async Task Init()
        {
            var usersFindResult = await usersCollection.FindAsync(info => true);

            foreach (var userInfo in usersFindResult.Current)
            {
                users[userInfo.Id] = userInfo;
            }
        }

        private readonly IUUIDProvider uuidProvider;
        private readonly ConcurrentDictionary<UInt64, UserInfo> users;
        private readonly IMongoCollection<UserInfo> usersCollection;
    }
}