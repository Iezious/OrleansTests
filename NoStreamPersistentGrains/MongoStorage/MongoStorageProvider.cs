using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;
using BsonWriter = Newtonsoft.Json.Bson.BsonWriter;

namespace MongoStorage
{
    public class MongoStorageProvider : IStorageProvider
    {
        private const string DATA_CONNECTION_STRING = "ConnectionString";
        private const string DATABASE_NAME_PROPERTY = "Database";
        private const string USE_GUID_AS_STORAGE_KEY = "UseGuidAsStorageKey";
        private const string USE_STRING_AS_STORAGE_KEY = "UseStringKey";
        private const string BINARY_SERIALIZATION = "Binary";

        private bool _useGuidAsStorageKey;
        private bool _useStringKey; 
        private string _connectionString;
        private string _databaseName;
        private bool _binarySerializer;

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

            _useStringKey = config.Properties.ContainsKey(USE_STRING_AS_STORAGE_KEY) &&
                "true".Equals(config.Properties[USE_STRING_AS_STORAGE_KEY], StringComparison.OrdinalIgnoreCase);

            _binarySerializer = config.Properties.ContainsKey(BINARY_SERIALIZATION) &&
                "true".Equals(config.Properties[BINARY_SERIALIZATION], StringComparison.OrdinalIgnoreCase);

            return TaskDone.Done;
        }

        public Task Close()
        {
            return TaskDone.Done;
        }

        private byte[] BinarySerialize(object data)
        {
            var ms = new BinaryTokenStreamWriter();
            new BinaryFormatterSerializer().Serialize(data, ms, data.GetType());
            return ms.ToByteArray();
        }

        private object BinaryDeserialize(byte[] data, Type type)
        {
            var ms = new BinaryTokenStreamReader(data);
            return new BinaryFormatterSerializer().Deserialize(type, ms);
        }

        private string JSONSerialize(object data)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(data);
        }

        private void JSONDeserialize(string data, object tofill)
        {
            Newtonsoft.Json.JsonConvert.PopulateObject(data, tofill);
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var filter = new BsonDocument();

            if (_useStringKey)
                filter["_id"] = grainReference.GetPrimaryKeyString();
            else if (_useGuidAsStorageKey)
                filter["_id"] = grainReference.GetPrimaryKey();
            else
                filter["_id"] = grainReference.ToKeyString();

            var bdata = await GetCollection(grainType).Find(filter).FirstOrDefaultAsync(new CancellationTokenSource(1000).Token);
            if (bdata == null) return;

            bdata.Remove("_id");

            if (!_binarySerializer)
            {
                JSONDeserialize(bdata["Data"].AsString, grainState.State);
                return;
            }

            var data = bdata["Data"].AsByteArray;
            grainState.State = BinaryDeserialize(data, grainState.State.GetType());
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var filter = new BsonDocument();

            var bdata = _binarySerializer 
                ? new BsonDocument {{"Data", BinarySerialize(grainState.State)}}
                : new BsonDocument { { "Data", JSONSerialize(grainState.State) } };

            if (_useStringKey)
            {
                filter["_id"] = grainReference.GetPrimaryKeyString();
                bdata["_id"] = grainReference.GetPrimaryKeyString();
            }
            else if (_useGuidAsStorageKey)
            {
                filter["_id"] = grainReference.GetPrimaryKey();
                bdata["_id"] = grainReference.GetPrimaryKey();
            }
            else
            {
                filter["_id"] = grainReference.ToKeyString();
                bdata["_id"] = grainReference.ToKeyString();

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
