using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            InfiniteRemoveOldUsersAsync();
        }

        public async Task<UserInfo> AddUser(UserMeta userMeta)
        {
            var userInfo = new UserInfo((await uuidProvider.GetUUID(userMeta)).ToString())
            {
                Meta = userMeta,
                RegistrationDate = DateTime.UtcNow
            };

            await usersCollection.InsertOneAsync(userInfo);
	        users[userInfo.UserId] = userInfo;

			return userInfo;
        }

        public UserInfo FindUser(string userId)
        {
            return users.TryGetValue(userId, out var userInfo) ? userInfo : null;
        }

        private async Task InfiniteRemoveOldUsersAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    foreach (var user in users.Where(pair => (DateTime.UtcNow -  pair.Value.RegistrationDate).TotalMinutes > TTLinMinutes))
                        DeleteUser(user.Key);
                }
                catch (Exception e)
                {
                }
            }
        }

        public void DeleteUser(string id)
        {
            usersCollection.DeleteOneAsync(userInfo => userInfo.UserId.Equals(id)).Wait();
            users.Remove(id, out var _);
        }


        private async Task Init()
        {
            //usersCollection.DeleteMany(info => true);
            var documents = await usersCollection.Find(_ => true).ToListAsync();
            foreach (var userInfo in documents)
                users[userInfo.UserId] = userInfo;
        }

        private const int TTLinMinutes = 30;
        private readonly IUUIDProvider uuidProvider;
        private readonly ConcurrentDictionary<string, UserInfo> users;
        private readonly IMongoCollection<UserInfo> usersCollection;
    }
}