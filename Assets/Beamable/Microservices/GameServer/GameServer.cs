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

			Debug.Log($"Skybox creation complete: {inferenceRsp.FileUrl}");
			return inferenceRsp.FileUrl;
		}
	}
}
