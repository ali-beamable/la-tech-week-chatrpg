using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatMessage : MonoBehaviour
{
    public Text messageAuthor;
    public Text messageBody;

    public void ResizeTextToFit()
    {
        messageBody.rectTransform.sizeDelta = new Vector2(messageBody.preferredWidth, messageBody.preferredHeight);
        
        Vector3 currentScale = transform.localScale;
        transform.localScale = new Vector3(currentScale.x, messageBody.rectTransform.localScale.y, currentScale.z);
    }
}
