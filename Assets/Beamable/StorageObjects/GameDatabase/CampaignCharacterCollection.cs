using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.Microservices.ChatRpg.Storage
{
	public static class CampaignCharacterCollection
    {
        private static readonly string _collectionName = "campaign.characters";

        private static IMongoCollection<CampaignCharacter> _collection;
        private static readonly IEnumerable<CreateIndexModel<CampaignCharacter>> _indexes = new CreateIndexModel<CampaignCharacter>[]
        {
            new CreateIndexModel<CampaignCharacter>(
                Builders<CampaignCharacter>.IndexKeys.Hashed(x => x.Name)
            ),
            new CreateIndexModel<CampaignCharacter>(
                Builders<CampaignCharacter>.IndexKeys.Descending(x => x.CampaignName).Descending(x => x.PlayerId),
                new CreateIndexOptions { Unique = true }
            )
        };

        public static async ValueTask<IMongoCollection<CampaignCharacter>> Get(IMongoDatabase db)
        {
            if (_collection is null)
            {
                _collection = db.GetCollection<CampaignCharacter>(_collectionName);
                if(_indexes.Count() > 0) { 
                    await _collection.Indexes.CreateManyAsync(_indexes);
                }
            }

            return _collection;
        }
        
        public static async Task<List<CampaignCharacter>> GetCharacters(IMongoDatabase db, string campaignName, string playerId)
        {
            var collection = await Get(db);
            var query = Builders<CampaignCharacter>.Filter.Eq(x => x.CampaignName, campaignName) & 
                    Builders<CampaignCharacter>.Filter.Eq(x => x.PlayerId, playerId);
            var results = await collection.Find(query).ToListAsync();

            return results;
        }
        
        public static async Task<List<CampaignCharacter>> GetCharactersByCampaign(IMongoDatabase db, string campaignName)
        {
            var collection = await Get(db);
            var query = Builders<CampaignCharacter>.Filter.Eq(x => x.CampaignName, campaignName);
            var results = await collection.Find(query).ToListAsync();

            return results;
        }

        public static async Task<bool> Insert(IMongoDatabase db, CampaignCharacter character)
        {
            var collection = await Get(db);
            try
            {
                await collection.InsertOneAsync(character);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        public static async Task<bool> Insert(IMongoDatabase db, IEnumerable<CampaignCharacter> characters)
        {
            var collection = await Get(db);
            try
            {
                 await collection.InsertManyAsync(characters);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }
    }

    public record CampaignCharacter
    {
        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public string CampaignName { get; set; }
        public string PlayerId { get; set; }
        
        public string Name { get; set; }
        public string Class { get; set; }
        public string Description { get; set; }
        public string Background { get; set; }
        public string Race { get; set; }
        public string Gender { get; set; }
        
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Intelligence { get; set; }
        public int Constitution { get; set; }
        public int Charisma { get; set; }
        public int Wisdom { get; set; }
        
        public int Level { get; set; }
        public int Health { get; set; }
        public int Mana { get; set; }
        public string NemesisName { get; set; }
        public string NemesisDescription { get; set; }
        
        /* Generated from Scenario and BlockadeLabs respectively */
        public string PortraitUrl { get; set; }
        public string SkyboxUrl { get; set; }

        public CharacterView ToCharacterView()
        {
            return new CharacterView
            {
                characterClass = Class,
                characterDescription = Description,
                characterBackground = Background,
                characterGender = Gender,
                characterRace = Race,
                characterName = Name,
                characterLevel = Level.ToString(),

                strength = Strength,
                dexterity = Dexterity,
                constitution = Constitution,
                intelligence = Intelligence,
                charisma = Charisma,
                wisdom = Wisdom,
                
                currentHp = Health,
                maxHp = Health,
                currentMana = Mana,
                maxMana = Mana,

                imageUrl = PortraitUrl,
                skyboxUrl = SkyboxUrl,

                nemesisName = NemesisName,
                nemesisDescription = NemesisDescription
            };
        }
    }
}