using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.Microservices.ChatRpg.Storage
{
	public static class BlockadeSkyboxesCollection
    {
        private static readonly string _collectionName = "blockadelabs.skyboxes";

        private static IMongoCollection<BlockadeSkybox> _collection;
        private static readonly IEnumerable<CreateIndexModel<BlockadeSkybox>> _indexes = new CreateIndexModel<BlockadeSkybox>[]
        {
            new CreateIndexModel<BlockadeSkybox>(
                Builders<BlockadeSkybox>.IndexKeys.Hashed(x => x.FileUrl)
            )
        };

        public static async ValueTask<IMongoCollection<BlockadeSkybox>> Get(IMongoDatabase db)
        {
            if (_collection is null)
            {
                _collection = db.GetCollection<BlockadeSkybox>(_collectionName);
                if(_indexes.Count() > 0) { 
                    await _collection.Indexes.CreateManyAsync(_indexes);
                }
            }

            return _collection;
        }

        public static async Task<List<BlockadeSkybox>> VectorSearch(IMongoDatabase db, float[] query)
        {
            var collection = await Get(db);
            var knnBeta = new BsonDocument { { "path", "Embedding"}, { "k", 15}, { "vector", new BsonArray(query) } };
            var searchStage = new BsonDocument { { "index", "embedding" }, { "knnBeta", knnBeta } };
            var projectStage = new BsonDocument { { "embedding", 0 }, { "_id", 0 }, { "score", new BsonDocument("$meta", "searchScore") } };

            PipelineDefinition<BlockadeSkybox, BlockadeSkybox> pipeline = new BsonDocument[]
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

        public static async Task<bool> Insert(IMongoDatabase db, BlockadeSkybox skybox)
        {
            var collection = await Get(db);
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

        public static async Task<bool> Insert(IMongoDatabase db, IEnumerable<BlockadeSkybox> skyboxes)
        {
            var collection = await Get(db);
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
        public string Content { get; set; } = "";
        public float[] Embedding { get; set; } = Array.Empty<float>();

        [BsonElement("score")]
        public double? Score { get; set; }
    }
}