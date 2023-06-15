using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Beamable;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Clients;
using BlockadeLabsSDK;
using UnityEngine;
using UnityEngine.UI;

public class AdventureManager : MonoBehaviour
{
    public CharacterWidget characterWidget;
    public GameObject chatMessagePrefab;
    public GameObject narrativeMessagePrefab;
    public GameObject chatScrollView;
    public InputField chatInputField;
    
    public BlockadeLabsSkybox blockadeSkybox;
    private CharacterView _characterView;
    private string _roomName;

    private string claudeContext = "";
    
    // Start is called before the first frame update
    async void Start()
    {
        RemoveAllChildren(chatScrollView);
        
        var beamContext = BeamContext.Default;
        await beamContext.OnReady;
        
        beamContext.Api.NotificationService.Subscribe("claude.reply", OnClaudeReply);
        beamContext.Api.NotificationService.Subscribe("blockade.reply", OnBlockadeReply);
        beamContext.Api.NotificationService.Subscribe("character.update", OnCharacterUpdate);

        if (CharacterCreation.SelectedCharacter == null)
        {
            _characterView = await BeamContext.Default.Microservices().GameServer().GetCharacter();
        }
        else
        {
            _characterView = CharacterCreation.SelectedCharacter;
        }
        
        characterWidget.SetCharacter(_characterView);
        
        var preamble = PreamblePrompt();
        var skyboxLoad = LoadFromUrl(_characterView.skyboxUrl);
        var tasks = new List<Task>
        {
            preamble,
            skyboxLoad
        };

        await Task.WhenAll(tasks);
        //await UpdateSkybox("A dark and ominous forest on the edge of massive chasm. Shadows and gloom abound.");
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

    public async void OnChatSubmit()
    {
        var chatInput = chatInputField.text;
        chatInputField.text = "";
        
        var newMessage = Instantiate(chatMessagePrefab, chatScrollView.transform);
        var chatMessage = newMessage.GetComponent<ChatMessage>();
        chatMessage.messageAuthor.text = $"{_characterView.characterName}:";
        chatMessage.messageBody.text = chatInput;
        var oldContext = claudeContext;
        var prompt = $"{_characterView.characterName}: {chatInput}";
        claudeContext += $"\n{prompt}";

        RefreshScrollView();

        await BeamContext.Default.Microservices().GameServer()
            .StartAdventure(Base64Encode(oldContext), Base64Encode(prompt));
    }

    private void RefreshScrollView()
    {
        var rectTransforms = chatScrollView.GetComponentsInChildren<RectTransform>();
        foreach (var rectTransform in rectTransforms)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        Canvas.ForceUpdateCanvases();
    }

    private async Task PreamblePrompt()
    {
        await BeamContext.Default.Microservices().GameServer()
            .StartAdventure(Base64Encode(claudeContext), Base64Encode("Introduce the scenario to the player, including the search for the nemesis."));
    }

    private async Task UpdateSkybox(string prompt)
    {
        await BeamContext.Default.Microservices().GameServer().TestBlockade(prompt);
    }
    
    private void OnCharacterUpdate(object message)
    {
        
    }
    
    private void OnClaudeReply(object message)
    {
        Debug.Log("Claude Message Received!");

        var parsedArrayDict = message as ArrayDict;
        var response = parsedArrayDict["stringValue"] as string;
        claudeContext += $"You:\n{response}";;
        
        var newMessage = Instantiate(chatMessagePrefab, chatScrollView.transform);
        var chatMessage = newMessage.GetComponent<ChatMessage>();
        
        var parsedContext = ParseContextXML(response);
        chatMessage.messageAuthor.text = "[DM]:";
        chatMessage.messageBody.text = parsedContext.story;

        RefreshScrollView();
        
        if (!string.IsNullOrEmpty(parsedContext.roomName) && parsedContext.roomName != _roomName && !string.IsNullOrEmpty(parsedContext.description))
        {
            _roomName = parsedContext.roomName;
            var roomDescription = $"{parsedContext.roomName}. {parsedContext.description}".Replace("\n"," ").Trim();
            UpdateSkybox(roomDescription);
        }
    }

    private ClaudeContext ParseContextXML(string message)
    {
        XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
        xmlDoc.LoadXml($"<claude>{message}</claude>");

        var story = "";
        foreach (XmlNode node in xmlDoc.GetElementsByTagName("STORY"))
        {
            var nodeText = node.InnerText;
            if (!string.IsNullOrEmpty(nodeText))
            {
                story += $"{node.InnerText}\n";
            }
        }

        if (string.IsNullOrEmpty(story))
        {
            if (!string.IsNullOrEmpty(xmlDoc.InnerText))
            {
                story += xmlDoc.InnerText;
            }
            else
            {
                story = message;
            }
        }

        var description = "";
        foreach (XmlNode node in xmlDoc.GetElementsByTagName("DESCRIPTION"))
        {
            var nodeText = node.InnerText;
            if (!string.IsNullOrEmpty(nodeText))
            {
                description += $"{node.InnerText}\n";
            }
        }
        
        var roomName = "";
        foreach (XmlNode node in xmlDoc.GetElementsByTagName("ROOM_NAME"))
        {
            var nodeText = node.InnerText;
            if (!string.IsNullOrEmpty(nodeText))
            {
                roomName = $"{node.InnerText}\n";//Grab only latest
            }
        }
        
        var music = "";
        foreach (XmlNode node in xmlDoc.GetElementsByTagName("MUSIC"))
        {
            var nodeText = node.InnerText;
            if (!string.IsNullOrEmpty(nodeText))
            {
                music += $"{node.InnerText}\n";
            }
        }
        
        var characters = "";
        foreach (XmlNode node in xmlDoc.GetElementsByTagName("CHARACTERS"))
        {
            var nodeText = node.InnerText;
            if (!string.IsNullOrEmpty(nodeText))
            {
                characters += $"{node.InnerText},";
            }
        }
        characters = characters.Replace(", ", ",").Trim();
        var characterList = Array.Empty<string>();
        if (!string.IsNullOrEmpty(characters))
        {
            characterList = characters.Split(",");
        }

        return new ClaudeContext
        {
            story = story.Trim(),
            description = description.Trim(),
            roomName = roomName.Replace("\n","").Trim(),
            music = music,
            characters = characterList
        };
    }

    // Update skybox
    private async void OnBlockadeReply(object message)
    {
        Debug.Log("Blockade Reply received!");
        var parsedArrayDict = message as ArrayDict;
        var url = parsedArrayDict["stringValue"] as string;

        await LoadFromUrl(url);
    }
    
    private async Task LoadFromUrl(string textureUrl)
    {
        if (!string.IsNullOrWhiteSpace(textureUrl))
        {
            var image = await ApiRequests.GetImagineImage(textureUrl);

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
    
    private static string Base64Encode(string plainText) 
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
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