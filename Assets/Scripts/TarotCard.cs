using System.Collections;
using System.Collections.Generic;
using Beamable;
using Beamable.Common.Content;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        
        var assetPath = AssetDatabase.GetAssetPath(resolved.cardTexture);
        var texture = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture2D;

        //var instanceTexture = Instantiate(resolved.cardTexture);
        //var texture = instanceTexture as Texture2D;
        rawImage.texture = texture;
    }
}
