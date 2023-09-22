using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Scheduler;
using Beamable.Server;
using Beamable.StorageObjects.GameDatabase;
using Anthropic;
using BlockadeLabs;
using OpenAI;
using Scenario;
using Unity.Plastic.Newtonsoft.Json;

namespace Beamable.Microservices
{
	[Microservice("GameServer")]
	public class GameServer : Microservice
	{
		private bool _enableVectorSearch = true;
		
		[ConfigureServices]
		public static void Configure(IServiceBuilder builder)
		{
			// Services
			builder.Builder.AddSingleton(p => new HttpClient());
			builder.Builder.AddSingleton<Config>();
			builder.Builder.AddSingleton<PromptService>();
			builder.Builder.AddSingleton<ScenarioApi>();
			builder.Builder.AddSingleton<SkyboxApi>();
			builder.Builder.AddSingleton<ClaudeApi>();
			builder.Builder.AddSingleton<OpenAI_API>();
			builder.Builder.AddSingleton<PortraitService>();
			builder.Builder.AddSingleton<SkyboxService>();
			builder.Builder.AddSingleton<CampaignService>();
			builder.Builder.AddSingleton<CharacterService>();

			// Collections
			builder.Builder.AddSingleton<CampaignCharacterCollection>();
			builder.Builder.AddSingleton<CampaignEventsCollection>();
			builder.Builder.AddSingleton<BlockadeSkyboxesCollection>();
			builder.Builder.AddSingleton<ScenarioPortraitsCollection>();
			
			// Only necessary if using custom atlas database
			builder.Builder.ReplaceSingleton<IStorageObjectConnectionProvider, AtlasStorageConnectionProvider>();
		}
		
		[InitializeServices]
		public static async Task Init(IServiceInitializer init)
		{
			var config = init.GetService<Config>();
			await config.Init();
			
			// Create Indexes
			await init.GetService<CampaignCharacterCollection>().EnsureIndexes();
			await init.GetService<CampaignEventsCollection>().EnsureIndexes();
			await init.GetService<BlockadeSkyboxesCollection>().EnsureIndexes();
			await init.GetService<ScenarioPortraitsCollection>().EnsureIndexes();
		}

		[ClientCallable("character/new")]
		public async Task<CharacterView> NewCharacter(string card1, string card2, string card3)
		{
			var characterService = Provider.GetService<CharacterService>();
			CampaignCharacter newCharacter = await characterService.GenerateCharacterSheet(card1, card2, card3);

			var characterJson = JsonConvert.SerializeObject(newCharacter.ToCharacterView());
			await Services.Notifications.NotifyPlayer(Context.UserId, "character.preview", characterJson);

			// Generate or Vector Search for a portrait
			var portraitService = Provider.GetService<PortraitService>();
			var portraitPrompt = $"Portrait,D&D,{newCharacter.Gender},{newCharacter.Class},{newCharacter.Race},{newCharacter.Description.Replace(",", " ")}";
			var portraitUrl = await portraitService.GetPortraitUrl(portraitPrompt, _enableVectorSearch);

			//TODO: Fetch a skybox based on the starting location of the campaign
			//Either generate one, or do a vector search
			var defaultSkyboxUrl =
				"https://blockade-platform-production.s3.amazonaws.com/images/imagine/Fantasy_equirectangular-jpg_The_tavern_interior_is_1334286254_8825770.jpg?ver=1";

			// Further Enrich Character Data
			newCharacter.CampaignName = "DefaultCampaign";
			newCharacter.PlayerId = Context.UserId.ToString();
			newCharacter.SkyboxUrl = defaultSkyboxUrl;
			newCharacter.PortraitUrl = portraitUrl;

			await characterService.SaveCharacter(newCharacter);
			await Services.Notifications.NotifyPlayer(Context.UserId, "character.created", "");
			
			return newCharacter.ToCharacterView();
		}

		[ClientCallable("character/get")]
		public async Task<CharacterView> GetCharacter()
		{
			var characterService = Provider.GetService<CharacterService>();
			var defaultCampaignName = "DefaultCampaign";

			var character = await characterService.GetCharacter(defaultCampaignName, Context.UserId);
			return character.ToCharacterView();
		}

