using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace XRMultiplayer
{
    public class CharacterHeightResetter : MonoBehaviour
    {

        [SerializeField] Vector2 m_MinMaxHeight = new Vector2(0.0f, 1.8f);
        // XR origin should be fixed to 0 to match with tracking origin mode==floor in AR
        [SerializeField] float m_ResetHeight = 1.7f;
        Camera m_MainCamera;

        private void Start()
        {
            // m_TeleportationProvider = GetComponentInChildren<TeleportationProvider>(); // Removed
            // m_MainCamera = GetComponentInChildren<Camera>(); // Removed original simple search

            // Find the specific CenterEyeAnchor camera
            m_MainCamera = FindCenterEyeCamera(); // Added

            if (m_MainCamera == null) // Modified Error Check
            {
                Utils.LogError("CenterEyeAnchor Camera with tag 'MainCamera' not found under XR Origin! CharacterHeightResetter cannot function."); // Modified Error Message
            }
        }

        // Added Function to find the specific camera
        private Camera FindCenterEyeCamera()
        {
            Camera[] cameras = GetComponentsInChildren<Camera>(true); // Search all children, including inactive
            foreach (Camera cam in cameras)
            {
                if (cam.gameObject.name == "CenterEyeAnchor" && cam.CompareTag("MainCamera"))
                {
                    return cam;
                }
            }
            return null; // Return null if not found
        }

        void Update()
        {
            if (m_MainCamera == null) return;
            float currentCameraHeight = m_MainCamera.transform.position.y;

            if (currentCameraHeight < m_MinMaxHeight.x || currentCameraHeight > m_MinMaxHeight.y)
            {
                ResetHeight();
            }
        }

        void ResetHeight()
        {
            if (m_MainCamera == null)
            {
                Utils.LogError("Cannot reset player height, Main Camera reference is missing.");
                return;
            }

            float cameraLocalY = m_MainCamera.transform.localPosition.y;
            float targetOriginY = m_ResetHeight - cameraLocalY;

            Vector3 newPosition = transform.position;
            newPosition.y = targetOriginY;
            transform.position = newPosition;

            Utils.Log($"Player height reset. Origin Y moved to {transform.position.y}.");
        }
    }
} 