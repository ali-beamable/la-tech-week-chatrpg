using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CharacterWidget : MonoBehaviour
{
    public Text characterName;
    public Text characterClass;
    public Text characterLevel;
    public Text currentHp;
    public Text currentMana;
    
    public Image healthBar;
    public Image manaBar;
    
    public RawImage currentPicture;

    public GameObject healthWarning;

    [SerializeField]
    private CharacterView _characterView;

    // Update is called once per frame
    void Update()
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

            if (_characterView.PercentHp <= 50 && !healthWarning.activeSelf)
            {
                healthWarning.SetActive(true);
            }
            else if(_characterView.PercentHp > 50 && healthWarning.activeSelf)
            {
                healthWarning.SetActive(false);
            }
        }
    }

    public void SetCharacter(CharacterView characterView)
    {
        _characterView = characterView;
        if (_characterView != null)
        {
            StartCoroutine(DownloadImage(_characterView.imageUrl));
        }
    }

    IEnumerator DownloadImage(string url)
    {   
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        if(request.result == UnityWebRequest.Result.ConnectionError) 
            Debug.Log(request.error);
        else
            currentPicture.texture = ((DownloadHandlerTexture) request.downloadHandler).texture;
    }
}
