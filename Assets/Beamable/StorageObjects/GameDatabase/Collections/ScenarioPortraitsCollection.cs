using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.StorageObjects.GameDatabase
{
	public class ScenarioPortraitsCollection : MongoCollection<Server.GameDatabase, ScenarioAsset>
    {
        protected override string CollectionName => "scenario.portraits";
        protected override IEnumerable<CreateIndexModel<ScenarioAsset>> Indexes => new []
        {
            new CreateIndexModel<ScenarioAsset>(
                Builders<ScenarioAsset>.IndexKeys.Hashed(x => x.FileUrl)
            )
        };
        
        protected override IEnumerable<CreateVectorIndexModel<ScenarioAsset>> VectorIndexes => new []
        {
            new CreateVectorIndexModel<ScenarioAsset>("portraits-vector-index", x => x.Embedding)
        };
        
        public ScenarioPortraitsCollection(IStorageObjectConnectionProvider connectionProvider) : base(connectionProvider){}

        public async Task<List<ScenarioAsset>> VectorSearch(float[] query, double scoreCutoff)
        {
            var collection = await GetCollection();
            var knnBeta = new BsonDocument { { "path", "Embedding"}, { "k", 15}, { "vector", new BsonArray(query) } };
            var searchStage = new BsonDocument { { "index", "portraits-vector-index" }, { "knnBeta", knnBeta } };
            var projectStage = new BsonDocument { { "embedding", 0 }, { "_id", 0 }, { "score", new BsonDocument("$meta", "searchScore") } };

            PipelineDefinition<ScenarioAsset, ScenarioAsset> pipeline = new []
            {
                new BsonDocument("$search",  searchStage),
                new BsonDocument("$project",  projectStage)
            };

            var result = await collection.Aggregate(pipeline).ToListAsync();
            var filteredResult = result.FindAll(skybox => skybox.Score >= scoreCutoff);
            return filteredResult;
        }

        public async Task<bool> DeleteAll()
        {
            var collection = await GetCollection();
            var result = await collection.DeleteManyAsync(new BsonDocument());

            return result.IsAcknowledged;
        }

        public async Task<bool> Insert(ScenarioAsset asset)
        {
            var collection = await GetCollection();
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

        public async Task<bool> Insert(IEnumerable<ScenarioAsset> assets)
        {
            var collection = await GetCollection();
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