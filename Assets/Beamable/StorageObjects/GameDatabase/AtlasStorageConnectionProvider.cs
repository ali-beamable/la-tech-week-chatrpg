using Beamable.Common;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Beamable.Server;
using Beamable.Server.Api.RealmConfig;
using MongoDB.Bson;

namespace Beamable.StorageObjects.GameDatabase
{
    public class AtlasStorageConnectionProvider : IStorageObjectConnectionProvider
    {
	    private readonly IRealmInfo _realmInfo;
	    private readonly IMicroserviceRealmConfigService _realmConfigService;
	    
	    private ConcurrentDictionary<Type, Promise<IMongoDatabase>> _databaseCache =
		    new ConcurrentDictionary<Type, Promise<IMongoDatabase>>();
	    
	    public AtlasStorageConnectionProvider(IRealmInfo realmInfo, IMicroserviceRealmConfigService realmConfigService)
	    {
		    _realmInfo = realmInfo;
		    _realmConfigService = realmConfigService;
	    }

	    public async Promise<IMongoDatabase> GetDatabase<TStorage>(bool useCache = true) where TStorage : MongoStorageObject
		{
			if (!useCache)
			{
				_databaseCache.TryRemove(typeof(TStorage), out _);
			}

			var db = await _databaseCache.GetOrAdd(typeof(TStorage), (type) =>
			{
				string storageName = string.Empty;
				var attributes = type.GetCustomAttributes(true);
				foreach (var attribute in attributes)
				{
				   if (attribute is StorageObjectAttribute storageAttr)
				   {
					   storageName = storageAttr.StorageName;
					   break;
				   }
				}

				if (string.IsNullOrEmpty(storageName))
				{
				   BeamableLogger.LogError($"Cannot find storage name for type {type} ");
				   return null;
				}

				return GetDatabaseByStorageName(storageName);
			});

			return db;
		}

		public Promise<IMongoCollection<TCollection>> GetCollection<TStorage, TCollection>()
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			return GetCollection<TStorage, TCollection>(typeof(TCollection).Name);
		}
		public async Promise<IMongoCollection<TCollection>> GetCollection<TStorage, TCollection>(string collectionName)
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			var db = await GetDatabase<TStorage>();
			return db.GetCollection<TCollection>(collectionName);
		}

		public Promise<IMongoDatabase> this[string name] => GetDatabaseByStorageName(name);

		private async Promise<IMongoDatabase> GetDatabaseByStorageName(string storageName)
		{
			var settings = await _realmConfigService.GetRealmConfigSettings();
			var connStr = settings.GetSetting("game", "mongo_srv");
			var client = new MongoClient(connStr);
			var db = client.GetDatabase($"{_realmInfo.CustomerID}{_realmInfo.ProjectName}_{storageName}");
			
			return db;
		}
    }
    
    public record AtlasSearchIndex
    {
        public string id;
        public string name;
        public string status;
        public bool queryable;

        public AtlasSearchIndexStatus Status
        {
            get
            {
                if (!string.IsNullOrEmpty(status) && Enum.TryParse(status, out AtlasSearchIndexStatus parsed))
                {
                    return parsed;
                }

                throw new ArgumentOutOfRangeException("status");
            }
        }
    }

    public enum AtlasSearchIndexStatus
    {
        BUILDING,
        FAILED,
        PENDING,
        READY,
        STALE
    }

    public record CreateVectorIndexModel<TDocument>
    {
        public string IndexName { get; set; }
        public string FieldName { get; set; }

        public CreateVectorIndexModel(string indexName, Expression<Func<TDocument, object>> field)
        {
            IndexName = indexName;
            if (field.Body is MemberExpression memberExpression)
            {
                FieldName = memberExpression.Member.Name;
            }
            else
            {
                throw new ArgumentException("Invalid Vector Index expression.");
            }
        }

        public BsonDocument ToBson()
        {
            return new BsonDocument {
                { "name", IndexName},
                { "definition", new BsonDocument { 
                    { "mappings", new BsonDocument { 
                        { "fields", new BsonDocument {
                            { FieldName, new BsonArray(new BsonDocument[] {
                                new BsonDocument
                                {
                                    {"dimensions", 1536}, 
                                    {"similarity", "cosine"},
                                    {"type", "knnVector" }
                                }
                            })}
                        }} 
                    }}
                }}
            };
        }
    }
}