using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace XRMultiplayer
{
    /// <summary>
    /// Helper script to ensure Quest 3 controllers and ray interactors are properly configured
    /// Works with OVRCameraRig (tagged as PlayerRig)
    /// </summary>
    [HelpURL("Assign controller prefabs from: Assets/Samples/XR Interaction Toolkit/[version]/Starter Assets/Prefabs/Controllers/")]
    public class XRControllerSetup : MonoBehaviour
    {
        [Header("Controller References")]
        [Tooltip("Assign XR Controller Left prefab from XR Interaction Toolkit Starter Assets")]
        [SerializeField] private GameObject m_LeftControllerModel;
        [Tooltip("Assign XR Controller Right prefab from XR Interaction Toolkit Starter Assets")]
        [SerializeField] private GameObject m_RightControllerModel;
        
        [Header("Ray Interactor Settings")]
        [SerializeField] private Material m_RayMaterial;
        [SerializeField] private Gradient m_ValidColorGradient;
        [SerializeField] private Gradient m_InvalidColorGradient;
        [SerializeField] private Gradient m_BlockedColorGradient;
        
        [Header("Ray Settings")]
        [SerializeField] private float m_RayLength = 10f;
        [SerializeField] private float m_RayWidth = 0.01f;
        [SerializeField] private bool m_UseDefaultBlueRay = true;
        
        private Transform m_OVRCameraRig;
        private Transform m_LeftController;
        private Transform m_RightController;
        
        void Start()
        {
            SetupControllers();
        }
        
        void SetupControllers()
        {
            // Find OVRCameraRig by tag
            GameObject ovrRig = GameObject.FindWithTag("PlayerRig");
            if (ovrRig == null)
            {
                // Try by name as fallback
                ovrRig = GameObject.Find("OVRCameraRig");
            }
            
            if (ovrRig == null)
            {
                Debug.LogError("[XRControllerSetup] OVRCameraRig not found! Make sure it's tagged as 'PlayerRig'");
                return;
            }
            
            m_OVRCameraRig = ovrRig.transform;
            Debug.Log("[XRControllerSetup] Found OVRCameraRig");
            
            // Find TrackingSpace under OVRCameraRig
            Transform trackingSpace = m_OVRCameraRig.Find("TrackingSpace");
            if (trackingSpace == null)
            {
                Debug.LogError("[XRControllerSetup] TrackingSpace not found under OVRCameraRig!");
                return;
            }
            
            // Try to find controllers with OVR naming conventions
            m_LeftController = trackingSpace.Find("LeftHandAnchor");
            if (m_LeftController == null) m_LeftController = trackingSpace.Find("LeftControllerAnchor");
            if (m_LeftController == null) m_LeftController = trackingSpace.Find("Left Controller");
            
            m_RightController = trackingSpace.Find("RightHandAnchor");
            if (m_RightController == null) m_RightController = trackingSpace.Find("RightControllerAnchor");
            if (m_RightController == null) m_RightController = trackingSpace.Find("Right Controller");
            
            if (m_LeftController == null || m_RightController == null)
            {
                Debug.LogError($"[XRControllerSetup] Controller anchors not found! Left: {m_LeftController}, Right: {m_RightController}");
                
                // Log all children to help debug
                Debug.Log("[XRControllerSetup] Available children under TrackingSpace:");
                foreach (Transform child in trackingSpace)
                {
                    Debug.Log($"  - {child.name}");
                }
                return;
            }
            
            Debug.Log($"[XRControllerSetup] Found controllers - Left: {m_LeftController.name}, Right: {m_RightController.name}");
            
            // Setup left controller
            SetupController(m_LeftController, true);
            
            // Setup right controller  
            SetupController(m_RightController, false);
        }
        
        void SetupController(Transform controller, bool isLeft)
        {
            // Note: ActionBasedController is deprecated in XR Interaction Toolkit 3.0
            // The controller prefabs from XR Interaction Toolkit should already have the necessary components
            
            // Check for Ray Interactor
            var rayInteractor = controller.GetComponentInChildren<XRRayInteractor>();
            if (rayInteractor == null)
            {
                // Create ray interactor as child
                GameObject rayGO = new GameObject(isLeft ? "Left Ray Interactor" : "Right Ray Interactor");
                rayGO.transform.SetParent(controller);
                rayGO.transform.localPosition = Vector3.zero;
                rayGO.transform.localRotation = Quaternion.identity;
                
                rayInteractor = rayGO.AddComponent<XRRayInteractor>();
                Debug.Log($"[XRControllerSetup] Added Ray Interactor to {controller.name}");
            }
            
            // Configure ray interactor
            ConfigureRayInteractor(rayInteractor);
            
            // Setup line visual
            SetupLineVisual(rayInteractor);
            
            // Add controller model if provided
            if (isLeft && m_LeftControllerModel != null)
            {
                AddControllerModel(controller, m_LeftControllerModel);
            }
            else if (!isLeft && m_RightControllerModel != null)
            {
                AddControllerModel(controller, m_RightControllerModel);
            }
            
            // Enable UI interaction
            rayInteractor.enableUIInteraction = true;
            
            Debug.Log($"[XRControllerSetup] Completed setup for {controller.name}");
        }
        
        void ConfigureRayInteractor(XRRayInteractor rayInteractor)
        {
            // Set ray length
            rayInteractor.maxRaycastDistance = m_RayLength;
            
            // Enable UI interaction
            rayInteractor.enableUIInteraction = true;
            
            // Set interaction layers (ensure UI layer is included)
            rayInteractor.interactionLayers = InteractionLayerMask.GetMask("Default", "UI");
            
            // Configure ray type
            rayInteractor.lineType = XRRayInteractor.LineType.StraightLine;
            
            // Enable selection
            rayInteractor.allowSelect = true;
            rayInteractor.allowHover = true;
        }
        
        void SetupLineVisual(XRRayInteractor rayInteractor)
        {
            // Get or add Line Renderer
            LineRenderer lineRenderer = rayInteractor.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = rayInteractor.gameObject.AddComponent<LineRenderer>();
            }
            
            // Configure line renderer
            lineRenderer.startWidth = m_RayWidth;
            lineRenderer.endWidth = m_RayWidth * 0.5f;
            lineRenderer.numCapVertices = 5;
            lineRenderer.numCornerVertices = 5;
            
            // Set material
            if (m_UseDefaultBlueRay || m_RayMaterial == null)
            {
                // Create default blue material
                Material blueMaterial = new Material(Shader.Find("Sprites/Default"));
                blueMaterial.color = new Color(0f, 0.5f, 1f, 0.8f); // Light blue with slight transparency
                lineRenderer.material = blueMaterial;
            }
            else if (m_RayMaterial != null)
            {
                lineRenderer.material = m_RayMaterial;
            }
            
            // Get or add XR Interactor Line Visual
            var lineVisual = rayInteractor.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
            if (lineVisual == null)
            {
                lineVisual = rayInteractor.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
            }
            
            // Configure line visual
            lineVisual.lineWidth = m_RayWidth;
            lineVisual.overrideInteractorLineLength = true;
            lineVisual.lineLength = m_RayLength;
            
            // Set color gradients
            if (m_UseDefaultBlueRay)
            {
                // Use blue-themed gradients
                lineVisual.validColorGradient = CreateDefaultBlueValidGradient();
                lineVisual.invalidColorGradient = CreateDefaultInvalidGradient();
                lineVisual.blockedColorGradient = CreateDefaultBlockedGradient();
            }
            else
            {
                // Use provided gradients if available
                if (m_ValidColorGradient != null)
                    lineVisual.validColorGradient = m_ValidColorGradient;
                if (m_InvalidColorGradient != null)
                    lineVisual.invalidColorGradient = m_InvalidColorGradient;
                if (m_BlockedColorGradient != null)
                    lineVisual.blockedColorGradient = m_BlockedColorGradient;
            }
        }
        
        void AddControllerModel(Transform controller, GameObject modelPrefab)
        {
            // Check if model already exists
            Transform existingModel = controller.Find("Controller Model");
            if (existingModel != null)
            {
                DestroyImmediate(existingModel.gameObject);
            }
            
            // Instantiate new model
            GameObject model = Instantiate(modelPrefab, controller);
            model.name = "Controller Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            
            Debug.Log($"[XRControllerSetup] Added controller model to {controller.name}");
        }
        
        // Helper method to create default blue gradient for valid state
        public static Gradient CreateDefaultBlueValidGradient()
        {
            Gradient gradient = new Gradient();
            Color lightBlue = new Color(0.2f, 0.6f, 1f);
            Color blue = new Color(0f, 0.5f, 1f);
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(lightBlue, 0.0f), new GradientColorKey(blue, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.8f, 1.0f) }
            );
            return gradient;
        }
        
        // Keep the original method for compatibility
        public static Gradient CreateDefaultValidGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            );
            return gradient;
        }
        
        public static Gradient CreateDefaultInvalidGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.red, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.5f, 0.0f), new GradientAlphaKey(0.5f, 1.0f) }
            );
            return gradient;
        }
        
        public static Gradient CreateDefaultBlockedGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(Color.yellow, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0.0f), new GradientAlphaKey(0.7f, 1.0f) }
            );
            return gradient;
        }
    }
}