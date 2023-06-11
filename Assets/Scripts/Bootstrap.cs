using Beamable;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
        await BeamContext.Default.OnReady;
        await BeamContext.Default.Accounts.OnReady;
    }

    public void OnCreateButtonClicked()
    {
        SceneManager.LoadScene("CharacterCreation", LoadSceneMode.Single);
    }
}
