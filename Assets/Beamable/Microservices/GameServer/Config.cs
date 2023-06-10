using Beamable.Common;
using Beamable.Server.Api.RealmConfig;

public class Config
{
    private readonly IMicroserviceRealmConfigService _realmConfigService;
    private RealmConfig _settings;

    public string AnthropicApiKey => _settings.GetSetting("game", "anthropic_key");
    public string ScenarioApiKey => _settings.GetSetting("game", "scenario_key");

    public Config(IMicroserviceRealmConfigService realmConfigService)
    {
        _realmConfigService = realmConfigService;
    }

    public async Promise Init()
    {
        _settings = await _realmConfigService.GetRealmConfigSettings();
    }
    
    
}