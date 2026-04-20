using UnityEngine;

public class SpiritPanelInput : MonoBehaviour
{
    [Header("Panel Controller")]
    [SerializeField]
    private SpiritPanelController spiritPanelController;

    [Header("Hotkey")]
    [SerializeField]
    private KeyCode toggleKey = KeyCode.Space;

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
        {
            return;
        }

        if (spiritPanelController == null)
        {
            Debug.LogWarning("[SpiritPanelInput] SpiritPanelController is not assigned!");
            return;
        }

        DialogController dialogController = FindObjectOfType<DialogController>(true);
        if (dialogController != null && dialogController.IsDialogueActive())
        {
            return;
        }

        spiritPanelController.TogglePanel();
    }
}
