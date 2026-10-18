
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuizButtonTest : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("QUIZ BUTTON: Pointer entered!");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("QUIZ BUTTON: Click received!");
    }

    private void Start()
    {
        Debug.Log("QuizButtonTest is active on " + gameObject.name);

        Button button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("No Button component found!");
        }
        else
        {
            button.onClick.AddListener(() =>
                Debug.Log("QUIZ BUTTON: Button onClick fired!"));
        }
    }
}
