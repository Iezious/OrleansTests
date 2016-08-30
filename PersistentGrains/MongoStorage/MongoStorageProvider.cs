using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;

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
            var sw = new BinaryTokenStreamWriter();
            new BinaryFormatterSerializer().Serialize(data, sw, data.GetType());
            return sw.ToByteArray();
        }

        private object BinaryDeserialize(byte[] data, Type t)
        {
            var rd = new BinaryTokenStreamReader(data);
            return new BinaryFormatterSerializer().Deserialize(t, rd);
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
                grainState.State = BsonSerializer.Deserialize(bdata, grainState.State.GetType());
                return;
            }

            var data = bdata["Data"].AsBsonBinaryData.Bytes;
            grainState.State = BinaryDeserialize(data, null);
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var filter = new BsonDocument();

            var bdata = _binarySerializer 
                ? new BsonDocument { {"Data", BinarySerialize(grainState) } }
                : grainState.State.ToBsonDocument();

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
