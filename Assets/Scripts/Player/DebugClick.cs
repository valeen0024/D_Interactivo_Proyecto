using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class DebugClick : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("<color=cyan>1. ¡El mouse HIZO CLIC en la pantalla!</color>");

            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current);
                    pointerData.position = Mouse.current.position.ReadValue();

                    var results = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    if (results.Count > 0)
                    {
                        Debug.Log("<color=green>2. La UI detectó el clic sobre: " + results[0].gameObject.name + "</color>");
                    }
                }
                else
                {
                    Debug.Log("<color=orange>2. El mouse hizo clic, pero NO está sobre ningún objeto de UI (estás clickeando en el vacío).</color>");
                }
            }
            else
            {
                Debug.Log("<color=red>¡ERROR: No hay EventSystem en la escena!</color>");
            }
        }
    }
}