using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Beamable.StorageObjects.GameDatabase
{
    public class BlockadeSkyboxesCollection : MongoCollection<Server.GameDatabase, BlockadeSkybox>
    {
        protected override string CollectionName => "blockadelabs.skyboxes";
        protected override IEnumerable<CreateIndexModel<BlockadeSkybox>> Indexes => new []
        {
            new CreateIndexModel<BlockadeSkybox>(
                Builders<BlockadeSkybox>.IndexKeys.Hashed(x => x.FileUrl)
            )
        };
        
        protected override IEnumerable<CreateVectorIndexModel<BlockadeSkybox>> VectorIndexes => new []
        {
            new CreateVectorIndexModel<BlockadeSkybox>("skybox-vector-index", x => x.Embedding)
        };

        public BlockadeSkyboxesCollection(IStorageObjectConnectionProvider connectionProvider) : base(connectionProvider) {}
        
        public async Task<List<BlockadeSkybox>> VectorSearch(float[] query, double scoreCutoff)
        {
            var collection = await GetCollection();
            var knnBeta = new BsonDocument { { "path", "Embedding"}, { "k", 15}, { "vector", new BsonArray(query) } };
            var searchStage = new BsonDocument { { "index", "skybox-vector-index" }, { "knnBeta", knnBeta } };
            var projectStage = new BsonDocument { { "embedding", 0 }, { "_id", 0 }, { "score", new BsonDocument("$meta", "searchScore") } };

            PipelineDefinition<BlockadeSkybox, BlockadeSkybox> pipeline = new []
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

        public async Task<bool> Insert(BlockadeSkybox skybox)
        {
            var collection = await GetCollection();
            try
            {
                await collection.InsertOneAsync(skybox);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        public async Task<bool> Insert(IMongoDatabase db, IEnumerable<BlockadeSkybox> skyboxes)
        {
            var collection = await GetCollection();
            try
            {
                await collection.InsertManyAsync(skyboxes);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }
    }
    
    public record BlockadeSkybox
    {
        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /* Skybox Generation Input */
        public string Prompt { get; set; } = "";
        public string StyleName { get; set; } = "";
        public int StyleId { get; set; } = -1;
        
        /* Skybox Generation Output */
        public string FileUrl { get; set; } = "";
        public string ThumbUrl { get; set; } = "";
        public string DepthMapUrl { get; set; } = "";
        
        /* Vector Search Fields */
        public float[] Embedding { get; set; } = Array.Empty<float>();

        [BsonElement("score")]
        public double? Score { get; set; }
    }
}