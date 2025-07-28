using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using System.Collections.Generic;

namespace XRMultiplayer
{
    /// <summary>
    /// Manages proper hand/controller switching for Quest 3
    /// Fixes issues with controller models stuck on hands
    /// Works alongside XRControllerSetup which handles ray interactors
    /// This script focuses on visual model switching and cleanup
    /// </summary>
    public class Quest3HandControllerManager : MonoBehaviour
    {
        [Header("Hand Model Setup")]
        [Tooltip("Prefabs that will be instantiated for hand tracking visuals")]
        [SerializeField] private GameObject m_LeftHandPrefab;
        [SerializeField] private GameObject m_RightHandPrefab;
        
        [Header("Controller Model Setup")]
        [Tooltip("Prefabs that will be instantiated for controller visuals")]
        [SerializeField] private GameObject m_LeftControllerModelPrefab;
        [SerializeField] private GameObject m_RightControllerModelPrefab;
        
        [Header("Tracking Space")]
        [SerializeField] private Transform m_TrackingSpace;
        
        private XRInputModalityManager m_ModalityManager;
        private XRHandSubsystem m_HandSubsystem;
        private List<XRHandSubsystem> m_HandSubsystems = new List<XRHandSubsystem>();
        
        private GameObject m_LeftHandInstance;
        private GameObject m_RightHandInstance;
        private GameObject m_LeftControllerInstance;
        private GameObject m_RightControllerInstance;
        
        private bool m_CurrentlyUsingHands = false;
        
        void Start()
        {
            // Find modality manager
            m_ModalityManager = FindFirstObjectByType<XRInputModalityManager>();
            
            // Find tracking space if not assigned
            if (m_TrackingSpace == null)
            {
                GameObject ovrRig = GameObject.FindWithTag("PlayerRig");
                if (ovrRig != null)
                {
                    m_TrackingSpace = ovrRig.transform.Find("TrackingSpace");
                }
            }
            
            // Initialize hand subsystem
            SubsystemManager.GetSubsystems(m_HandSubsystems);
            if (m_HandSubsystems.Count > 0)
            {
                m_HandSubsystem = m_HandSubsystems[0];
            }
            
            // Setup hand and controller models
            SetupHandAndControllerModels();
            
            // Subscribe to modality changes if available
            if (m_ModalityManager != null)
            {
                m_ModalityManager.trackedHandModeStarted.AddListener(OnHandModeStarted);
                m_ModalityManager.motionControllerModeStarted.AddListener(OnControllerModeStarted);
                Debug.Log("[Quest3HandControllerManager] Subscribed to modality manager events");
            }
            else
            {
                Debug.LogWarning("[Quest3HandControllerManager] XRInputModalityManager not found - manual mode detection will be used");
            }
        }
        
        void OnDestroy()
        {
            if (m_ModalityManager != null)
            {
                m_ModalityManager.trackedHandModeStarted.RemoveListener(OnHandModeStarted);
                m_ModalityManager.motionControllerModeStarted.RemoveListener(OnControllerModeStarted);
            }
        }
        
        void Update()
        {
            // Manual detection if modality manager is not available
            if (m_ModalityManager == null && m_HandSubsystem != null)
            {
                bool handsTracked = m_HandSubsystem.leftHand.isTracked || m_HandSubsystem.rightHand.isTracked;
                
                if (handsTracked && !m_CurrentlyUsingHands)
                {
                    OnHandModeStarted();
                }
                else if (!handsTracked && m_CurrentlyUsingHands)
                {
                    OnControllerModeStarted();
                }
            }
        }
        
