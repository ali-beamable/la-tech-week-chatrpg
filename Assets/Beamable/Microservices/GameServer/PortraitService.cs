using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server;
using Beamable.StorageObjects.GameDatabase;
using OpenAI;
using Scenario;

namespace Beamable.Microservices
{
    public class PortraitService
    {
        private OpenAI_API _openAI;
        private ScenarioApi _scenarioApi;
        private string _scenarioModel;
        private ScenarioPortraitsCollection _portraitsCollection;
        
        public PortraitService(OpenAI_API openAI, ScenarioApi scenarioApi, ScenarioPortraitsCollection portraitsCollection)
        {
            _openAI = openAI;
            _scenarioApi = scenarioApi;
            _portraitsCollection = portraitsCollection;
            _scenarioModel = "EnoXc8q4QlqAfCfoObmW3Q"; // Public Scenario Generator
        }

        public async Task<string> GetPortraitUrl(string prompt, bool useVectorSearch)
        {
            string portraitUrl;
            
            // Generate vector embedding for current or future vector search (in case of inference)
            var scenarioEmbeddings = await _openAI.GetEmbeddings(prompt);

            // Perform a Vector Search to see if we can find a good portrait match
            List<ScenarioAsset> scenarioSearchResults = new List<ScenarioAsset>();
            if (useVectorSearch)
                scenarioSearchResults = await _portraitsCollection.VectorSearch(scenarioEmbeddings, 0.96);

            if (scenarioSearchResults.Count > 0)
            {
                BeamableLogger.Log("Found Scenario asset with sufficient score.");
                portraitUrl = scenarioSearchResults.OrderByDescending(skybox => skybox.Score).First().FileUrl;
            }
            else
            {
                // Generate Scenario Portrait
                var scenarioRsp = await _scenarioApi.CreateInference(_scenarioModel, prompt);
                var completedInference = await _scenarioApi.PollInferenceToCompletion(scenarioRsp.inference.modelId, scenarioRsp.inference.id);
                var scenarioAsset = new ScenarioAsset
                {
                    Prompt = prompt,
                    Model = completedInference.inference.modelId,
                    FileUrl = completedInference.inference.images.Select(i => i.url).FirstOrDefault(),
                    Content = prompt,
                    Embedding = scenarioEmbeddings
                };
                var insertedScenario = await _portraitsCollection.Insert(scenarioAsset);
                if (!insertedScenario)
                {
                    throw new MicroserviceException(500, "UnableToSavePortrait",
                        "Failed to save character portrait inference data to database.");
                }

                portraitUrl = scenarioAsset.FileUrl;
            }

            return portraitUrl;
        }
    }
}