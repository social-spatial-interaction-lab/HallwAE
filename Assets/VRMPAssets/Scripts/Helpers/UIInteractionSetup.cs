using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace XRMultiplayer
{
    /// <summary>
    /// Helper script to ensure UI canvases are properly configured for XR interaction
    /// </summary>
    public class UIInteractionSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool m_SetupAllCanvasesOnStart = true;
        [SerializeField] private float m_DefaultRaycastDistance = 10f;
        
        void Start()
        {
            if (m_SetupAllCanvasesOnStart)
            {
                SetupAllCanvases();
            }
            
            // Ensure XR UI Input Module exists
            EnsureXRUIInputModule();
        }
        
        void SetupAllCanvases()
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            
            foreach (Canvas canvas in allCanvases)
            {
                SetupCanvasForXR(canvas);
            }
            
            Debug.Log($"[UIInteractionSetup] Set up {allCanvases.Length} canvases for XR interaction");
        }
        
        public void SetupCanvasForXR(Canvas canvas)
        {
            // Ensure the canvas has a GraphicRaycaster
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"[UIInteractionSetup] Added GraphicRaycaster to {canvas.name}");
            }
            
            // For world space canvases, ensure proper setup
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                // Add TrackedDeviceGraphicRaycaster for better XR support
                var trackedRaycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
                if (trackedRaycaster == null)
                {
                    trackedRaycaster = canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                    Debug.Log($"[UIInteractionSetup] Added TrackedDeviceGraphicRaycaster to {canvas.name}");
                }
                
                // Ensure canvas has a collider for ray interaction
                BoxCollider collider = canvas.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = canvas.gameObject.AddComponent<BoxCollider>();
                    
                    // Set collider size based on canvas rect
                    RectTransform rectTransform = canvas.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        collider.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0.01f);
                        collider.center = Vector3.zero;
                    }
                    
                    Debug.Log($"[UIInteractionSetup] Added BoxCollider to {canvas.name}");
                }
            }
            
            // Ensure all buttons have proper interaction settings
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                // Ensure buttons are on the UI layer
                if (button.gameObject.layer != LayerMask.NameToLayer("UI"))
                {
                    button.gameObject.layer = LayerMask.NameToLayer("UI");
                }
                
                // Ensure button has a proper collider for direct interaction
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    BoxCollider buttonCollider = button.GetComponent<BoxCollider>();
                    if (buttonCollider == null)
                    {
                        buttonCollider = button.gameObject.AddComponent<BoxCollider>();
                        RectTransform buttonRect = button.GetComponent<RectTransform>();
                        if (buttonRect != null)
                        {
                            buttonCollider.size = new Vector3(buttonRect.rect.width, buttonRect.rect.height, 1f);
                            buttonCollider.center = Vector3.zero;
                        }
                    }
                }
            }
        }
        
        void EnsureXRUIInputModule()
        {
            // Check if EventSystem exists
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<EventSystem>();
                Debug.Log("[UIInteractionSetup] Created EventSystem");
            }
            
            // Check for XR UI Input Module
            XRUIInputModule xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
            if (xrInputModule == null)
            {
                // Remove standard input module if it exists
                StandaloneInputModule standaloneInput = eventSystem.GetComponent<StandaloneInputModule>();
                if (standaloneInput != null)
                {
                    DestroyImmediate(standaloneInput);
                }
                
                // Add XR UI Input Module
                xrInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
                Debug.Log("[UIInteractionSetup] Added XRUIInputModule to EventSystem");
            }
        }
        
        /// <summary>
        /// Helper method to set up a specific canvas for XR interaction
        /// </summary>
        public static void ConfigureCanvasForXR(Canvas canvas, float raycastDistance = 10f)
        {
            UIInteractionSetup setup = new UIInteractionSetup();
            setup.m_DefaultRaycastDistance = raycastDistance;
            setup.SetupCanvasForXR(canvas);
        }
    }
}