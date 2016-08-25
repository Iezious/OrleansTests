using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;

namespace MongoStorage
{
    public class MongoStorageProvider : IStorageProvider
    {
        private const string DATA_CONNECTION_STRING = "ConnectionString";
        private const string DATABASE_NAME_PROPERTY = "Database";
        private const string USE_GUID_AS_STORAGE_KEY = "UseGuidAsStorageKey";

        private bool _useGuidAsStorageKey;
        private string _connectionString;
        private string _databaseName;

        public Logger Log { get; private set; }

        public string Name { get; private set; }

        private IMongoDatabase Database => new MongoClient(_connectionString).GetDatabase(_databaseName);

        private IMongoCollection<BsonDocument> GetCollection(string tyname) => Database.GetCollection<BsonDocument>(tyname);

        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Log = providerRuntime.GetLogger(this.GetType().FullName);

            Name = name;

            if (!config.Properties.ContainsKey(DATA_CONNECTION_STRING) ||
                !config.Properties.ContainsKey(DATABASE_NAME_PROPERTY))
            {
                throw new ArgumentException("ConnectionString Or Database property not set");
            }

            _connectionString = config.Properties[DATA_CONNECTION_STRING];
            _databaseName = config.Properties[DATABASE_NAME_PROPERTY];

            _useGuidAsStorageKey = config.Properties.ContainsKey(USE_GUID_AS_STORAGE_KEY) &&
                "true".Equals(config.Properties[USE_GUID_AS_STORAGE_KEY], StringComparison.OrdinalIgnoreCase);

            return TaskDone.Done;
        }

        public Task Close()
        {
            return TaskDone.Done;
        }


        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var filter = new BsonDocument();

            if (_useGuidAsStorageKey)
                filter["_id"] = grainReference.GetPrimaryKey();
            else
                filter["_id"] = grainReference.GetPrimaryKeyString();

            var bdata = await GetCollection(grainType).Find(filter).FirstOrDefaultAsync(new CancellationTokenSource(1000).Token);
            if(bdata == null) return;

            bdata.Remove("_id");

            grainState.State = BsonSerializer.Deserialize(bdata, grainState.State.GetType());
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var filter = new BsonDocument();


            var bdata = grainState.State.ToBsonDocument();

            if (_useGuidAsStorageKey)
            {
                filter["_id"] = grainReference.GetPrimaryKey();
                bdata["_id"] = grainReference.GetPrimaryKey();
            }
            else
            {
                filter["_id"] = grainReference.GetPrimaryKeyString();
                bdata["_id"] = grainReference.GetPrimaryKeyString();
            }

            await GetCollection(grainType).ReplaceOneAsync(filter, bdata, new UpdateOptions{IsUpsert = true}, new CancellationTokenSource(500).Token);
        }

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var filter = new BsonDocument();

            if (_useGuidAsStorageKey)
                filter["_id"] = grainReference.GetPrimaryKey();
            else
                filter["_id"] = grainReference.GetPrimaryKeyString();

            await GetCollection(grainType).DeleteOneAsync(filter, new CancellationTokenSource(500).Token);
        }

    }
}
