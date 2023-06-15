using UnityEngine;
using UnityEngine.UI;

public class ScrollViewFixer : MonoBehaviour
{
    public void FixScrollView()
    {
        var rectTransforms = GetComponentsInChildren<RectTransform>();
        foreach (var rectTransform in rectTransforms)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        Canvas.ForceUpdateCanvases();
        //contentView.SetActive(!contentView.activeSelf);
    }
}
