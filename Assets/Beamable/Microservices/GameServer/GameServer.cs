using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Anthropic;
using Beamable.Microservices.ChatRpg.Storage;
using Beamable.Server;
using BlockadeLabs;
using OpenAI;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace Beamable.Microservices
{
	[Microservice("GameServer")]
	public class GameServer : Microservice
	{
		[ConfigureServices]
		public static void Configure(IServiceBuilder builder)
		{
			// Singleton
			builder.Builder.AddSingleton<Config>();
			builder.Builder.AddSingleton<Scenario>();
			builder.Builder.AddSingleton<SkyboxApi>();
			builder.Builder.AddSingleton(p => new HttpClient());
			
			// Scoped
			builder.Builder.AddScoped<Claude>();
			builder.Builder.AddScoped<OpenAI_API>();
			builder.Builder.AddScoped<PromptService>();
		}
		
		[InitializeServices]
		public static async Task Init(IServiceInitializer init)
		{
			var config = init.GetService<Config>();
			await config.Init();
		}

		private async Task<CampaignCharacter> GetCharacter(string campaignName, long playerId)
		{
            var db = await Storage.GameDatabaseDatabase();
            var characters = await CampaignCharacterCollection.GetCharacters(db, campaignName, Context.UserId.ToString());
            if (!characters.Any())
            {
                throw new MicroserviceException(404, "CharacterNotFound",
                    "A character has not been created yet for this campaign.");
            }

            return characters.First();
        }

		private CampaignEvent ParseCampaignEventFromXML(string campaignName, string xml)
		{
            XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
            xmlDoc.LoadXml(xml);
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

		[ClientCallable("character/new")]
		public async Task<CharacterView> NewCharacter(string card1, string card2, string card3)
		{
			var db = await Storage.GameDatabaseDatabase();
			var defaultCampaignName = "DefaultCampaign";
			
			//TODO: Fetch a skybox based on the starting location of the campaign
			//Either generate one, or do a vector search
			var defaultSkyboxUrl =
				"https://blockade-platform-production.s3.amazonaws.com/images/imagine/high_quality_detailed_digital_painting_cd_vr_computer_render_fantasy__6607fe49fc7369ec__5716463_.jpg?ver=1";
			
			// Feed Character Creation Prompt into Claude to obtain a character sheet
			var promptService = Provider.GetService<PromptService>();
			var characterPrompt = promptService.GetClaudeCharacterPrompt(card1, card2, card3);
			var claude = Provider.GetService<Claude>();
			var claudeResponse = await claude.Send(new ClaudeCompletionRequest
			{
				Prompt = $"\n\nHuman: {characterPrompt}\n\nAssistant:",
				Model = ClaudeModels.ClaudeInstantV1_1_100k,
				MaxTokensToSample = 100000
			});

			CampaignCharacter newCharacter;
			try
			{
				XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
				xmlDoc.LoadXml($"<root>{claudeResponse.Completion}</root>");
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
				Debug.LogError(ex);
				throw new MicroserviceException(500, "UnableToGenerateCharacter", "Failed to parse LLM output during character creation.");
			}

			var characterJson = JsonConvert.SerializeObject(newCharacter.ToCharacterView());
			await Services.Notifications.NotifyPlayer(Context.UserId, "character.preview", characterJson);

			//TODO: Perform a Vector Search to see if we can find a good portrait match
			// Need to implement OpenAI embeddings
			// Generate Scenario Portrait
			var scenario = Provider.GetService<Scenario>();
			var scenarioModel = "EnoXc8q4QlqAfCfoObmW3Q"; // Public Scenario Generator
			var scenarioPrompt = $"Portrait,D&D,{newCharacter.Gender},{newCharacter.Class},{newCharacter.Race},{newCharacter.Description.Replace(",", " ")}";
			var scenarioRsp = await scenario.CreateInference(scenarioModel, scenarioPrompt);
			var completedInference = await scenario.PollInferenceToCompletion(scenarioRsp.inference.modelId, scenarioRsp.inference.id);
			
			// Generate Embeddings
			var openAI = Provider.GetService<OpenAI_API>();
			var scenarioEmbeddings = await openAI.GetEmbeddings(completedInference.inference.displayPrompt);
			var scenarioAsset = new ScenarioAsset
			{
				Prompt = scenarioPrompt,
				Model = completedInference.inference.modelId,
				FileUrl = completedInference.inference.images.Select(i => i.url).FirstOrDefault(),
				Content = scenarioPrompt,
				Embedding = scenarioEmbeddings
			};
			var insertedScenario = await ScenarioAssetsCollection.Insert(db, scenarioAsset);
			if (!insertedScenario)
			{
				throw new MicroserviceException(500, "UnableToSavePortrait",
					"Failed to save character portrait inference data to database.");
			}
			
			// Further Enrich Character Data
			newCharacter.CampaignName = defaultCampaignName;
			newCharacter.PlayerId = Context.UserId.ToString();
			newCharacter.SkyboxUrl = defaultSkyboxUrl;
			newCharacter.PortraitUrl = completedInference.inference.images.Select(i => i.url).FirstOrDefault();
			
			var inserted = await CampaignCharacterCollection.Insert(db, newCharacter);
			if (!inserted)
			{
				throw new MicroserviceException(500, "UnableToSaveCharacter",
					"Failed to save the new character to the database.");
			}

			await Services.Notifications.NotifyPlayer(Context.UserId, "character.created", "");
			return newCharacter.ToCharacterView();
		}

		[ClientCallable("character/get")]
		public async Task<CharacterView> GetCharacter()
		{
			var db = await Storage.GameDatabaseDatabase();
			var defaultCampaignName = "DefaultCampaign";
			
			var characters = await CampaignCharacterCollection.GetCharacters(db, defaultCampaignName, Context.UserId.ToString());
			if (!characters.Any())
			{
				throw new MicroserviceException(404, "CharacterNotFound",
					"A character has not been created yet for this campaign.");
			}

			return characters.First().ToCharacterView();
		}

		[ClientCallable("adventure/ready")]
		public async Task<WorldState> Ready()
		{
            var claude = Provider.GetService<Claude>();
            var promptService = Provider.GetService<PromptService>();

            var db = await Storage.GameDatabaseDatabase();
            var campaignName = "DefaultCampaign";
            var character = await GetCharacter(campaignName, Context.UserId);
			var campaignEvents = await CampaignEventsCollection.GetAscendingCampaignEvents(db, campaignName);

			var newCampaign = campaignEvents.Count == 0;
            var prompt = promptService.AdventurePromptV2(character, campaignEvents);
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

            var response = await claude.Send(new ClaudeCompletionRequest
            {
                Prompt = $"\n\nHuman: {prompt}\n\nAssistant:",
                Model = ClaudeModels.ClaudeV1_3_100k,
                MaxTokensToSample = 100000
            });

			var campaignEvent = ParseCampaignEventFromXML(campaignName, $"<root>{response.Completion}</root>");
			if(newCampaign)
			{
				campaignEvent.SkyboxUrl = "https://blockade-platform-production.s3.amazonaws.com/images/imagine/high_quality_detailed_digital_painting_cd_vr_computer_render_fantasy__6c9ca4a642910ce8__6154819_.jpg?ver=1";
				var inserted = await CampaignEventsCollection.Insert(db, campaignEvent);
				if (!inserted)
					throw new MicroserviceException(500, "FailedToSaveCampaignEvent", "Failed to persist initial campaign event to the database.");
			}
			else
            {
				campaignEvent.SkyboxUrl = campaignEvents.Last().SkyboxUrl;
            }

            var worldState = campaignEvent.ToWorldState();
            var worldJson = JsonConvert.SerializeObject(worldState);
			await Services.Notifications.NotifyPlayer(Context.UserId, "world.update", worldJson);

			return worldState;
        }

        [ClientCallable("adventure/play")]
        public async Task<WorldState> Play(AdventurePlayRequest request)
        {
            var claude = Provider.GetService<Claude>();
            var promptService = Provider.GetService<PromptService>();

            var db = await Storage.GameDatabaseDatabase();
            var campaignName = "DefaultCampaign";
			var character = await GetCharacter(campaignName, Context.UserId);
			var campaignEvents = await CampaignEventsCollection.GetAscendingCampaignEvents(db, campaignName);

			var prompt = promptService.AdventurePromptV2(character, campaignEvents);
			prompt += $"\n\n[{character.Name}]: {request.playerAction}";
            var response = await claude.Send(new ClaudeCompletionRequest
            {
                Prompt = $"\n\nHuman: {prompt}\n\nAssistant:",
                Model = ClaudeModels.ClaudeV1_3_100k,
                MaxTokensToSample = 100000
            });

			var xml = $"<root>{response.Completion}</root>";
			var campaignEvent = ParseCampaignEventFromXML(campaignName, xml);
			var inserted = await CampaignEventsCollection.Insert(db, campaignEvent);
            if (!inserted)
                throw new MicroserviceException(500, "FailedToSaveCampaignEvent", "Failed to persist campaign event to the database.");

            var worldState = campaignEvent.ToWorldState();
            var worldJson = JsonConvert.SerializeObject(worldState);
            await Services.Notifications.NotifyPlayer(Context.UserId, "world.update", worldJson);

			//Update Skybox
			//TODO: Search Vector Database first
			var skyboxStyle = SkyboxStyles.FANTASY_LAND;
			var blockade = Provider.GetService<SkyboxApi>();

			Debug.Log("Creating Skybox...");
			var inferenceRsp = await blockade.CreateSkybox(skyboxStyle, campaignEvent.Description);
			if (!inferenceRsp.IsCompleted)
			{
				Debug.Log("Polling skybox for completion...");
				var rsp = await blockade.PollSkyboxToCompletion(inferenceRsp.Id.Value);
				inferenceRsp = rsp.Request;
			}
			campaignEvent.SkyboxUrl = inferenceRsp.FileUrl;
			var updated = await CampaignEventsCollection.Replace(db, campaignEvent);
			if (!updated)
				throw new MicroserviceException(500, "FailedToUpdateCampaignEvent", "Failed to update campaign event skybox in the database.");

			worldState = campaignEvent.ToWorldState();
			worldJson = JsonConvert.SerializeObject(worldState);
			await Services.Notifications.NotifyPlayer(Context.UserId, "world.update", worldJson);

			// Get Embeddings
			var openAI = Provider.GetService<OpenAI_API>();
			var embeddingsContent = $"{skyboxStyle}: {campaignEvent.Description}";
			var embeddingsVector = await openAI.GetEmbeddings(embeddingsContent);

			var insertedSkybox = await BlockadeSkyboxesCollection.Insert(db, new BlockadeSkybox
			{
				StyleId = (int)skyboxStyle,
				StyleName = skyboxStyle.ToString(),
				Prompt = campaignEvent.Description,
				FileUrl = inferenceRsp.FileUrl,
				DepthMapUrl = inferenceRsp.DepthMapUrl,
				ThumbUrl = inferenceRsp.ThumbUrl,
				Content = embeddingsContent,
				Embedding = embeddingsVector
			});

			if (!insertedSkybox)
				throw new MicroserviceException(500, "FailedToSaveSkybox", "Failed to save skybox to the database.");

			return worldState;
        }
		
		[ClientCallable("claude")]
		public async Task<string> TestClaude(string prompt)
		{
			// This code executes on the server.
			var claude = Provider.GetService<Claude>();
			var response = await claude.Send(new ClaudeCompletionRequest
			{
				Prompt = $"\n\nHuman: {prompt}\n\nAssistant:",
				Model = ClaudeModels.ClaudeV1_3_100k,
				MaxTokensToSample = 100000
			});

			return response.Completion;
		}

		[ClientCallable("scenario")]
		public async Task<string> TestScenario(string prompt)
		{
			// This code executes on the server.
			var model = "RNHPqrHFQYu-oqZGb5VBtg";
			var scenario = Provider.GetService<Scenario>();

			var inferenceRsp = await scenario.CreateInference(model, "dungeons & dragons, level 1, wizard");
			if (!inferenceRsp.inference.IsCompleted)
			{
				Debug.Log("Polling for inference...");
				inferenceRsp =
					await scenario.PollInferenceToCompletion(inferenceRsp.inference.modelId, inferenceRsp.inference.id);
			}

			var url = inferenceRsp.inference.images.FirstOrDefault().url;
			Debug.Log(url);

			return url;
		}

		[ClientCallable("blockade")]
		public async Task<string> TestBlockade(string prompt)
		{
			var skyboxStyle = SkyboxStyles.FANTASY_LAND;
			var blockade = Provider.GetService<SkyboxApi>();
			
			Debug.Log("Creating Skybox...");
			var inferenceRsp = await blockade.CreateSkybox(skyboxStyle, prompt);
			if (!inferenceRsp.IsCompleted)
			{
				Debug.Log("Polling skybox for completion...");
				var rsp = await blockade.PollSkyboxToCompletion(inferenceRsp.Id.Value);
				inferenceRsp = rsp.Request;
			}
			
			Debug.Log($"Skybox creation complete: {inferenceRsp.FileUrl}");
			await Services.Notifications.NotifyPlayer(Context.UserId, "blockade.reply", inferenceRsp.FileUrl);

			// Get Embeddings
			var openAI = Provider.GetService<OpenAI_API>();
			var embeddingsContent = $"{skyboxStyle.ToString()}: {prompt}";
			var embeddingsVector = await openAI.GetEmbeddings(embeddingsContent);
			
			var db = await Storage.GameDatabaseDatabase();
			var inserted = await BlockadeSkyboxesCollection.Insert(db, new BlockadeSkybox
			{
				StyleId = (int)skyboxStyle,
				StyleName = skyboxStyle.ToString(),
				Prompt = prompt,
				FileUrl = inferenceRsp.FileUrl,
				DepthMapUrl = inferenceRsp.DepthMapUrl,
				ThumbUrl = inferenceRsp.ThumbUrl,
				Content = embeddingsContent,
				Embedding = embeddingsVector
			});

			if (!inserted)
			{
				Debug.LogError("Failed to insert blockade document into database.");
			}
			
			return inferenceRsp.FileUrl;
		}
	}
}
