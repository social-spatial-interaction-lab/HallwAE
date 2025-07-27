using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace XRMultiplayer
{
    /// <summary>
    /// In AR multiplayer, this script only provides height warnings.
    /// We cannot adjust player height in AR - it's determined by physical device position.
    /// </summary>
    public class CharacterHeightResetter : MonoBehaviour
    {
        [SerializeField] Vector2 m_MinMaxHeight = new Vector2(0.5f, 2.0f);
        [SerializeField] bool m_ShowHeightWarnings = true;
        Camera m_MainCamera;
        bool m_HeightWarningShown = false;

        private void Start()
        {
            m_MainCamera = FindCenterEyeCamera();
            if (m_MainCamera == null)
            {
                Utils.LogError("CenterEyeAnchor Camera with tag 'MainCamera' not found! CharacterHeightResetter cannot function.");
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
            if (m_MainCamera == null || !m_ShowHeightWarnings) return;
            
            float currentHeight = m_MainCamera.transform.position.y;
            bool isOutOfRange = currentHeight < m_MinMaxHeight.x || currentHeight > m_MinMaxHeight.y;
            
            // Only show warning once when entering out-of-range state
            if (isOutOfRange && !m_HeightWarningShown)
            {
                m_HeightWarningShown = true;
                
                if (currentHeight < m_MinMaxHeight.x)
                {
                    Utils.LogWarning($"Camera height ({currentHeight:F2}m) is below recommended minimum ({m_MinMaxHeight.x:F2}m). " +
                                   "Consider raising your device for better experience.");
                }
                else
                {
                    Utils.LogWarning($"Camera height ({currentHeight:F2}m) is above recommended maximum ({m_MinMaxHeight.y:F2}m). " +
                                   "Consider lowering your device for better experience.");
                }
            }
            else if (!isOutOfRange && m_HeightWarningShown)
            {
                m_HeightWarningShown = false;
                Utils.Log($"Camera height ({currentHeight:F2}m) is now within recommended range.");
            }
        }
    }
} 