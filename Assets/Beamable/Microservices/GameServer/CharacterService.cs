using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Anthropic;
using Beamable.Common;
using Beamable.Server;
using Beamable.StorageObjects.GameDatabase;

namespace Beamable.Microservices
{
    public class CharacterService
    {
        private PromptService _promptService;
        private ClaudeApi _claudeApi;
        private CampaignCharacterCollection _characterCollection;
        
        public CharacterService(PromptService promptService, ClaudeApi claudeApi, CampaignCharacterCollection characterCollection)
        {
            _promptService = promptService;
            _claudeApi = claudeApi;
            _characterCollection = characterCollection;
        }
        
        public async Task<CampaignCharacter> GetCharacter(string campaignName, long playerId)
        {
            var characters = await _characterCollection.GetCharacters(campaignName, playerId.ToString());
            if (!characters.Any())
            {
                throw new MicroserviceException(404, "CharacterNotFound",
                    "A character has not been created yet for this campaign.");
            }

            return characters.First();
        }

        public async Task<CampaignCharacter> GenerateCharacterSheet(string card1, string card2, string card3)
        {
	        // Feed Character Creation Prompt into Claude to obtain a character sheet
			var characterPrompt = _promptService.GetClaudeCharacterPrompt(card1, card2, card3);
			var claudeResponse = await _claudeApi.Send(new ClaudeCompletionRequest
			{
				Prompt = $"\n\nHuman: {characterPrompt}\n\nAssistant:",
				Model = ClaudeModels.ClaudeInstantLatestV1,
				MaxTokensToSample = 100000
			});

			CampaignCharacter newCharacter;
			try
			{
				var xml = Regex.Replace(claudeResponse.Completion, @"<\?xml.*\?>", "").Trim();
				XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
				xmlDoc.LoadXml($"<root>{xml}</root>");
				int.TryParse(xmlDoc.GetElementsByTagName("hp")[0].InnerText, out var hp);
				int.TryParse(xmlDoc.GetElementsByTagName("strength")[0].InnerText, out var strength);
				int.TryParse(xmlDoc.GetElementsByTagName("dexterity")[0].InnerText, out var dexterity);
				int.TryParse(xmlDoc.GetElementsByTagName("intelligence")[0].InnerText, out var intelligence);
				int.TryParse(xmlDoc.GetElementsByTagName("wisdom")[0].InnerText, out var wisdom);
				int.TryParse(xmlDoc.GetElementsByTagName("charisma")[0].InnerText, out var charisma);
				int.TryParse(xmlDoc.GetElementsByTagName("constitution")[0].InnerText, out var constitution);

				newCharacter = new CampaignCharacter
				{
					Name = xmlDoc.GetElementsByTagName("name")[0].InnerText,
					Gender = xmlDoc.GetElementsByTagName("gender")[0].InnerText,
					Race = xmlDoc.GetElementsByTagName("race")[0].InnerText,
					Class = xmlDoc.GetElementsByTagName("class")[0].InnerText,
					Description = xmlDoc.GetElementsByTagName("description")[0].InnerText,
					Background = xmlDoc.GetElementsByTagName("background")[0].InnerText,
					Strength = strength,
					Dexterity = dexterity,
					Constitution = constitution,
					Intelligence = intelligence,
					Wisdom = wisdom,
					Charisma = charisma,
					Health = hp,
					Mana = hp,
					Level = 1,
					NemesisName = xmlDoc.GetElementsByTagName("nemesis")[0].InnerText,
					NemesisDescription = xmlDoc.GetElementsByTagName("nemesis_description")[0].InnerText
				};
			}
			catch (Exception ex)
			{
				BeamableLogger.LogException(ex);
				throw new MicroserviceException(500, "UnableToGenerateCharacter", "Failed to parse LLM output during character creation.");
			}

			return newCharacter;
        }

        public async Task SaveCharacter(CampaignCharacter newCharacter)
        {
	        var inserted = await _characterCollection.Insert(newCharacter);
	        if (!inserted)
	        {
		        throw new MicroserviceException(500, "UnableToSaveCharacter",
			        "Failed to save the new character to the database.");
	        }
        }
    }
}