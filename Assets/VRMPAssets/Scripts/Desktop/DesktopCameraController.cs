using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace XRMultiplayer
{
    // FPS-style mouse-look + WASD for desktop/editor play sessions.
    // Injected at runtime by DesktopModeActivator — do not place manually.
    public class DesktopCameraController : MonoBehaviour
    {
        const float k_MoveSpeed = 4f;
        const float k_LookSensitivity = 0.15f;

        float m_Pitch;
        Transform m_CameraTransform;

        void Start()
        {
            var origin = GetComponent<XROrigin>();
            m_CameraTransform = origin != null ? origin.Camera?.transform : Camera.main?.transform;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            // Toggle cursor lock
            if (kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                return;
            }
            if (Cursor.lockState == CursorLockMode.None)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    Cursor.lockState = CursorLockMode.Locked;
                return;
            }

            // Mouse look
            var delta = mouse.delta.ReadValue();
            transform.Rotate(0f, delta.x * k_LookSensitivity, 0f);
            m_Pitch = Mathf.Clamp(m_Pitch - delta.y * k_LookSensitivity, -80f, 80f);
            if (m_CameraTransform != null)
                m_CameraTransform.localRotation = Quaternion.Euler(m_Pitch, 0f, 0f);

            // WASD movement
            float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            transform.Translate(new Vector3(h, 0f, v) * k_MoveSpeed * Time.deltaTime, Space.Self);
        }
    }
}
