using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable;
using Beamable.Common.Content;
using Beamable.Server.Clients;
using UnityEngine;
using UnityEngine.UI;

public class TarotSelector : MonoBehaviour
{
    public GameObject cardSet1;
    public GameObject cardSet2;
    public GameObject cardSet3;

    public ContentRef<TarotContent> card1Selection;
    public ContentRef<TarotContent> card2Selection;
    public ContentRef<TarotContent> card3Selection;

    public GameObject aboutText;
    
    // Start is called before the first frame update
    async void Start()
    {
        aboutText.SetActive(false);
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

        var ctx = BeamContext.Default;
        await ctx.OnReady;
    }

    public async void OnTarotCardsSelected()
    {
        Debug.Log("Tarot Cards Selection complete!");
        aboutText.SetActive(true);
        var resolvedCard1 = await card1Selection.Resolve();
        var resolvedCard2 = await card2Selection.Resolve();
        var resolvedCard3 = await card3Selection.Resolve();
        
        await BeamContext.Default.Microservices().GameServer().NewCharacter(
            "Tarinth", 
            "Male", 
            resolvedCard1.cardName, 
            resolvedCard2.cardName, 
            resolvedCard3.cardName
        );
    }
}
