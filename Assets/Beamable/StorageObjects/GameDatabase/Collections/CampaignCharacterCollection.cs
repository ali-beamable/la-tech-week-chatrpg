using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Beamable.Server;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.StorageObjects.GameDatabase
{
    public class CampaignCharacterCollection : MongoCollection<Server.GameDatabase, CampaignCharacter>
    {
        protected override string CollectionName => "campaign.characters";
        protected override IEnumerable<CreateIndexModel<CampaignCharacter>> Indexes => new []
        {
            new CreateIndexModel<CampaignCharacter>(
                Builders<CampaignCharacter>.IndexKeys.Ascending(x => x.CampaignName).Ascending(x => x.PlayerId)
            )
        };

        public CampaignCharacterCollection(IStorageObjectConnectionProvider connectionProvider) : base(connectionProvider){}
        
        public async Task<List<CampaignCharacter>> GetCharacters(string campaignName, string playerId)
        {
            var collection = await GetCollection();
            var query = Builders<CampaignCharacter>.Filter.Eq(x => x.CampaignName, campaignName) & 
                        Builders<CampaignCharacter>.Filter.Eq(x => x.PlayerId, playerId);
            var results = await collection.Find(query).ToListAsync();

            return results;
        }

        public async Task<bool> Insert(CampaignCharacter character)
        {
            var collection = await GetCollection();
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
    }
    
    public record CampaignCharacter
    {
        [BsonElement("_id")]
        public string PlayerId { get; set; }
        public string CampaignName { get; set; }
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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

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