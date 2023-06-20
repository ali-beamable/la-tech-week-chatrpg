using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CharacterWidget : MonoBehaviour
{
    public CharacterSheet characterSheet;
    
    public Text characterName;
    public Text characterClass;
    public Text characterLevel;
    public Text currentHp;
    public Text currentMana;
    
    public Image healthBar;
    public Image manaBar;
    
    public RawImage currentPicture;
    public GameObject healthWarning;

    private string currentImageUrl;

    public CharacterView Character
    {
        get => _characterView;
        set
        {
            if(_characterView != value)
            {
                _characterView = value;
                OnCharacterUpdated();
            }
        }
    }
    
    [SerializeField]
    private CharacterView _characterView;

    public void OnOpenCharacterSheet()
    {
        if (characterSheet != null)
        {
            characterSheet.gameObject.SetActive(true);
            characterSheet.Character = _characterView;
        }
    }
    
    void OnCharacterUpdated()
    {
        if (_characterView != null)
        {
            characterName.text = _characterView.characterName;
            characterClass.text = _characterView.characterClass;
            characterLevel.text = _characterView.characterLevel;
            currentHp.text = $"{_characterView.currentHp}/{_characterView.maxHp}";
            currentMana.text = $"{_characterView.currentMana}/{_characterView.maxMana}";

            healthBar.fillAmount = _characterView.PercentHp;
            manaBar.fillAmount = _characterView.PercentMana;

            if (_characterView.PercentHp <= 0.5 && !healthWarning.activeSelf)
            {
                healthWarning.SetActive(true);
            }
            else if(_characterView.PercentHp > 0.5 && healthWarning.activeSelf)
            {
                healthWarning.SetActive(false);
            }

            if (!string.IsNullOrEmpty(_characterView.imageUrl) && _characterView.imageUrl != currentImageUrl)
            {
                StartCoroutine(DownloadImage(_characterView.imageUrl));
            }

            if(characterSheet != null)
            {
                characterSheet.Character = _characterView;
            }
        }
    }

    IEnumerator DownloadImage(string url)
    {
        currentImageUrl = url;
        
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        if(request.result == UnityWebRequest.Result.ConnectionError) 
            Debug.Log(request.error);
        else
            currentPicture.texture = ((DownloadHandlerTexture) request.downloadHandler).texture;
    }
}
