using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CharacterSheet : MonoBehaviour
{
    private CharacterView _character;
    public CharacterView Character
    {
        get => _character;
        set
        {
            _character = value;
            OnCharacterUpdated();
        }
    }
    
    public Text characterNameText;
    public Text classText;
    public Text speciesText;
    public Text genderText;
    public Text healthText;
    public Text manaText;
    public Text levelText;
    
    public Text strengthText;
    public Text dexterityText;
    public Text constitutionText;
    public Text intelligenceText;
    public Text charismaText;
    public Text wisdomText;
    
    public Text nemesisNameText;
    public Text nemesisBackgroundText;
    
    public Text bodyText;

    public RawImage portraitImage;
    public GameObject portraitLoadingWidget;
    
    private void OnCharacterUpdated()
    {
        if (_character != null)
        {
            characterNameText.text = _character.characterName;
            classText.text = _character.characterClass;
            speciesText.text = _character.characterRace;
            genderText.text = _character.characterGender;
            healthText.text = _character.maxHp.ToString();
            manaText.text = _character.maxMana.ToString();
            levelText.text = _character.characterLevel;
            strengthText.text = _character.strength.ToString();
            dexterityText.text = _character.dexterity.ToString();
            constitutionText.text = _character.constitution.ToString();
            intelligenceText.text = _character.intelligence.ToString();
            charismaText.text = _character.charisma.ToString();
            wisdomText.text = _character.wisdom.ToString();
            nemesisNameText.text = _character.nemesisName;
            nemesisBackgroundText.text = ""; // blank for now
            bodyText.text = $"Description:\n{_character.characterDescription}\n\nBackground:\n{_character.characterBackground}\n\nNemesis Description:\n{_character.nemesisDescription}";

            if (string.IsNullOrEmpty(_character.imageUrl))
            {
                portraitLoadingWidget.SetActive(true);
            }
            else
            {
                StartCoroutine(DownloadImage(_character.imageUrl));
            }
        }
    }
    
    IEnumerator DownloadImage(string url)
    {   
        portraitLoadingWidget.SetActive(true);
        
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        
        portraitLoadingWidget.SetActive(false);
        if(request.result == UnityWebRequest.Result.ConnectionError) 
            Debug.Log(request.error);
        else
            portraitImage.texture = ((DownloadHandlerTexture) request.downloadHandler).texture;
    }
}
