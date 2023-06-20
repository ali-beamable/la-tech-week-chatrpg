using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.Microservices.ChatRpg.Storage
{
    public static class CampaignEventsCollection
    {
        private static readonly string _collectionName = "campaign.events";

        private static IMongoCollection<CampaignEvent> _collection;
        private static readonly IEnumerable<CreateIndexModel<CampaignEvent>> _indexes = new CreateIndexModel<CampaignEvent>[]
        {
            new CreateIndexModel<CampaignEvent>(
                Builders<CampaignEvent>.IndexKeys.Descending(x => x.CampaignName).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Unique = true }
            )
        };

        public static async ValueTask<IMongoCollection<CampaignEvent>> Get(IMongoDatabase db)
        {
            if (_collection is null)
            {
                _collection = db.GetCollection<CampaignEvent>(_collectionName);
                if (_indexes.Count() > 0)
                {
                    await _collection.Indexes.CreateManyAsync(_indexes);
                }
            }

            return _collection;
        }

        public static async Task<List<CampaignEvent>> GetOrderedCampaignEvents(IMongoDatabase db, string campaignName)
        {
            var collection = await Get(db);
            var query = Builders<CampaignEvent>.Filter.Eq(x => x.CampaignName, campaignName);
            var results = await collection.Find(query).ToListAsync();

            return results.OrderByDescending(x => x.CreatedAt).ToList();
        }

        public static async Task<bool> Insert(IMongoDatabase db, CampaignEvent campaignEvent)
        {
            var collection = await Get(db);
            try
            {
                await collection.InsertOneAsync(campaignEvent);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        public static async Task<bool> Replace(IMongoDatabase db, CampaignEvent campaignEvent)
        {
            var collection = await Get(db);
            try
            {
                var filter = Builders<CampaignEvent>.Filter.Eq(x => x.Id, campaignEvent.Id);
                await collection.ReplaceOneAsync(filter, campaignEvent);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        public static async Task<bool> Insert(IMongoDatabase db, IEnumerable<CampaignEvent> campaignEvents)
        {
            var collection = await Get(db);
            try
            {
                await collection.InsertManyAsync(campaignEvents);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }
    }

    public record CampaignEvent
    {
        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CampaignName { get; set; }
        public string RoomName { get; set; }
        public string Description { get; set; }
        public string Music { get; set; }
        public string Story { get; set; }
        public string[] Characters { get; set; }
        public string[] Items { get; set; }
        public string DM { get; set; }
        public string SkyboxUrl { get; set; }

        public WorldState ToWorldState()
        {
            return new WorldState
            {
                roomName = RoomName,
                description = Description,
                music = Music,
                story = Story,
                characters = Characters,
                items = Items,
                skyboxUrl = SkyboxUrl,
                dm = DM
            };
        }
    }
}