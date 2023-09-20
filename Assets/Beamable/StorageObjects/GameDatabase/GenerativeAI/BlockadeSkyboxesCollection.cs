using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Linq;
using Beamable.Common;
using Beamable.StorageObjects.GameDatabase;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.Microservices.ChatRpg.Storage
{
	public static class BlockadeSkyboxesCollection
    {
        private static readonly string _collectionName = "blockadelabs.skyboxes";

        private static IMongoCollection<BlockadeSkybox> _collection;
        private static readonly IEnumerable<CreateIndexModel<BlockadeSkybox>> _indexes = new []
        {
            new CreateIndexModel<BlockadeSkybox>(
                Builders<BlockadeSkybox>.IndexKeys.Hashed(x => x.FileUrl)
            )
        };
        
        private static readonly IEnumerable<CreateVectorIndexModel<BlockadeSkybox>> _vectorIndexes = new []
        {
            new CreateVectorIndexModel<BlockadeSkybox>("vector-search-index", x => x.Embedding)
        };

        private static Task<BsonDocument> CreateVectorIndexes(IMongoDatabase db)
        {
            var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument
            {
                { "createSearchIndexes", _collectionName}, 
                { "indexes", new BsonArray(_vectorIndexes.Select(i => i.ToBson())) }
            });
            
            return db.RunCommandAsync<BsonDocument>(command);
        }
        
        private static Task<List<AtlasSearchIndex>> GetSearchIndexes(IMongoDatabase db)
        {
            var collection = db.GetCollection<AtlasSearchIndex>(_collectionName);
            PipelineDefinition<AtlasSearchIndex, AtlasSearchIndex> pipeline = new []
            {
                new BsonDocument("$listSearchIndexes",  new BsonDocument())
            };

            return collection.Aggregate(pipeline).ToListAsync();
        }

        public static async ValueTask<IMongoCollection<BlockadeSkybox>> Get(IMongoDatabase db)
        {
            if (_collection is null)
            {
                _collection = db.GetCollection<BlockadeSkybox>(_collectionName);
                if(_indexes.Any()) { 
                    await _collection.Indexes.CreateManyAsync(_indexes);
                }

                if (_vectorIndexes.Any())
                {
                    try
                    {
                        await CreateVectorIndexes(db);
                    }
                    // IndexAlreadyExists
                    catch (MongoCommandException ex) when(ex.Code == 68)
                    {
                        BeamableLogger.Log("Vector Index already exists, continuing.");
                    }
                }
            }

            return _collection;
        }

        public static async Task<List<BlockadeSkybox>> VectorSearch(IMongoDatabase db, float[] query, double scoreCutoff)
        {
            var collection = await Get(db);
            var knnBeta = new BsonDocument { { "path", "Embedding"}, { "k", 15}, { "vector", new BsonArray(query) } };
            var searchStage = new BsonDocument { { "index", "vector-search-index" }, { "knnBeta", knnBeta } };
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
        public float[] Embedding { get; set; } = Array.Empty<float>();

        [BsonElement("score")]
        public double? Score { get; set; }
    }
}