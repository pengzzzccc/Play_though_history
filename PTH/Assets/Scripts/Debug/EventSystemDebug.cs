using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemDebug : MonoBehaviour
{
    void Update()
    {
        if (EventSystem.current == null)
            return;

        Debug.Log(
            "Current Input Module: " +
            EventSystem.current.currentInputModule
        );
    }
}