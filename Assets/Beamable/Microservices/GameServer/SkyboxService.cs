using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server;
using Beamable.StorageObjects.GameDatabase;
using BlockadeLabs;
using OpenAI;

namespace Beamable.Microservices
{
    public class SkyboxService
    {
        private OpenAI_API _openAI;
        private SkyboxApi _skyboxApi;
        private SkyboxStyles _skyboxStyle;
        private BlockadeSkyboxesCollection _skyboxCollection;

        public SkyboxService(OpenAI_API openAI, SkyboxApi skyboxApi, BlockadeSkyboxesCollection skyboxCollection)
        {
            _openAI = openAI;
            _skyboxApi = skyboxApi;
            _skyboxCollection = skyboxCollection;
            _skyboxStyle = SkyboxStyles.FANTASY_LAND;
        }

        public async Task<string> GetSkyboxUrl(string prompt, bool useVectorSearch)
        {
            string skyboxUrl;

            // Generate vector embedding for current or future vector search (in case of inference)
            var skyboxEmbeddingsVector = await _openAI.GetEmbeddings(prompt);

            // Perform a Vector Search to see if we can find a good skybox match
            var skyboxSearchResults = new List<BlockadeSkybox>();
            if(useVectorSearch)
                skyboxSearchResults = await _skyboxCollection.VectorSearch(skyboxEmbeddingsVector, 0.96);
            
            if (skyboxSearchResults.Count > 0)
            {
                BeamableLogger.Log("Found Skybox with sufficient score.");
                skyboxUrl = skyboxSearchResults.OrderByDescending(skybox => skybox.Score).First().FileUrl;
            }
            else
            {
                BeamableLogger.Log("Creating Skybox...");
                var inferenceRsp = await _skyboxApi.CreateSkybox(_skyboxStyle, prompt);
                if (!inferenceRsp.IsCompleted)
                {
                    BeamableLogger.Log("Polling skybox for completion...");
                    var rsp = await _skyboxApi.PollSkyboxToCompletion(inferenceRsp.Id.Value);
                    inferenceRsp = rsp.Request;
                }
				
                var insertedSkybox = await _skyboxCollection.Insert(new BlockadeSkybox
                {
                    StyleId = (int)_skyboxStyle,
                    StyleName = _skyboxStyle.ToString(),
                    Prompt = prompt,
                    FileUrl = inferenceRsp.FileUrl,
                    DepthMapUrl = inferenceRsp.DepthMapUrl,
                    ThumbUrl = inferenceRsp.ThumbUrl,
                    Embedding = skyboxEmbeddingsVector
                });

                if (!insertedSkybox)
                    throw new MicroserviceException(500, "FailedToSaveSkybox", "Failed to save skybox to the database.");

                skyboxUrl = inferenceRsp.FileUrl;
            }

            return skyboxUrl;
        }
    }
}