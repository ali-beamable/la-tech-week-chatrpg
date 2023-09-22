using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Anthropic;
using Beamable.Common;
using Beamable.Server;
using Beamable.StorageObjects.GameDatabase;
using UnityEngine;

namespace Beamable.Microservices
{
    public class CampaignService
    {
        private PromptService _promptService;
        private ClaudeApi _claudeApi;
        private CampaignEventsCollection _campaignEventsCollection;
        
        public CampaignService(PromptService promptService, ClaudeApi claudeApi, CampaignEventsCollection campaignEventsCollection)
        {
            _promptService = promptService;
            _claudeApi = claudeApi;
            _campaignEventsCollection = campaignEventsCollection;
        }

        public async Task<WorldState> GetWorldState(string campaignName, CampaignCharacter character)
        {
	        var campaignEvents = await _campaignEventsCollection.GetAscendingCampaignEvents(campaignName);

	        var newCampaign = campaignEvents.Count == 0;
	        var prompt = _promptService.AdventurePromptV2(character, campaignEvents);
	        if (newCampaign)
	        {
		        // This is a new campaign, inject a starting point
		        prompt += "\n\nBriefly introduce the campaign to the player in narrative form.";
	        }
	        else
	        {
		        // Summarize the events of the campaign so far
		        prompt += "\n\nBriefly summarize the campaign so far, adopting a tone appropriate for a returning player that may have forgotten some details.";
	        }

	        var response = await _claudeApi.Send(new ClaudeCompletionRequest
	        {
		        Prompt = $"\n\nHuman: {prompt}\n\nAssistant:",
		        Model = ClaudeModels.ClaudeInstantLatestV1,
		        MaxTokensToSample = 100000
	        });

	        var campaignEvent = ParseCampaignEventFromXML(campaignName, response.Completion);
	        if(newCampaign)
	        {
		        campaignEvent.SkyboxUrl = "https://blockade-platform-production.s3.amazonaws.com/images/imagine/high_quality_detailed_digital_painting_cd_vr_computer_render_fantasy__6c9ca4a642910ce8__6154819_.jpg?ver=1";
		        var inserted = await _campaignEventsCollection.Insert(campaignEvent);
		        if (!inserted)
			        throw new MicroserviceException(500, "FailedToSaveCampaignEvent", "Failed to persist initial campaign event to the database.");
	        }
	        else
	        {
		        campaignEvent.SkyboxUrl = campaignEvents.Last().SkyboxUrl;
	        }

	        return campaignEvent.ToWorldState();
        }

        public async Task<CampaignEvent> GenerateCampaignEvent(string campaignName, CampaignCharacter character, string playerAction)
        {
	        var campaignEvents = await _campaignEventsCollection.GetAscendingCampaignEvents(campaignName);
	        
	        var prompt = _promptService.AdventurePromptV2(character, campaignEvents);
	        prompt += $"\n\n[{character.Name}]: {playerAction}";
	        var response = await _claudeApi.Send(new ClaudeCompletionRequest
	        {
		        Prompt = $"\n\nHuman: {prompt}\n\nAssistant:",
		        Model = ClaudeModels.ClaudeInstantLatestV1,
		        MaxTokensToSample = 100000
	        });

	        return ParseCampaignEventFromXML(campaignName, response.Completion);
        }

        public async Task SaveCampaignEvent(CampaignEvent campaignEvent)
        {
	        var saved = await _campaignEventsCollection.Replace(campaignEvent);
	        if (!saved)
		        throw new MicroserviceException(500, "FailedToSaveCampaignEvent", "Failed to persist campaign event to the database.");
        }
        
        private CampaignEvent ParseCampaignEventFromXML(string campaignName, string completion)
		{
			var xml = Regex.Replace(completion, @"<\?xml.*\?>", "").Trim();
			XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
            try
            {
	            xmlDoc.LoadXml($"<root>{xml}</root>");
            }
            catch (XmlException ex)
            {
	            BeamableLogger.LogError($"Unable to parse LLM XML:\n{xml}");
	            BeamableLogger.LogException(ex);
	            throw new MicroserviceException(500, "InvalidGenerativeResponse", "The LLM responsible for narrative text outputted an invalid response. Please try again with a different prompt.");
            }
            
            var parsedRoomName = xmlDoc.GetElementsByTagName("ROOM_NAME")[0].InnerText;
            var parsedStory = xmlDoc.GetElementsByTagName("STORY")[0].InnerText;
            var parsedDescription = xmlDoc.GetElementsByTagName("DESCRIPTION")[0].InnerText;
            var parsedMusic = xmlDoc.GetElementsByTagName("MUSIC")[0].InnerText;

			string[] parsedItems = Array.Empty<string>();
			var xmlItems = xmlDoc.GetElementsByTagName("ITEMS");
			if(xmlItems.Count > 0)
			{
				parsedItems = xmlItems[0].InnerText.Trim().Replace(", ", ",").Split(",");
            }

            string[] parsedCharacters = Array.Empty<string>();
            var xmlCharacters = xmlDoc.GetElementsByTagName("CHARACTERS");
            if (xmlItems.Count > 0)
            {
                parsedCharacters = xmlCharacters[0].InnerText.Trim().Replace(", ", ",").Split(",");
            }

			string parsedDM = null;
			var xmlDM = xmlDoc.GetElementsByTagName("DM");
			if (xmlDM.Count > 0)
			{
				parsedDM = xmlDM[0].InnerText;
			}

			return new CampaignEvent
            {
                CampaignName = campaignName,
                RoomName = parsedRoomName,
                Characters = parsedCharacters,
                Items = parsedItems,
                Story = parsedStory,
                Description = parsedDescription,
                Music = parsedMusic,
				DM = parsedDM
            };
        }
    }
}