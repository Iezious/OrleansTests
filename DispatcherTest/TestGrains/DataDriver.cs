using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace TestGrains
{
    [BsonIgnoreExtraElements]
    public class InputData
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement]
        public string Actor { get; set; }

        [BsonElement]
        public DateTime Date { get; set; }

        [BsonElement]
        public bool Processed { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ProfileData
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement]
        public string Name { get; set; }

        [BsonElement]
        public string DName { get; set; }

        [BsonElement, BsonDefaultValue(0)]
        public int LogsCount { get; set; }
    }


    internal class DataDriver
    {
        private IMongoDatabase GetDB()
        {
            return new MongoClient(new MongoClientSettings()
            {
                Server = new MongoServerAddress("localhost", 27017),
                ConnectionMode = ConnectionMode.Standalone,
                MaxConnectionPoolSize = 5000,
                WaitQueueSize = 25000
            }).GetDatabase("tracker", new MongoDatabaseSettings
            {
                ReadPreference = ReadPreference.Nearest
                
            });
        }

        private IMongoCollection<InputData> GetInputsCollection()
        {
            return GetDB().GetCollection<InputData>("Logs");
        }
        private IMongoCollection<ProfileData> GetProfilesCollection()
        {
            return GetDB().GetCollection<ProfileData>("Profiles");
        }

        public Task<IAsyncCursor<InputData>> FetchInputs()
        {
            return GetInputsCollection()
                .Find(d => d.Processed == false)
                .Limit(500)
                .ToCursorAsync(new CancellationTokenSource(1000).Token);
        }

        public Task MarkInput(ObjectId id)
        {
            return GetInputsCollection().UpdateOneAsync(d => d.Id == id, Builders<InputData>.Update.Set(d => d.Processed, true), null,  new CancellationTokenSource(1000).Token);
        }

        public Task<ProfileData> GetProfile(string id)
        {
            return GetProfilesCollection()
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync(new CancellationTokenSource(1000).Token);
        }

        public Task UpdateProfile(string id, int count)
        {
            return GetProfilesCollection()
                .UpdateOneAsync(p => p.Id == id, Builders<ProfileData>.Update.Set(p => p.LogsCount, count), null,
                    new CancellationTokenSource(1000).Token);
        }
    }
}
