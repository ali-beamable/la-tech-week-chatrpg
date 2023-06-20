using Beamable;
using Beamable.Common.Content;
using UnityEngine;
using UnityEngine.UI;

public class TarotCard : MonoBehaviour
{
    public ContentRef<TarotContent> contentRef;
    
    // Start is called before the first frame update
    async void Start()
    {
        await BeamContext.Default.OnReady;
        var resolved = await contentRef.Resolve();
        var rawImage = GetComponent<RawImage>();
        var textureAsset = await resolved.textureReference.LoadAssetAsync().Task;

        rawImage.texture = textureAsset;
    }
}
