using System.Collections;
using System.Collections.Generic;
using Beamable;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Clients;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreation : MonoBehaviour
{
    public GameObject chatMessagePrefab;
    public GameObject chatScrollView;

    public Text claudeTextResponse;
    public InputField chatInputField;

    private string claudeContext = "";

    public string playerName = "Player 1";
    
    // Start is called before the first frame update
    async void Start()
    {
        claudeTextResponse.text = "";
        RemoveAllChildren(chatScrollView);
        
        await BeamContext.Default.Accounts.OnReady;
        var playerAlias = BeamContext.Default.Accounts.Current.Alias;
        if (!string.IsNullOrEmpty(playerAlias))
        {
            playerName = playerAlias;
        }
        
        BeamContext.Default.Api.NotificationService.Subscribe("claude.reply", OnClaudeReply);
        await BeamContext.Default.Microservices().GameServer().NewCharacter("");
    }

    void OnClaudeReply(object message)
    {
        Debug.Log("Claude Message Received!");

        var parsedArrayDict = message as ArrayDict;
        var response = parsedArrayDict["stringValue"] as string;
        claudeContext += $"<claude>{response}</claude>";;
        claudeTextResponse.text += response;
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
        claudeContext += $"<player>{chatInput}</player>";
        
        
        await BeamContext.Default.Microservices().GameServer().NewCharacter(Base64Encode(claudeContext));

        /*
        var newMessage = Instantiate(chatMessagePrefab, chatScrollView.transform);
        var chatMessage = newMessage.GetComponent<ChatMessage>();
        chatMessage.messageAuthor.text = "[Player 1]:";
        chatMessage.messageBody.text = chatInput;
        */
    }
    
    private static string Base64Encode(string plainText) 
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }
}
