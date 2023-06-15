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
		
		private static string Base64Decode(string base64EncodedData) 
		{
			var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
		}

		[ClientCallable("character/new")]
		public async Task<CharacterView> NewCharacter(string name, string gender, string card1, string card2, string card3)
		{
			var db = await Storage.GameDatabaseDatabase();
			var defaultCampaignName = "DefaultCampaign";
			
			//TODO: Fetch a skybox based on the starting location of the campaign
			//Either generate one, or do a vector search
			var defaultSkyboxUrl =
				"https://blockade-platform-production.s3.amazonaws.com/images/imagine/high_quality_detailed_digital_painting_cd_vr_computer_render_fantasy__6607fe49fc7369ec__5716463_.jpg?ver=1";
			
			// Feed Character Creation Prompt into Claude to obtain a character sheet
			var promptService = Provider.GetService<PromptService>();
			var characterPrompt = promptService.GetClaudeCharacterPrompt(name, gender, card1, card2, card3);
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
			
			await Services.Notifications.NotifyPlayer(Context.UserId, "character.preview", $"{newCharacter.Background}\n\n{newCharacter.Description}");

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

			/*
			return new CharacterView
			{
				characterClass = "Druid",
				characterLevel = "1",
				characterName = "Tarinth",
				currentHp = 200,
				maxHp = 500,
				currentMana = 23,
				maxMana = 120,
				characterGender = "Male",
				characterRace = "Dwarf",
				characterDescription = "A stout dwarf of the hill clans, wearing simple leathers and carrying a gnarled oaken staff, with a thick red beard and inquisitive eyes. Though quiet, his gaze seems to reflect hidden knowledge of the natural world.",
				nemesisName = "The Mad Wizard",
				nemesisDescription = "A crazed magic-user who covets ancient dwarven gold and relics, determined to raid the dungeons and crypts of all the dwarf clans to loot their treasures.",
				skyboxUrl = "https://blockade-platform-production.s3.amazonaws.com/images/imagine/high_quality_detailed_digital_painting_cd_vr_computer_render_fantasy__ab0d379706d4de4c__5758744_.jpg?ver=1",
				imageUrl = "https://cdn.cloud.scenario.com/assets/CMmu-yJxQnCBX48MCOv2XA?p=100&Expires=1687392000&Key-Pair-Id=K36FIAB9LE2OLR&Signature=PyL-YtiHFWe3DHRPvPMNe7rHB5guG-BXZtoF7RGkVCp24JbhepTngDj5nb8chkXuU8zLBOpUGJlQTpxV3TvKNHelywjutMFdW8RdZ8qHf7DZnjTws7p4jwHl2xaG4mGSxmUSXQtlxriFo01zIdzAKrg5Rp~Tp02eQkNIQXjL8cNOXnrq8~MUy7NxrPJvofkILLBtIhMNLTeC-qOKB0iUYG~YqYrHV6PrcxQxVLkwskRUmoTy67-JhRQOckb1ryvi6WMGQq9rhcyaCfmTNIM0ox8NQjFdYOIJUBQxQgnF6SvmocSu70FpVnv4NhNPzwL~SjnDnOQNzRpMEqjBbhnaCQ__"
			};
			*/
		}
		
		[ClientCallable("adventure/start")]
		public async Task<string> StartAdventure(string history, string prompt)
		{
			var decodedHistory = Base64Decode(history);
			var decodedPrompt = Base64Decode(prompt);
			
			var db = await Storage.GameDatabaseDatabase();
			var defaultCampaignName = "DefaultCampaign";
			
			var characters = await CampaignCharacterCollection.GetCharacters(db, defaultCampaignName, Context.UserId.ToString());
			if (!characters.Any())
			{
				throw new MicroserviceException(404, "CharacterNotFound",
					"A character has not been created yet for this campaign.");
			}

			var character = characters.First().ToCharacterView();
			
			// This code executes on the server.
			var claude = Provider.GetService<Claude>();
			var response = await claude.StartAdventure(character, decodedHistory, decodedPrompt);

			return response.Completion;
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
			var playerId = Context.UserId;
			// This code executes on the server.
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
			
			await Services.Notifications.NotifyPlayer(playerId, "blockade.reply", inferenceRsp.FileUrl);
			Debug.Log($"Skybox creation complete: {inferenceRsp.FileUrl}");
			return inferenceRsp.FileUrl;
		}
	}
}
