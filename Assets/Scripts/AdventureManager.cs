using Beamable;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Clients;
using BlockadeLabsSDK;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AdventureManager : MonoBehaviour
{
    public CharacterWidget characterWidget;
    public GameObject chatMessagePrefab;
    public GameObject narrativeMessagePrefab;
    public GameObject chatScrollView;
    public GameObject storyScrollView;
    public InputField chatInputField;
    public TMPro.TMP_Text roomText;

    public BlockadeLabsSkybox blockadeSkybox;

    [SerializeField] private CharacterView _characterView;
    [SerializeField] private WorldState _worldState;

    public async void OnChatSubmit()
    {
        var chatInput = chatInputField.text;
        chatInputField.text = "";

        InsertChatMessage($"[{_characterView.characterName}]:", chatInput);

        await BeamContext.Default.Microservices().GameServer()
            .Play(new AdventurePlayRequest { playerAction = chatInput });
    }

    async void Start()
    {
        RemoveAllChildren(chatScrollView);
        RemoveAllChildren(storyScrollView);

        var beamContext = BeamContext.Default;
        await beamContext.OnReady;

        beamContext.Api.NotificationService.Subscribe("world.update", OnWorldUpdate);
        beamContext.Api.NotificationService.Subscribe("character.update", OnCharacterUpdate);

        CharacterView character = await BeamContext.Default.Microservices().GameServer().GetCharacter();
        if (CharacterCreation.SelectedCharacter == null)
        {
            character = await BeamContext.Default.Microservices().GameServer().GetCharacter();
        }
        else
        {
            character = CharacterCreation.SelectedCharacter;
        }
        RefreshCharacter(character);

        //TODO: Make this a loading screen ideally
        await beamContext.Microservices().GameServer().Ready();
    }

    void OnWorldUpdate(object message)
    {
        var parsedArrayDict = message as ArrayDict;
        var payload = parsedArrayDict["stringValue"] as string;
        var worldState = JsonConvert.DeserializeObject<WorldState>(payload);
        RefreshWorldState(worldState);
    }

    //Update Room Name, Characters, Music, Skybox, etc.
    void RefreshWorldState(WorldState worldState)
    {
        if (worldState != _worldState)
        {
            if(_worldState.skyboxUrl != worldState.skyboxUrl && !string.IsNullOrEmpty(worldState.skyboxUrl))
            {
                // Maybe should be Coroutine? Maybe await all the way down?
                LoadSkyboxFromUrl(worldState.skyboxUrl);
            }

            if(!string.IsNullOrEmpty(worldState.story) && worldState.story != _worldState.story)
            {
                InsertStoryMessage(worldState.story);
            }

            if (!string.IsNullOrEmpty(worldState.dm) && worldState.dm != _worldState.dm)
            {
                InsertChatMessage("[DM]:", worldState.dm);
            }

            if(!string.IsNullOrEmpty(worldState.roomName))
            {
                roomText.text = worldState.roomName;
            }

            //TODO: Simplify, ideally should inject a World State service of some kind that is observable
            _worldState = worldState;
        }
    }

    void OnCharacterUpdate(object message)
    {
        var parsedArrayDict = message as ArrayDict;
        var payload = parsedArrayDict["stringValue"] as string;
        var character = JsonConvert.DeserializeObject<CharacterView>(payload);
        RefreshCharacter(character);
    }

    void RefreshCharacter(CharacterView character)
    {
        if (character != _characterView)
        {
            //TODO: Simplify, ideally should inject a Character service of some kind that is observable
            _characterView = character;
            characterWidget.Character = character;
        }
    }

    void InsertChatMessage(string author, string body)
    {
        var newMessage = Instantiate(chatMessagePrefab, chatScrollView.transform);
        var chatMessage = newMessage.GetComponent<ChatMessage>();
        chatMessage.messageAuthor.text = author;
        chatMessage.messageBody.text = body;

        StartCoroutine(RefreshScrollView(chatScrollView));
    }

    void InsertStoryMessage(string body)
    {
        var newMessage = Instantiate(narrativeMessagePrefab, storyScrollView.transform);
        var storyMessage = newMessage.GetComponent<ChatMessage>();
        storyMessage.messageBody.text = body;

        StartCoroutine(RefreshScrollView(storyScrollView));
    }

    async void LoadSkyboxFromUrl(string skyboxUrl)
    {
        if (!string.IsNullOrWhiteSpace(skyboxUrl))
        {
            var image = await ApiRequests.GetImagineImage(skyboxUrl);
            var texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            texture.LoadImage(image);

            var r = blockadeSkybox.GetComponent<Renderer>();
            if (r != null)
            {
                if (r.sharedMaterial != null)
                {
                    r.sharedMaterial.mainTexture = texture;
                }
            }
        }
    }

    void RemoveAllChildren(GameObject parent)
    {
        int childCount = parent.transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    IEnumerator RefreshScrollView(GameObject scrollView)
    {
        // Wait a frame
        yield return 0;

        var rectTransforms = scrollView.GetComponentsInChildren<RectTransform>();
        foreach (var rectTransform in rectTransforms)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        Canvas.ForceUpdateCanvases();
    }
}

public class ClaudeContext
{
    public string story;
    public string roomName;
    public string[] characters;
    public string description;
    public string music;
}