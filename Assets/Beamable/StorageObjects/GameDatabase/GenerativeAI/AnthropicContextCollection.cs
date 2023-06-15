using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.Microservices.ChatRpg.Storage
{
	public static class AnthropicContextCollection
    {
        private static readonly string _collectionName = "anthropic.context";

        private static IMongoCollection<AnthropicContextFragment> _collection;
        private static readonly IEnumerable<CreateIndexModel<AnthropicContextFragment>> _indexes = new CreateIndexModel<AnthropicContextFragment>[]
        {
            new CreateIndexModel<AnthropicContextFragment>(
                Builders<AnthropicContextFragment>.IndexKeys.Hashed(x => x.CampaignName)
            )
        };

        public static async ValueTask<IMongoCollection<AnthropicContextFragment>> Get(IMongoDatabase db)
        {
            if (_collection is null)
            {
                _collection = db.GetCollection<AnthropicContextFragment>(_collectionName);
                if(_indexes.Count() > 0) { 
                    await _collection.Indexes.CreateManyAsync(_indexes);
                }
            }

            return _collection;
        }

        public static async Task<List<AnthropicContextFragment>> VectorSearch(IMongoDatabase db, float[] query)
        {
            var collection = await Get(db);
            var knnBeta = new BsonDocument { { "path", "Embedding"}, { "k", 15}, { "vector", new BsonArray(query) } };
            var searchStage = new BsonDocument { { "index", "embedding" }, { "knnBeta", knnBeta } };
            var projectStage = new BsonDocument { { "embedding", 0 }, { "_id", 0 }, { "score", new BsonDocument("$meta", "searchScore") } };

            PipelineDefinition<AnthropicContextFragment, AnthropicContextFragment> pipeline = new BsonDocument[]
            {
                new BsonDocument("$search",  searchStage),
                new BsonDocument("$project",  projectStage)
            };

            return await collection.Aggregate(pipeline).ToListAsync();
        }

        public static async Task<List<string>> GetCampaigns(IMongoDatabase db)
        {
            var collection = await Get(db);
            var query = Builders<AnthropicContextFragment>.Filter.Empty;
            var cursor = await collection.DistinctAsync(fragment => fragment.CampaignName, query);
            var resultList = await cursor.ToListAsync();

            return resultList;
        }
        
        public static async Task<List<AnthropicContextFragment>> GetContextByCampaign(IMongoDatabase db, string campaignName)
        {
            var collection = await Get(db);
            var query = Builders<AnthropicContextFragment>.Filter.Eq(x => x.CampaignName, campaignName);
            var results = await collection.Find(query).ToListAsync();

            return results;
        }

        public static async Task<bool> DeleteAll(IMongoDatabase db)
        {
            var collection = await Get(db);
            var result = await collection.DeleteManyAsync(new BsonDocument());

            return result.IsAcknowledged;
        }

        public static async Task<bool> DeleteCampaign(IMongoDatabase db, string campaignName)
        {
            var collection = await Get(db);
            var query = Builders<AnthropicContextFragment>.Filter.Eq(
            document => document.CampaignName, campaignName
            );
            var result = await collection.DeleteManyAsync(query);

            return result.IsAcknowledged;
        }

        public static async Task<bool> Insert(IMongoDatabase db, AnthropicContextFragment fragment)
        {
            var collection = await Get(db);
            try
            {
                await collection.InsertOneAsync(fragment);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        public static async Task<bool> Insert(IMongoDatabase db, IEnumerable<AnthropicContextFragment> fragments)
        {
            var collection = await Get(db);
            try
            {
                 await collection.InsertManyAsync(fragments);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }
    }

    public record AnthropicContextFragment
    {
        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public DateTime CreatedAt { get; set; }

        public string CampaignName { get; set; } = "";
        public string Content { get; set; } = "";
        public float[] Embedding { get; set; } = Array.Empty<float>();

        [BsonElement("score")]
        public double? Score { get; set; }
    }
}