
using UnityEngine;
using UnityEngine.EventSystems;

public class QuizPointerDebug : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Pointer entered: " + gameObject.name);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Pointer clicked: " + gameObject.name);
    }
}