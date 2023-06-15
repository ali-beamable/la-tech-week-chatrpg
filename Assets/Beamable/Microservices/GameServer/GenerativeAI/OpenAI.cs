using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;

namespace OpenAI
{
    public class OpenAI_API
    {
        private readonly string _url = "https://api.openai.com/v1";

        private readonly HttpClient _httpClient;
        private readonly Config _config;
        
        public OpenAI_API(Config config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<float[]> GetEmbeddings(string input, string model = "text-embedding-ada-002")
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_url}/embeddings"))
            {
                var json = JsonConvert.SerializeObject(new OpenAIEmbeddingsRequest
                {
                    Input = input, 
                    Model = model
                });
                var requestBody = new StringContent(json, Encoding.UTF8, "application/json");
                requestMessage.Headers.Add("Authorization", $"Bearer {_config.OpenAIKey}");
                requestMessage.Content = requestBody;
                var response = await _httpClient.SendAsync(requestMessage);
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseDeserialized = JsonConvert.DeserializeObject<OpenAIEmbeddingsResponse>(responseBody);

                return responseDeserialized.Data.FirstOrDefault().Embedding;
            }
        }
    }
    
    public class OpenAIEmbeddingsRequest
    {
        [JsonProperty("input")]
        public string Input { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }
    }
    
    public class OpenAIEmbeddingsResponse
    {
        [JsonProperty("data")]
        public OpenAIEmbeddingObject[] Data { get; set; }
    }

    public class OpenAIEmbeddingObject
    {
        [JsonProperty("embedding")]
        public float[] Embedding { get; set; }
    }
}

