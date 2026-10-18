
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIRaycastDebug : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current == null || EventSystem.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Mouse clicked at: " + mousePosition);
            Debug.Log("Active input module: " +
                EventSystem.current.currentInputModule);

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = mousePosition
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count == 0)
            {
                Debug.LogWarning("NO UI ELEMENTS HIT BY RAYCAST.");
            }

            foreach (var result in results)
            {
                Debug.Log("UI hit: " + result.gameObject.name);
            }
        }
    }
}
