using System.Net.Http;
using System.Threading.Tasks;
using Anthropic;
using Beamable.Server;

namespace Beamable.Microservices
{
	[Microservice("GameServer")]
	public class GameServer : Microservice
	{
		[ConfigureServices]
		public static void Configure(IServiceBuilder builder)
		{
			builder.Builder.AddSingleton<Config>();
			builder.Builder.AddScoped<Claude>();
			builder.Builder.AddSingleton<Scenario>();
			builder.Builder.AddSingleton(p => new HttpClient());
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
		public void TestScenario(string prompt)
		{
			// This code executes on the server.
		}

		[ClientCallable("blockade")]
		public void TestBlockade(string prompt)
		{
			// This code executes on the server.
		}
	}
}