		[ClientCallable("adventure/ready")]
		public async Task<WorldState> Ready()
		{
			var characterService = Provider.GetService<CharacterService>();
			var campaignService = Provider.GetService<CampaignService>();

            var campaignName = "DefaultCampaign";
            var character = await characterService.GetCharacter(campaignName, Context.UserId);
            
            var worldState = await campaignService.GetWorldState(campaignName, character);
            var worldJson = JsonConvert.SerializeObject(worldState);
			await Services.Notifications.NotifyPlayer(Context.UserId, "world.update", worldJson);

			return worldState;
        }

        [ClientCallable("adventure/play")]
        public async Task<WorldState> Play(AdventurePlayRequest request)
        {
	        var characterService = Provider.GetService<CharacterService>();
	        var campaignService = Provider.GetService<CampaignService>();
 
            var campaignName = "DefaultCampaign";
			var character = await characterService.GetCharacter(campaignName, Context.UserId);
			var campaignEvent = await campaignService.GenerateCampaignEvent(campaignName, character, request.playerAction);
            await campaignService.SaveCampaignEvent(campaignEvent);
            
            var worldState = campaignEvent.ToWorldState();
            var worldJson = JsonConvert.SerializeObject(worldState);
            await Services.Notifications.NotifyPlayer(Context.UserId, "world.update", worldJson);

			// Update Skybox
			var skyboxService = Provider.GetService<SkyboxService>();
			var skyboxUrl = await skyboxService.GetSkyboxUrl(campaignEvent.Description, _enableVectorSearch);
			campaignEvent.SkyboxUrl = skyboxUrl;
			await campaignService.SaveCampaignEvent(campaignEvent);
			
			worldState = campaignEvent.ToWorldState();
			worldJson = JsonConvert.SerializeObject(worldState);
			await Services.Notifications.NotifyPlayer(Context.UserId, "world.update", worldJson);
			
			return worldState;
        }
		
		[ClientCallable("claude")]
		public async Task<string> TestClaude(string prompt)
		{
			// This code executes on the server.
			var claude = Provider.GetService<ClaudeApi>();
			var response = await claude.Send(new ClaudeCompletionRequest
			{
				Prompt = $"\n\nHuman: {prompt}\n\nAssistant:",
				Model = ClaudeModels.ClaudeInstantLatestV1,
				MaxTokensToSample = 100000
			});

			return response.Completion;
		}

		[ClientCallable("scenario")]
		public async Task<string> TestScenario(string prompt)
		{
			// This code executes on the server.
			var model = "RNHPqrHFQYu-oqZGb5VBtg";
			var scenario = Provider.GetService<ScenarioApi>();

			var inferenceRsp = await scenario.CreateInference(model, "dungeons & dragons, level 1, wizard");
			if (!inferenceRsp.inference.IsCompleted)
			{
				BeamableLogger.Log("Polling for inference...");
				inferenceRsp =
					await scenario.PollInferenceToCompletion(inferenceRsp.inference.modelId, inferenceRsp.inference.id);
			}

			var url = inferenceRsp.inference.images.FirstOrDefault().url;
			BeamableLogger.Log(url);

			return url;
		}

		[ClientCallable("blockade")]
		public async Task<string> TestBlockade(string prompt)
		{
			var skyboxStyle = SkyboxStyles.FANTASY_LAND;
			var blockade = Provider.GetService<SkyboxApi>();
			
			BeamableLogger.Log("Creating Skybox...");
			var inferenceRsp = await blockade.CreateSkybox(skyboxStyle, prompt);
			if (!inferenceRsp.IsCompleted)
			{
				BeamableLogger.Log("Polling skybox for completion...");
				var rsp = await blockade.PollSkyboxToCompletion(inferenceRsp.Id.Value);
				inferenceRsp = rsp.Request;
			}
			
			BeamableLogger.Log($"Skybox creation complete: {inferenceRsp.FileUrl}");
			return inferenceRsp.FileUrl;
		}

		[ClientCallable]
		public async Task<Job> TestScheduler()
		{
			var job = await Services.Scheduler.Schedule()
				.Microservice<GameServer>(useLocal: true)
				.Run(t => t.DelayedTask)
				.After(TimeSpan.FromSeconds(10))
				.Save("test");
				
			return job;
		}

		[Callable]
		public async Task DelayedTask()
		{
			BeamableLogger.Log("Delayed Task Executing...");
			await Task.Delay(30000);
			BeamableLogger.Log("Delayed Task Completed.");
		}
	}
}
