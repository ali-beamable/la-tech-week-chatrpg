using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.Microservices.ChatRpg.Storage
{
	public static class ScenarioAssetsCollection
    {
        private static readonly string _collectionName = "scenario.assets";

        private static IMongoCollection<ScenarioAsset> _collection;
        private static readonly IEnumerable<CreateIndexModel<ScenarioAsset>> _indexes = new CreateIndexModel<ScenarioAsset>[]
        {
            new CreateIndexModel<ScenarioAsset>(
                Builders<ScenarioAsset>.IndexKeys.Hashed(x => x.FileUrl)
            )
        };

        public static async ValueTask<IMongoCollection<ScenarioAsset>> Get(IMongoDatabase db)
        {
            if (_collection is null)
            {
                _collection = db.GetCollection<ScenarioAsset>(_collectionName);
                if(_indexes.Count() > 0) { 
                    await _collection.Indexes.CreateManyAsync(_indexes);
                }
            }

            return _collection;
        }

        public static async Task<List<ScenarioAsset>> VectorSearch(IMongoDatabase db, float[] query)
        {
            var collection = await Get(db);
            var knnBeta = new BsonDocument { { "path", "Embedding"}, { "k", 15}, { "vector", new BsonArray(query) } };
            var searchStage = new BsonDocument { { "index", "embedding" }, { "knnBeta", knnBeta } };
            var projectStage = new BsonDocument { { "embedding", 0 }, { "_id", 0 }, { "score", new BsonDocument("$meta", "searchScore") } };

            PipelineDefinition<ScenarioAsset, ScenarioAsset> pipeline = new BsonDocument[]
            {
                new BsonDocument("$search",  searchStage),
                new BsonDocument("$project",  projectStage)
            };

            return await collection.Aggregate(pipeline).ToListAsync();
        }

        public static async Task<bool> DeleteAll(IMongoDatabase db)
        {
            var collection = await Get(db);
            var result = await collection.DeleteManyAsync(new BsonDocument());

            return result.IsAcknowledged;
        }

        public static async Task<bool> Insert(IMongoDatabase db, ScenarioAsset asset)
        {
            var collection = await Get(db);
            try
            {
                await collection.InsertOneAsync(asset);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        public static async Task<bool> Insert(IMongoDatabase db, IEnumerable<ScenarioAsset> assets)
        {
            var collection = await Get(db);
            try
            {
                 await collection.InsertManyAsync(assets);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }
    }

    public record ScenarioAsset
    {
        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
        /* Generation Input */
        public string Prompt { get; set; } = "";
        public string Model { get; set; } = "";
        
        /* Generation Output */
        public string FileUrl { get; set; } = "";
        
        /* Vector Search Fields */
        public string Content { get; set; } = "";
        public float[] Embedding { get; set; } = Array.Empty<float>();

        [BsonElement("score")]
        public double? Score { get; set; }
    }
}