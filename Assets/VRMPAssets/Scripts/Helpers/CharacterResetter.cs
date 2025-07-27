using System;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// In AR multiplayer, this script only provides boundary warnings.
    /// We cannot and should not reset player positions in AR.
    /// </summary>
    public class CharacterResetter : MonoBehaviour
    {
        [SerializeField] float m_WarningDistance = 15.0f;
        [SerializeField] bool m_ShowDistanceWarnings = true;
        Camera m_MainCamera;
        bool m_IsOutOfBounds = false;

        private void Start()
        {
            m_MainCamera = FindCenterEyeCamera();
            if (m_MainCamera == null)
            {
                Utils.LogError("CenterEyeAnchor Camera with tag 'MainCamera' not found! CharacterResetter cannot function.");
            }
        }

        private Camera FindCenterEyeCamera()
        {
            Camera[] cameras = GetComponentsInChildren<Camera>(true);
            foreach (Camera cam in cameras)
            {
                if (cam.gameObject.name == "CenterEyeAnchor" && cam.CompareTag("MainCamera"))
                {
                    return cam;
                }
            }
            return null;
        }

        void Update()
        {
            if (m_MainCamera == null || !m_ShowDistanceWarnings) return;
            
            Vector3 cameraPos = m_MainCamera.transform.position;
            float distance = Mathf.Sqrt(cameraPos.x * cameraPos.x + cameraPos.z * cameraPos.z);
            
            bool isCurrentlyOutOfBounds = distance > m_WarningDistance;
            
            // Only log when crossing the boundary
            if (isCurrentlyOutOfBounds != m_IsOutOfBounds)
            {
                m_IsOutOfBounds = isCurrentlyOutOfBounds;
                
                if (m_IsOutOfBounds)
                {
                    Utils.LogWarning($"Player has moved {distance:F1}m from origin, exceeding the {m_WarningDistance}m boundary. " +
                                   "In AR, players should stay within their physical play space.");
                }
                else
                {
                    Utils.Log("Player returned to play area.");
                }
            }
        }

    }
}
