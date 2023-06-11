using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Anthropic;
using Beamable.Server;
using BlockadeLabs;
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
		public async Task<string> NewCharacter(string claudeContext)
		{
			var claude = Provider.GetService<Claude>();
			var decodedContext = "";
			if (!string.IsNullOrEmpty(claudeContext))
			{
				decodedContext = Base64Decode(claudeContext);
			}
			var rsp = await claude.NewCharacter(decodedContext);

			return rsp.Completion;
		}
		
		[ClientCallable("character/get")]
		public CharacterView GetCharacter()
		{
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
				skyboxUrl = "https://blockade-platform-production.s3.amazonaws.com/images/imagine/high_quality_detailed_digital_painting_cd_vr_computer_render_fantasy__6607fe49fc7369ec__5716463_.jpg?ver=1",
				imageUrl = "https://cdn.cloud.scenario.com/assets/CMmu-yJxQnCBX48MCOv2XA?p=100&Expires=1687392000&Key-Pair-Id=K36FIAB9LE2OLR&Signature=PyL-YtiHFWe3DHRPvPMNe7rHB5guG-BXZtoF7RGkVCp24JbhepTngDj5nb8chkXuU8zLBOpUGJlQTpxV3TvKNHelywjutMFdW8RdZ8qHf7DZnjTws7p4jwHl2xaG4mGSxmUSXQtlxriFo01zIdzAKrg5Rp~Tp02eQkNIQXjL8cNOXnrq8~MUy7NxrPJvofkILLBtIhMNLTeC-qOKB0iUYG~YqYrHV6PrcxQxVLkwskRUmoTy67-JhRQOckb1ryvi6WMGQq9rhcyaCfmTNIM0ox8NQjFdYOIJUBQxQgnF6SvmocSu70FpVnv4NhNPzwL~SjnDnOQNzRpMEqjBbhnaCQ__"
			};
		}
		
		[ClientCallable("adventure/start")]
		public async Task<string> StartAdventure(string history, string prompt)
		{
			var decodedHistory = Base64Decode(history);
			var decodedPrompt = Base64Decode(prompt);
			
			var character = new CharacterView
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
				skyboxUrl = "https://blockade-platform-production.s3.amazonaws.com/images/imagine/high_quality_detailed_digital_painting_cd_vr_computer_render_fantasy__6607fe49fc7369ec__5716463_.jpg?ver=1",
				imageUrl = "https://cdn.cloud.scenario.com/assets/CMmu-yJxQnCBX48MCOv2XA?p=100&Expires=1687392000&Key-Pair-Id=K36FIAB9LE2OLR&Signature=PyL-YtiHFWe3DHRPvPMNe7rHB5guG-BXZtoF7RGkVCp24JbhepTngDj5nb8chkXuU8zLBOpUGJlQTpxV3TvKNHelywjutMFdW8RdZ8qHf7DZnjTws7p4jwHl2xaG4mGSxmUSXQtlxriFo01zIdzAKrg5Rp~Tp02eQkNIQXjL8cNOXnrq8~MUy7NxrPJvofkILLBtIhMNLTeC-qOKB0iUYG~YqYrHV6PrcxQxVLkwskRUmoTy67-JhRQOckb1ryvi6WMGQq9rhcyaCfmTNIM0ox8NQjFdYOIJUBQxQgnF6SvmocSu70FpVnv4NhNPzwL~SjnDnOQNzRpMEqjBbhnaCQ__"
			};
			
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
