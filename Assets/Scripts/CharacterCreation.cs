using Beamable;
using Beamable.Api;
using Beamable.Common.Content;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Clients;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterCreation : MonoBehaviour
{
    public static CharacterView SelectedCharacter;
    
    public CharacterSheet characterSheet;
    public GameObject cardSet1;
    public GameObject cardSet2;
    public GameObject cardSet3;

    public ContentRef<TarotContent> card1Selection;
    public ContentRef<TarotContent> card2Selection;
    public ContentRef<TarotContent> card3Selection;
    
    async void Start()
    {
        characterSheet.gameObject.SetActive(false);
        cardSet1.SetActive(false);
        cardSet2.SetActive(false);
        cardSet3.SetActive(false);
        
        await BeamContext.Default.OnReady;
        await BeamContext.Default.Accounts.OnReady;
        BeamContext.Default.Api.NotificationService.Subscribe("character.created", OnCharacterCreated);
        BeamContext.Default.Api.NotificationService.Subscribe("character.preview", OnCharacterPreview);
        
        try
        {
            SelectedCharacter = await BeamContext.Default.Microservices().GameServer().GetCharacter();
            characterSheet.gameObject.SetActive(true);
            characterSheet.Character = SelectedCharacter;
        }
        catch (PlatformRequesterException ex)
        {
            if (ex.Error.error == "CharacterNotFound")
            {
                InitializeCharacterCreator();
            }
        }
    }

    async void OnCharacterCreated(object message)
    {
        Debug.Log("Character Created!");

        SelectedCharacter = await BeamContext.Default.Microservices().GameServer().GetCharacter();
        characterSheet.gameObject.SetActive(true);
        characterSheet.Character = SelectedCharacter;
    }
    
    void OnCharacterPreview(object message)
    {
        Debug.Log("Character Preview!");
        
        var parsedArrayDict = message as ArrayDict;
        var response = parsedArrayDict["stringValue"] as string;
        var characterView = JsonConvert.DeserializeObject<CharacterView>(response);
        
        characterSheet.gameObject.SetActive(true);
        characterSheet.Character = characterView;
    }

    void InitializeCharacterCreator()
    {
        cardSet1.SetActive(true);
        cardSet2.SetActive(false);
        cardSet3.SetActive(false);

        foreach (var tarotCard in cardSet1.GetComponentsInChildren<TarotCard>())
        {
            tarotCard.GetComponent<Button>().onClick.AddListener(delegate(){
                cardSet1.SetActive(false);
                cardSet2.SetActive(true);
                cardSet3.SetActive(false);
                card1Selection = tarotCard.contentRef;
            });
        }
        
        foreach (var tarotCard in cardSet2.GetComponentsInChildren<TarotCard>())
        {
            tarotCard.GetComponent<Button>().onClick.AddListener(delegate(){
                cardSet1.SetActive(false);
                cardSet2.SetActive(false);
                cardSet3.SetActive(true);
                card2Selection = tarotCard.contentRef;
            });
        }
        
        foreach (var tarotCard in cardSet3.GetComponentsInChildren<TarotCard>())
        {
            tarotCard.GetComponent<Button>().onClick.AddListener(delegate(){
                cardSet1.SetActive(false);
                cardSet2.SetActive(false);
                cardSet3.SetActive(false);
                card3Selection = tarotCard.contentRef;
                OnTarotCardsSelected();
            });
        }
    }
    
    async void OnTarotCardsSelected()
    {
        Debug.Log("Tarot Cards Selection complete!");
        //aboutText.SetActive(true);
        var resolvedCard1 = await card1Selection.Resolve();
        var resolvedCard2 = await card2Selection.Resolve();
        var resolvedCard3 = await card3Selection.Resolve();
        
        await BeamContext.Default.Microservices().GameServer().NewCharacter(
            resolvedCard1.cardName, 
            resolvedCard2.cardName, 
            resolvedCard3.cardName
        );
    }

    public void StartAdventure()
    {
        SceneManager.LoadScene("Adventure", LoadSceneMode.Single);
    }
}
