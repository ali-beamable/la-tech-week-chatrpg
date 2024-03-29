﻿using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;

namespace Anthropic
{
	public class ClaudeApi
	{
        private readonly string _url = "https://api.anthropic.com/v1/complete";

        private readonly HttpClient _httpClient;
        private readonly Config _config;

        private readonly JsonSerializerSettings _newtonSoftJsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public ClaudeApi(HttpClient httpClient, Config config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<ClaudeCompletionResponse> Send(ClaudeCompletionRequest requestPayload)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _url))
            {
                var json = JsonConvert.SerializeObject(requestPayload, _newtonSoftJsonSettings);
                var requestBody = new StringContent(json, Encoding.UTF8, "application/json");

                requestMessage.Headers.Add("X-API-Key", _config.AnthropicApiKey);
                requestMessage.Content = requestBody;
                var response = await _httpClient.SendAsync(requestMessage);
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseDeserialized = JsonConvert.DeserializeObject<ClaudeCompletionResponse>(responseBody, _newtonSoftJsonSettings);

                return responseDeserialized;
            }
        }
    }

    public class ClaudeCompletionRequest
    {
        #region required
        
        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("max_tokens_to_sample")]
        public int? MaxTokensToSample { get; set; }

        #endregion

        #region optional

        [JsonProperty("stop_sequences")]
        public string[] StopSequences { get; set; }

        [JsonProperty("stream")]
        public bool? Stream { get; set; }

        [JsonProperty("temperature")]
        public float? Temperature { get; set; }

        [JsonProperty("top_k")]
        public int? TopK { get; set; }

        [JsonProperty("top_p")]
        public float? TopP { get; set; }
        //public object? metadata;

        #endregion
    }

    public class ClaudeCompletionResponse
    {
        [JsonProperty("completion")]
        [JsonRequired]
        public string Completion { get; set; }

        [JsonProperty("stop_reason")]
        public string StopReason { get; set; }
    }

    public static class ClaudeModels
    {
        // Claude: superior performance on tasks that require complex reasoning
        public const string ClaudeLatestV2 = "claude-2";
        
        // Claude Instant: low-latency, high throughput
        public const string ClaudeInstantLatestV1 = "claude-instant-1";

        // Our largest model, ideal for a wide range of more complex tasks.
        public static readonly string ClaudeV1 = "claude-v1";

        // An enhanced version of claude-v1 with a 100,000 token (roughly 75,000 word) context window.
        // Ideal for summarizing, analyzing, and querying long documents and conversations for nuanced understanding of complex topics and relationships across very long spans of text.
        public static readonly string ClaudeV1_100k = "claude-v1-100k";

        // A smaller model with far lower latency, sampling at roughly 40 words/sec!
        // Its output quality is somewhat lower than the latest claude-v1 model, particularly for complex tasks.
        // However, it is much less expensive and blazing fast.
        // We believe that this model provides more than adequate performance on a range of tasks including text classification, summarization, and lightweight chat applications, as well as search result summarization.
        public static readonly string ClaudeInstantV1 = "claude-instant-v1";

        // An enhanced version of claude-instant-v1 with a 100,000 token context window that retains its performance.
        // Well-suited for high throughput use cases needing both speed and additional context, allowing deeper understanding from extended conversations and documents.
        public static readonly string ClaudeInstantV1_100k = "claude-instant-v1-100k";

        // Our latest version of claude-instant-v1.
        // It is better than claude-instant-v1.0 at a wide variety of tasks including writing, coding, and instruction following.
        // It performs better on academic benchmarks, including math, reading comprehension, and coding tests.
        // It is also more robust against red-teaming inputs.
        public static readonly string ClaudeInstantV1_1 = "claude-instant-v1.1";

        // An enhanced version of claude-instant-v1.1 with a 100,000 token context window that retains its lightning fast 40 word/sec performance.
        public static readonly string ClaudeInstantV1_1_100k = "claude-instant-v1.1-100k";

        // An improved version of claude-v1. It is slightly improved at general helpfulness, instruction following, coding, and other tasks.
        // It is also considerably better with non-English languages.
        // This model also has the ability to role play (in harmless ways) more consistently, and it defaults to writing somewhat longer and more thorough responses.
        public static readonly string ClaudeV1_2 = "claude-v1.2";

        // Compared to claude-v1.2, it's more robust against red-team inputs, better at precise instruction-following, better at code, and better and non-English dialogue and writing.
        public static readonly string ClaudeV1_3 = "claude-v1.3";

        // An enhanced version of claude-v1.3 with a 100,000 token (roughly 75,000 word) context window.
        public static readonly string ClaudeV1_3_100k = "claude-v1.3-100k";
    }
}

