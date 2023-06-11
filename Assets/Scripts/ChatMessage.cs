using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatMessage : MonoBehaviour
{
    public Text messageAuthor;
    public Text messageBody;
    public RectTransform myRectTransform;

    void Update()
    {
        if (myRectTransform.sizeDelta.y != messageBody.rectTransform.sizeDelta.y + 10f)
        {
            myRectTransform.sizeDelta =
                new Vector2(myRectTransform.sizeDelta.x, messageBody.rectTransform.sizeDelta.y + 10f);
        }
    }

    public void ResizeTextToFit()
    {
        messageBody.rectTransform.sizeDelta = new Vector2(messageBody.preferredWidth, messageBody.preferredHeight);
        
        Vector3 currentScale = transform.localScale;
        transform.localScale = new Vector3(currentScale.x, messageBody.rectTransform.localScale.y, currentScale.z);
    }
}
