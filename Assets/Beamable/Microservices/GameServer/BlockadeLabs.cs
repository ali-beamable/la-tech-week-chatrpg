using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Beamable.Server;
using Beamable.Server.Api.Notifications;
using Unity.Plastic.Newtonsoft.Json;

namespace BlockadeLabs
{
    public class SkyboxApi
    {
        private readonly string _url = "https://backend.blockadelabs.com/api/v1";

        private readonly RequestContext _ctx;
        private readonly HttpClient _httpClient;
        private readonly IMicroserviceNotificationsApi _notifications;
        private readonly Config _config;
        
        private readonly JsonSerializerSettings _newtonSoftJsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        
        public SkyboxApi(RequestContext ctx, HttpClient httpClient, IMicroserviceNotificationsApi notifications, Config config)
        {
            _ctx = ctx;
            _httpClient = httpClient;
            _notifications = notifications;
            _config = config;
        }

        public async Task<CreateSkyboxResponse> CreateSkybox(SkyboxStyles skyboxStyle, string prompt)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_url}/skybox"))
            {
                var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"prompt", prompt},     
                    {"skybox_style_id", ((int)skyboxStyle).ToString() }
                });
                
                requestMessage.Headers.Add("x-api-key", _config.BlockadeLabsApiKey);
                requestMessage.Content = requestBody;
                var response = await _httpClient.SendAsync(requestMessage);
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseDeserialized = JsonConvert.DeserializeObject<CreateSkyboxResponse>(responseBody, _newtonSoftJsonSettings);

                return responseDeserialized;
            }
        }

        public async Task<GetSkyboxStatusResponse> GetSkyboxStatus(long requestId)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_url}/imagine/requests/{requestId}"))
            {
                requestMessage.Headers.Add("x-api-key", _config.BlockadeLabsApiKey);
                var response = await _httpClient.SendAsync(requestMessage);
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseDeserialized = JsonConvert.DeserializeObject<GetSkyboxStatusResponse>(responseBody, _newtonSoftJsonSettings);

                return responseDeserialized;
            }
        }
        
        public async Task<GetSkyboxStatusResponse> PollSkyboxToCompletion(long requestId)
        {
            int pollCount = 0;
            bool completed = false;
            GetSkyboxStatusResponse updatedInference = null;
            while (!completed)
            {
                // Wait 1 second before polling again
                await Task.Delay(1000);

                updatedInference = await GetSkyboxStatus(requestId);
                completed = updatedInference.Request.IsCompleted;
                pollCount += 1;

                if (pollCount >= 300 && !completed)
                {
                    throw new Exception("Polling timed out after 10 attempts.");
                }
            }

            return updatedInference;
        }
    }

    public class CreateSkyboxResponse
    {
        [JsonProperty("id")]
        [JsonRequired]
        public long? Id { get; set; }
        
        [JsonProperty("status")]
        [JsonRequired]
        public string Status { get; set; }
        
        [JsonProperty("file_url")]
        public string FileUrl { get; set; }
        
        [JsonProperty("thumb_url")]
        public string ThumbUrl { get; set; }
        
        [JsonProperty("depth_map_url")]
        public string DepthMapUrl { get; set; }
        
        [JsonProperty("pusher_channel")]
        public string PusherChannel { get; set; }
        
        [JsonProperty("pusher_event")]
        public string PusherEvent { get; set; }

        public bool IsCompleted => Status == "complete";
        public bool InProgress => Status == "pending" || Status == "processing";
    }

    public class GetSkyboxStatusResponse
    {
        [JsonProperty("request")]
        [JsonRequired]
        public CreateSkyboxResponse Request { get; set; }
    }

    public enum SkyboxStyles
    {
        DIGITAL_PAINTING = 5,
        FANTASY_LAND = 2
    }
}