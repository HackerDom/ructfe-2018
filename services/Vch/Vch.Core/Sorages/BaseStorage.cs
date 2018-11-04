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
              return mongoDatabase.GetCollection<TValue>(name);
        }


        private readonly IMongoDatabase mongoDatabase;
    }
}