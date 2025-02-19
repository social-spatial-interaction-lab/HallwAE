using UnityEngine;
using UnityEngine.InputSystem;

public class MenuButton : MonoBehaviour
{
    [SerializeField] private InputActionReference _menuButtonAction;
    [SerializeField] private GameObject _menuWindow;

    private void Awake()
    {
        // Directly bind the action event
        _menuButtonAction.action.performed += ToggleMenu;
        _menuButtonAction.action.Enable();
    }

    private void OnDestroy()
    {
        // Cleanup when object is destroyed
        _menuButtonAction.action.performed -= ToggleMenu;
    }

    private void ToggleMenu(InputAction.CallbackContext context)
    {
        _menuWindow.SetActive(!_menuWindow.activeSelf);
    }
}

