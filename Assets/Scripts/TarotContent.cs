using Beamable.Common.Content;
using UnityEngine;
using UnityEngine.AddressableAssets;

[ContentType("tarot")]
public class TarotContent : ContentObject
{
    public string cardName;
    public string cardDescription;
    public AssetReferenceTexture2D textureReference;
}
