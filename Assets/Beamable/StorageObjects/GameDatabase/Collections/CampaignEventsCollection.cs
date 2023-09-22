using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Linq;
using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.StorageObjects.GameDatabase
{
    public class CampaignEventsCollection : MongoCollection<Server.GameDatabase, CampaignEvent>
    {
        protected override string CollectionName => "campaign.events";
        protected override IEnumerable<CreateIndexModel<CampaignEvent>> Indexes => new []
        {
            new CreateIndexModel<CampaignEvent>(
                Builders<CampaignEvent>.IndexKeys.Ascending(x => x.CampaignName).Ascending(x => x.CreatedAt)
            )
        };
        
        public CampaignEventsCollection(IStorageObjectConnectionProvider connectionProvider) : base(connectionProvider){}
        
        public async Task<List<CampaignEvent>> GetAscendingCampaignEvents(string campaignName)
        {
            var collection = await GetCollection();
            var query = Builders<CampaignEvent>.Filter.Eq(x => x.CampaignName, campaignName);
            var results = await collection.Find(query).ToListAsync();

            return results.OrderBy(x => x.CreatedAt).ToList();
        }

        public async Task<bool> Insert(CampaignEvent campaignEvent)
        {
            var collection = await GetCollection();
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

        public async Task<bool> Replace(CampaignEvent campaignEvent)
        {
            var collection = await GetCollection();
            try
            {
                var filter = Builders<CampaignEvent>.Filter.Eq(x => x.Id, campaignEvent.Id);
                await collection.ReplaceOneAsync(filter, campaignEvent, new ReplaceOptions{ IsUpsert = true });
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