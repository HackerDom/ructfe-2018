using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Vch.Core.Sorages
{
    public abstract class BaseStorage
    {
        protected BaseStorage(IMongoClient mongoClient)
        {
            mongoDatabase = mongoClient.GetDatabase(NameReslover.MongoDBName);
        }

        protected IMongoCollection<TValue> GetCollection<TValue>(string name)
        {
            if (!CollectionExistsAsync(name).GetAwaiter().GetResult())
            {
                mongoDatabase.CreateCollection(name);
            }

            return mongoDatabase.GetCollection<TValue>(name);
        }

        public async Task<bool> CollectionExistsAsync(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            //filter by collection name
            var collections = await mongoDatabase.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            //check for existence
            return await collections.AnyAsync();
        }

        private readonly IMongoDatabase mongoDatabase;
    }
}