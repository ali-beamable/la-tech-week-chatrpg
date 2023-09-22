using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;

namespace Scenario
{
    public class ScenarioApi
    {
        private readonly string _url = "https://api.cloud.scenario.com/v1";
        private readonly HttpClient _client;
        private readonly Config _config;

        public ScenarioApi(HttpClient client, Config config)
        {
            _client = client;
            _config = config;
        }

        public async Task<InferenceResponse> CreateInference(string model, string prompt)
        {
            var requestBody = new CreateInferenceRequest
            {
                parameters = new InferenceParameters
                {
                    prompt = prompt
                }
            };
            var requestPayload = JsonConvert.SerializeObject(requestBody);
            var req = new HttpRequestMessage(HttpMethod.Post, $"{_url}/models/{model}/inferences");
            req.Content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
            req.Headers.Add("Authorization", $"Basic {_config.ScenarioApiKey}");

            var response = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            var responseJson = await response.Content.ReadAsStringAsync();
            var completionResponse = JsonConvert.DeserializeObject<InferenceResponse>(responseJson);
            
            return completionResponse;
        }

        public async Task<InferenceResponse> GetInference(string model, string inferenceId)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{_url}/models/{model}/inferences/{inferenceId}");
            req.Headers.Add("Authorization", $"Basic {_config.ScenarioApiKey}");
            
            var response = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            var responseJson = await response.Content.ReadAsStringAsync();
            var completionResponse = JsonConvert.DeserializeObject<InferenceResponse>(responseJson);
            return completionResponse;
        }

        public async Task<InferenceResponse> PollInferenceToCompletion(string model, string inferenceId)
        {
            int pollCount = 0;
            bool completed = false;
            InferenceResponse updatedInference = null;
            while (!completed)
            {
                // Wait 1 second before polling again
                await Task.Delay(1000);

                updatedInference = await GetInference(model, inferenceId);
                completed = updatedInference.inference.IsCompleted;
                pollCount += 1;

                if (pollCount >= 300 && !completed)
                {
                    throw new Exception("Polling timed out after 10 attempts.");
                }
            }

            return updatedInference;
        }
    }

    public class CreateInferenceRequest
    {
        public InferenceParameters parameters;
    }

    public class InferenceParameters
    {
        public string prompt;
        public string type = "txt2img";
        public double guidance = 7.0;
        public int width = 512;
        public int height = 512;
        public int numInferenceSteps = 30;
        public int numSamples = 1;
        public bool enableSafetyCheck = false;
    }

    public class InferenceResponse
    {
        public Inference inference;
    }

    public class Inference
    {
        public string id;
        public string userId;
        public string authorId;
        public string modelId;
        public string createdAt;
        public InferenceParameters parameters;
        public string status;
        public InferenceImage[] images;
        public int imagesNumber;
        public string displayPrompt;
        public double progress;

        public bool IsCompleted => status == "succeeded";
        public bool InProgress => status == "in-progress";
    }

    public class InferenceImage
    {
        public string id;
        public string url;
    }
}