        void SetupHandAndControllerModels()
        {
            if (m_TrackingSpace == null)
            {
                Debug.LogError("[Quest3HandControllerManager] Tracking space not found!");
                return;
            }
            
            // Find anchors
            Transform leftAnchor = m_TrackingSpace.Find("LeftHandAnchor");
            Transform rightAnchor = m_TrackingSpace.Find("RightHandAnchor");
            
            if (leftAnchor == null || rightAnchor == null)
            {
                Debug.LogError("[Quest3HandControllerManager] Hand anchors not found!");
                return;
            }
            
            // Create hand models if prefabs are assigned
            if (m_LeftHandPrefab != null)
            {
                m_LeftHandInstance = Instantiate(m_LeftHandPrefab, leftAnchor);
                m_LeftHandInstance.name = "Left Hand Visual";
                m_LeftHandInstance.SetActive(false);
            }
            
            if (m_RightHandPrefab != null)
            {
                m_RightHandInstance = Instantiate(m_RightHandPrefab, rightAnchor);
                m_RightHandInstance.name = "Right Hand Visual";
                m_RightHandInstance.SetActive(false);
            }
            
            // Create controller models if prefabs are assigned
            if (m_LeftControllerModelPrefab != null)
            {
                m_LeftControllerInstance = Instantiate(m_LeftControllerModelPrefab, leftAnchor);
                m_LeftControllerInstance.name = "Left Controller Model";
                m_LeftControllerInstance.SetActive(false);
            }
            
            if (m_RightControllerModelPrefab != null)
            {
                m_RightControllerInstance = Instantiate(m_RightControllerModelPrefab, rightAnchor);
                m_RightControllerInstance.name = "Right Controller Model";
                m_RightControllerInstance.SetActive(false);
            }
            
            Debug.Log("[Quest3HandControllerManager] Hand and controller models setup completed");
        }
        
        void OnHandModeStarted()
        {
            Debug.Log("[Quest3HandControllerManager] Switching to hand mode");
            m_CurrentlyUsingHands = true;
            
            // Hide all controller visuals
            SetControllerVisualsActive(false);
            
            // Show hand visuals
            SetHandVisualsActive(true);
            
            // Ensure no controller models are stuck on hands
            CleanupStuckControllerModels();
        }
        
        void OnControllerModeStarted()
        {
            Debug.Log("[Quest3HandControllerManager] Switching to controller mode");
            m_CurrentlyUsingHands = false;
            
            // Hide hand visuals
            SetHandVisualsActive(false);
            
            // Show controller visuals
            SetControllerVisualsActive(true);
        }
        
        void SetHandVisualsActive(bool active)
        {
            if (m_LeftHandInstance != null)
                m_LeftHandInstance.SetActive(active);
            
            if (m_RightHandInstance != null)
                m_RightHandInstance.SetActive(active);
        }
        
        void SetControllerVisualsActive(bool active)
        {
            if (m_LeftControllerInstance != null)
                m_LeftControllerInstance.SetActive(active);
            
            if (m_RightControllerInstance != null)
                m_RightControllerInstance.SetActive(active);
        }
        
        void CleanupStuckControllerModels()
        {
            // Find and disable any controller models that might be stuck on hand transforms
            if (m_TrackingSpace != null)
            {
                // Check all children of hand anchors for controller models
                Transform[] handAnchors = new Transform[] {
                    m_TrackingSpace.Find("LeftHandAnchor"),
                    m_TrackingSpace.Find("RightHandAnchor")
                };
                
                foreach (var anchor in handAnchors)
                {
                    if (anchor == null) continue;
                    
                    // Look for any controller model components
                    foreach (Transform child in anchor)
                    {
                        // Skip our managed instances
                        if (child.gameObject == m_LeftControllerInstance || 
                            child.gameObject == m_RightControllerInstance ||
                            child.gameObject == m_LeftHandInstance ||
                            child.gameObject == m_RightHandInstance)
                            continue;
                        
                        // Check if this might be a controller model
                        if (child.name.ToLower().Contains("controller") && 
                            !child.name.ToLower().Contains("hand"))
                        {
                            Debug.Log($"[Quest3HandControllerManager] Disabling stuck controller model: {child.name}");
                            child.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Manually force switch to hand mode
        /// </summary>
        public void ForceHandMode()
        {
            OnHandModeStarted();
        }
        
        /// <summary>
        /// Manually force switch to controller mode
        /// </summary>
        public void ForceControllerMode()
        {
            OnControllerModeStarted();
        }
    }
}