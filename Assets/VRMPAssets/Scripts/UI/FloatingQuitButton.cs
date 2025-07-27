using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace XRMultiplayer
{
    /// <summary>
    /// A floating quit button that follows the player camera with smooth movement.
    /// Designed for AR applications where fixed-position UI elements are problematic.
    /// </summary>
    public class FloatingQuitButton : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] Transform m_FollowTarget; // Usually the Main Camera
        [SerializeField] float m_Distance = 2.0f; // Distance in front of camera
        [SerializeField] float m_Height = 0.3f; // Height offset from camera center
        [SerializeField] float m_HorizontalOffset = 0.8f; // Offset to the right
        [SerializeField] float m_SmoothTime = 0.3f; // Smooth follow time
        
        [Header("Behavior")]
        [SerializeField] bool m_OnlyShowWhenLookingForward = true;
        [SerializeField] float m_MinPitch = -30f; // Don't show if looking down too much
        [SerializeField] float m_MaxPitch = 60f; // Don't show if looking up too much
        [SerializeField] bool m_BillboardMode = true; // Always face the camera
        
        [Header("UI References")]
        [SerializeField] Button m_QuitButton;
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] float m_FadeSpeed = 2.0f;
        
        Camera m_MainCamera;
        Vector3 m_Velocity = Vector3.zero;
        float m_CurrentAlpha = 1.0f;
        bool m_ShouldBeVisible = true;
        
        void Start()
        {
            // Find main camera if not set
            if (m_FollowTarget == null)
            {
                m_MainCamera = Camera.main;
                if (m_MainCamera != null)
                {
                    m_FollowTarget = m_MainCamera.transform;
                }
                else
                {
                    Debug.LogError("No Main Camera found for FloatingQuitButton!");
                    enabled = false;
                    return;
                }
            }
            else
            {
                m_MainCamera = m_FollowTarget.GetComponent<Camera>();
                if (m_MainCamera == null)
                    m_MainCamera = Camera.main;
            }
            
            // Setup quit button
            if (m_QuitButton != null)
            {
                m_QuitButton.onClick.AddListener(OnQuitButtonPressed);
            }
            
            // Ensure we have a CanvasGroup for fading
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = GetComponent<CanvasGroup>();
                if (m_CanvasGroup == null)
                    m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Initial position
            UpdatePosition(true);
        }
        
        void Update()
        {
            if (m_FollowTarget == null) return;
            
            // Check visibility conditions
            UpdateVisibility();
            
            // Update position
            UpdatePosition(false);
            
            // Update rotation (billboard)
            if (m_BillboardMode)
            {
                UpdateRotation();
            }
            
            // Update fade
            UpdateFade();
        }
        
        void UpdateVisibility()
        {
            if (m_OnlyShowWhenLookingForward)
            {
                // Get camera pitch (x rotation)
                float pitch = m_FollowTarget.eulerAngles.x;
                if (pitch > 180) pitch -= 360; // Convert to -180 to 180 range
                
                m_ShouldBeVisible = pitch >= m_MinPitch && pitch <= m_MaxPitch;
            }
            else
            {
                m_ShouldBeVisible = true;
            }
        }
        
        void UpdatePosition(bool instant)
        {
            // Calculate target position
            Vector3 forward = m_FollowTarget.forward;
            forward.y = 0; // Keep it level
            forward.Normalize();
            
            // If forward is zero (looking straight up/down), use world forward
            if (forward.magnitude < 0.1f)
                forward = Vector3.forward;
            
            Vector3 right = m_FollowTarget.right;
            right.y = 0;
            right.Normalize();
            
            Vector3 targetPosition = m_FollowTarget.position + 
                                   forward * m_Distance + 
                                   Vector3.up * m_Height + 
                                   right * m_HorizontalOffset;
            
            // Apply smooth follow or instant
            if (instant)
            {
                transform.position = targetPosition;
                m_Velocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref m_Velocity, m_SmoothTime);
            }
        }
        
        void UpdateRotation()
        {
            // Look at camera but stay upright
            Vector3 lookDirection = m_FollowTarget.position - transform.position;
            lookDirection.y = 0;
            
            if (lookDirection.magnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
        
        void UpdateFade()
        {
            float targetAlpha = m_ShouldBeVisible ? 1.0f : 0.0f;
            m_CurrentAlpha = Mathf.Lerp(m_CurrentAlpha, targetAlpha, Time.deltaTime * m_FadeSpeed);
            
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = m_CurrentAlpha;
                m_CanvasGroup.interactable = m_CurrentAlpha > 0.5f;
                m_CanvasGroup.blocksRaycasts = m_CurrentAlpha > 0.5f;
            }
        }
        
        async void OnQuitButtonPressed()
        {
            Debug.Log("Quit button pressed - Exiting application");
            
            // Use the same quit logic as PlayerOptions
            if (XRINetworkGameManager.Connected.Value)
            {
                await XRINetworkGameManager.Instance.DisconnectAsync();
            }
            
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        /// <summary>
        /// Call this to immediately reposition the button in front of the camera
        /// </summary>
        public void Recenter()
        {
            UpdatePosition(true);
        }
        
        /// <summary>
        /// Temporarily show the button even if visibility conditions aren't met
        /// </summary>
        public void ForceShow(float duration = 3.0f)
        {
            StopAllCoroutines();
            StartCoroutine(ForceShowCoroutine(duration));
        }
        
        System.Collections.IEnumerator ForceShowCoroutine(float duration)
        {
            bool previousOnlyShowWhenLookingForward = m_OnlyShowWhenLookingForward;
            m_OnlyShowWhenLookingForward = false;
            
            yield return new WaitForSeconds(duration);
            
            m_OnlyShowWhenLookingForward = previousOnlyShowWhenLookingForward;
        }
    }
}