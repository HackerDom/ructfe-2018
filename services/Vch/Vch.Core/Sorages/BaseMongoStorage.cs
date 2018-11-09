using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Vch.Core.Sorages
{
    public abstract class BaseMongoStorage
    {
        protected BaseMongoStorage(IMongoClient mongoClient)
        {
            mongoDatabase = mongoClient.GetDatabase(NameReslover.MongoDBName);
        }

        protected IMongoCollection<TValue> GetOrCreateCollection<TValue>(string name)
        {
	        if(!CollectionExistsAsync(name).Result)
            {
                mongoDatabase.CreateCollection(name);

                OnCollectionCreate(name);
            }

            return mongoDatabase.GetCollection<TValue>(name);
        }

        protected virtual void OnCollectionCreate(string name)
        {

        }

        public async Task<bool> CollectionExistsAsync(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = await mongoDatabase.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            return await collections.AnyAsync();
        }

        protected readonly IMongoDatabase mongoDatabase;
    }
}