using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace XRMultiplayer
{
    /// <summary>
    /// This class will synchronize the hand poses over the network.
    /// It will also allow for the user to control the fidelity of the hand poses and how much data is being sent over the network.
    /// This class is going to have major changes in the near future based on the Hand Pose work being done.
    /// </summary>
    public class XRHandPoseReplicator : NetworkBehaviour
    {
        /// <summary>
        /// Controls the level of fidelity and how much data is being sent over the network.
        /// 0 is highest level of fidelity and the most bandwidth, 2 is the lowest and the least bandwidth.
        /// </summary>
        [Header("Hands and Fingers"), Tooltip("0 is highest, 2 is lowest")]
        [Range(0, 2), SerializeField] int m_FidelityLevel;
        [SerializeField, Tooltip("Determines minimum value threshold for updating finger rotations")] float m_MinUpdateDelta = .1f;
        [SerializeField] JointBasedHand[] m_HandCurler;

        [SerializeField] float m_FingerLerpSpeed = 20.0f;
        [SerializeField] bool m_UpdateHandsLocally;

        [Header("Controller Inputs")]
        [SerializeField] InputActionProperty[] m_GripInputProperties;
        [SerializeField] InputActionProperty[] m_TriggerInputProperties;
        [SerializeField] InputActionProperty[] m_ThumbTouchProperties;

        NetworkList<Vector3> m_FingerRotationsLeft;
        NetworkList<Vector3> m_FingerRotationsRight;

        NetworkList<float> m_FingerCurlLeft;
        NetworkList<float> m_FingerCurlRight;

        NetworkVariable<bool> m_IsInitialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        Pose[] m_handTrackedStartPose = new Pose[2];

        [Header("Offsets")]
        [SerializeField] Vector3[] m_HandControllerOffsets;
        [SerializeField] Vector3[] m_HandControllerEulerOffsets;

        public XRInputModalityManager.InputMode trackingType { get => m_TrackingType.Value; }
        readonly NetworkVariable<XRInputModalityManager.InputMode> m_TrackingType = new(XRInputModalityManager.InputMode.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        XRInputModalityManager m_XRModalityManager;
        Transform m_OVRCameraRig;

        Transform m_LeftHandTransformReference;
        Transform m_RightHandTransformReference;

        Transform m_LeftControllerTransformReference;
        Transform m_RightControllerTransformReference;

        /// <summary>
        /// Internal references to the Local Player Transforms.
        /// </summary>
        protected Transform m_LeftHandOrigin, m_RightHandOrigin;

        // Hand-tracking subsystem
        List<XRHandSubsystem> m_HandSubsystems = new List<XRHandSubsystem>();
        XRHandSubsystem m_HandSubsystem;

        private void Awake()
        {
            // Initialize hand-tracking subsystem
            SubsystemManager.GetSubsystems(m_HandSubsystems);
            if (m_HandSubsystems.Count > 0)
            {
                m_HandSubsystem = m_HandSubsystems[0];
            }
            else
            {
                // This is expected in Editor or when hand tracking is not available
                Debug.LogWarning("No XRHandSubsystem found; hand tracking features will be disabled. This is normal in Unity Editor.");
            }

            m_FingerRotationsLeft = new NetworkList<Vector3>(new List<Vector3>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
            m_FingerRotationsRight = new NetworkList<Vector3>(new List<Vector3>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

            m_FingerCurlLeft = new NetworkList<float>(new List<float>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
            m_FingerCurlRight = new NetworkList<float>(new List<float>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

            for (int i = 0; i < m_HandCurler.Length; i++)
            {
                m_handTrackedStartPose[i].position = m_HandCurler[i].transform.localPosition;
                m_handTrackedStartPose[i].rotation = m_HandCurler[i].transform.localRotation;
            }
        }

        private void OnEnable()
        {
            m_TrackingType.OnValueChanged += UpdateTrackingType;
        }

        private void OnDisable()
        {
            m_TrackingType.OnValueChanged -= UpdateTrackingType;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (IsOwner && m_XRModalityManager != null)
            {
                m_XRModalityManager.trackedHandModeStarted.RemoveListener(SwapToHands);
                m_XRModalityManager.motionControllerModeStarted.RemoveListener(SwapToControllers);
            }
        }

        private void Update()
        {
            if (!m_IsInitialized.Value || !NetworkManager.IsConnectedClient || NetworkManager.ShutdownInProgress) return;

            // If no XRInputModalityManager, try to detect hand tracking manually
            if (IsOwner && m_XRModalityManager == null && m_HandSubsystem != null)
            {
                CheckHandTrackingStatus();
            }

            if (trackingType == XRInputModalityManager.InputMode.TrackedHand)
            {
                switch (m_FidelityLevel)
                {
                    case 0:
                        SyncAllFingerData();
                        break;
                    case 1:
                        SyncFingerCurl();
                        break;
                    case 2:
                        SyncFingerCurlLimited();
                        break;
                }
            }
            else
            {
                SyncControllerTracking();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                // Find XRInputModalityManager if it exists
                m_XRModalityManager = FindFirstObjectByType<XRInputModalityManager>();
                if (m_XRModalityManager != null)
                {
                    m_XRModalityManager.trackedHandModeStarted.AddListener(SwapToHands);
                    m_XRModalityManager.motionControllerModeStarted.AddListener(SwapToControllers);
                    Utils.Log("[XRHandPoseReplicator] Found XRInputModalityManager - hand/controller switching enabled");
                }
                else
                {
                    Utils.LogWarning("[XRHandPoseReplicator] XRInputModalityManager not found - hand/controller auto-switching disabled. " +
                                   "Add XRInputModalityManager component to XR Origin to enable auto-switching.");
                }
                
                // Locate the rig instance by tag and find hand anchors under it
                var rigGO = GameObject.FindWithTag("PlayerRig");
                if (rigGO == null)
                {
                    Debug.LogError("Could not find GameObject tagged 'PlayerRig'.");
                    return;
                }
                
                // Find tracking space
                var trackingSpace = rigGO.transform.Find("TrackingSpace");
                if (trackingSpace == null)
                {
                    Debug.LogError("TrackingSpace not found under PlayerRig.");
                    return;
                }
                
                // Find hand anchors
                m_LeftHandTransformReference = trackingSpace.Find("LeftHandAnchor");
                m_RightHandTransformReference = trackingSpace.Find("RightHandAnchor");
                
                // Find controller anchors (they might be the same as hand anchors on Quest)
                m_LeftControllerTransformReference = m_LeftHandTransformReference;
                m_RightControllerTransformReference = m_RightHandTransformReference;
                
                if (m_LeftHandTransformReference == null || m_RightHandTransformReference == null)
                {
                    Debug.LogError("LeftHandAnchor or RightHandAnchor not found under TrackingSpace.");
                    return;
                }
                
                // Set initial hand origins
                // On Quest, hand and controller use the same anchors
                m_LeftHandOrigin = m_LeftHandTransformReference;
                m_RightHandOrigin = m_RightHandTransformReference;
                
                XRINetworkPlayer.LocalPlayer.SetHandOrigins(m_LeftHandOrigin, m_RightHandOrigin);
                // 手指引用部分保持不变
                SetupLocalFingerReferences();
            }
            else
            {
                ChangeControllerType(m_TrackingType.Value);
            }
        }

        void UpdateTrackingType(XRInputModalityManager.InputMode old, XRInputModalityManager.InputMode current)
        {
            ChangeControllerType(current);
        }

        void SetupLocalFingerReferences()
        {
            if (m_HandSubsystem == null)
            {
                Debug.LogWarning("No XRHandSubsystem available for finger tracking. This is normal in Unity Editor.");
                InitializeEmptyFingerData();
                m_IsInitialized.Value = true;
                return;
            }
            
            // Clear previous data
            m_FingerRotationsLeft.Clear();
            m_FingerRotationsRight.Clear();
            m_FingerCurlLeft.Clear();
            m_FingerCurlRight.Clear();

            // Iterate through all hand joints by ID
            int jointCount = (int)XRHandJointID.EndMarker;
            for (int i = 0; i < jointCount; ++i)
            {
                XRHandJointID jointId = (XRHandJointID)i;

                if (m_HandSubsystem != null)
                {
                    // Left hand joint
                    var leftJoint = m_HandSubsystem.leftHand.GetJoint(jointId);
                    if (leftJoint.TryGetPose(out Pose leftPose))
                        m_FingerRotationsLeft.Add(leftPose.rotation.eulerAngles);
                    else
                        m_FingerRotationsLeft.Add(Vector3.zero);

                    // Right hand joint
                    var rightJoint = m_HandSubsystem.rightHand.GetJoint(jointId);
                    if (rightJoint.TryGetPose(out Pose rightPose))
                        m_FingerRotationsRight.Add(rightPose.rotation.eulerAngles);
                    else
                        m_FingerRotationsRight.Add(Vector3.zero);
                }
                else
                {
                    // No hand subsystem, use default values
                    m_FingerRotationsLeft.Add(Vector3.zero);
                    m_FingerRotationsRight.Add(Vector3.zero);
                }

                // Initialize curl values
                m_FingerCurlLeft.Add(0f);
                m_FingerCurlRight.Add(0f);
            }

            m_IsInitialized.Value = true;
        }

        void InitializeEmptyFingerData()
        {
            // Initialize empty data for Unity Editor or when hand tracking is not available
            m_FingerRotationsLeft.Clear();
            m_FingerRotationsRight.Clear();
            m_FingerCurlLeft.Clear();
            m_FingerCurlRight.Clear();

            // Add default values for controller-based finger tracking (5 fingers per hand)
            for (int i = 0; i < 5; i++)
            {
                m_FingerCurlLeft.Add(0f);
                m_FingerCurlRight.Add(0f);
            }

            // Add empty rotation data for all joints
            int jointCount = (int)XRHandJointID.EndMarker;
            for (int i = 0; i < jointCount; i++)
            {
                m_FingerRotationsLeft.Add(Vector3.zero);
                m_FingerRotationsRight.Add(Vector3.zero);
            }
        }

        public void ChangeControllerType(XRInputModalityManager.InputMode inputMode)
        {
            if (inputMode == XRInputModalityManager.InputMode.MotionController)
            {
                SetFidelity(2);
                SetHandsToControllerOffset();
            }
            else
            {
                SetFidelity(m_FidelityLevel);
                ResetHandsToStart();
            }
        }

        void SetFidelity(int fidelity)
        {
            fidelity = Mathf.Clamp(fidelity, 0, 2);

            m_HandCurler[0].fidelityLevel = fidelity;
            m_HandCurler[1].fidelityLevel = fidelity;

            m_HandCurler[0].useCurl = fidelity > 0;
            m_HandCurler[1].useCurl = fidelity > 0;
        }

        void SyncAllFingerData()
        {
            if (IsOwner)
            {
                SetNetworkFingerRotations();

                if (m_UpdateHandsLocally)
                {
                    GetNetworkFingerRotations();
                }
            }
            else
            {
                GetNetworkFingerRotations();
            }
        }

        void SetNetworkFingerRotations()
        {
            if (m_HandSubsystem == null) return;

            // Update all joint rotations from XRHandSubsystem
            int jointCount = (int)XRHandJointID.EndMarker;
            for (int i = 0; i < jointCount && i < m_FingerRotationsLeft.Count; ++i)
            {
                XRHandJointID jointId = (XRHandJointID)i;

                // Left hand
                var leftJoint = m_HandSubsystem.leftHand.GetJoint(jointId);
                if (leftJoint.TryGetPose(out Pose leftPose))
                    m_FingerRotationsLeft[i] = leftPose.rotation.eulerAngles;

                // Right hand
                var rightJoint = m_HandSubsystem.rightHand.GetJoint(jointId);
                if (rightJoint.TryGetPose(out Pose rightPose))
                    m_FingerRotationsRight[i] = rightPose.rotation.eulerAngles;
            }
        }

        void GetNetworkFingerRotations()
        {
            int currentIdx = 0;
            int hand = 0;
            for (int i = 0; i < m_HandCurler[hand].handFidelityOptions[0].fingerJoints.Length; i++)
            {
                for (int j = 0; j < m_HandCurler[hand].handFidelityOptions[0].fingerJoints[i].jointTransformReferences.Count; j++)
                {
                    m_HandCurler[hand].handFidelityOptions[0].fingerJoints[i].jointTransformReferences[j].jointTransform.rotation =
                        Quaternion.Slerp(m_HandCurler[hand].handFidelityOptions[0].fingerJoints[i].jointTransformReferences[j].jointTransform.rotation,
                        Quaternion.Euler(m_FingerRotationsLeft[currentIdx++]),
                        Time.deltaTime * m_FingerLerpSpeed);
                }
            }

            currentIdx = 0;
            hand = 1;
            for (int i = 0; i < m_HandCurler[hand].handFidelityOptions[0].fingerJoints.Length; i++)
            {
                for (int j = 0; j < m_HandCurler[hand].handFidelityOptions[0].fingerJoints[i].jointTransformReferences.Count; j++)
                {
                    m_HandCurler[hand].handFidelityOptions[0].fingerJoints[i].jointTransformReferences[j].jointTransform.rotation =
                       Quaternion.Slerp(m_HandCurler[hand].handFidelityOptions[0].fingerJoints[i].jointTransformReferences[j].jointTransform.rotation,
                       Quaternion.Euler(m_FingerRotationsRight[currentIdx++]),
                       Time.deltaTime * m_FingerLerpSpeed);
                }
            }
        }

        void SyncFingerCurl()
        {
            if (IsOwner)
            {
                SetNetworkCurl();

                if (m_UpdateHandsLocally)
                {
                    GetNetworkCurl();
                }
            }
            else
            {
                GetNetworkCurl();
            }
        }

        void SetNetworkCurl()
        {
            // Calculate curl for 5 fingers (thumb, index, middle, ring, little)
            for (int i = 0; i < 5; i++)
            {
                SetLocalNetworkFingerCurl(0, i, GetAverageFingerCurl(0, i));
                SetLocalNetworkFingerCurl(1, i, GetAverageFingerCurl(1, i));
            }
        }

        void GetNetworkCurl()
        {
            for (int i = 0; i < m_HandCurler[0].handFidelityOptions[0].fingerJoints.Length; i++)
            {
                m_HandCurler[0].SetCurl(i, m_FingerCurlLeft[i]);
            }

            for (int i = 0; i < m_HandCurler[1].handFidelityOptions[0].fingerJoints.Length; i++)
            {
                m_HandCurler[1].SetCurl(i, m_FingerCurlRight[i]);
            }
        }


        float GetAverageFingerCurl(int hand, int finger)
        {
            if (m_HandSubsystem == null) return 0f;

            // Map finger index to XRHandJointID ranges
            // Thumb: 2-5, Index: 6-10, Middle: 11-15, Ring: 16-20, Little: 21-25
            int startJoint = 2 + (finger * 5);
            int endJoint = startJoint + 4;
            
            // For thumb, we have one less joint
            if (finger == 0)
            {
                startJoint = 2;
                endJoint = 5;
            }

            float totalCurl = 0f;
            int validJoints = 0;

            var handToCheck = (hand == 0) ? m_HandSubsystem.leftHand : m_HandSubsystem.rightHand;

            for (int i = startJoint + 1; i <= endJoint; i++) // Skip the metacarpal
            {
                var joint = handToCheck.GetJoint((XRHandJointID)i);
                if (joint.TryGetPose(out Pose jointPose))
                {
                    // Get the local rotation and extract the curl angle
                    float curlAngle = jointPose.rotation.eulerAngles.x;
                    if (curlAngle > 180f) curlAngle -= 360f;
                    curlAngle = Mathf.Clamp(curlAngle, 0f, 90f);
                    
                    totalCurl += curlAngle;
                    validJoints++;
                }
            }

            return validJoints > 0 ? (totalCurl / validJoints) / 90f : 0f; // Normalize to 0-1
        }

        void SyncFingerCurlLimited()
        {
            if (IsOwner)
            {
                SetNetworkCurlLimited();

                if (m_UpdateHandsLocally)
                {
                    GetNetworkCurlLimited();
                }
            }
            else
            {
                GetNetworkCurlLimited();
            }
        }

        void SetNetworkCurlLimited()
        {
            // Set thumb and index finger individually
            for (int i = 0; i < 2; i++)
            {
                SetLocalNetworkFingerCurl(0, i, GetAverageFingerCurl(0, i));
                SetLocalNetworkFingerCurl(1, i, GetAverageFingerCurl(1, i));
            }

            // Set combined curl for middle, ring, and little fingers
            SetLocalNetworkFingerCurl(0, 2, GetAverageCombinedFingersCurl(0));
            SetLocalNetworkFingerCurl(1, 2, GetAverageCombinedFingersCurl(1));
        }

        void GetNetworkCurlLimited()
        {
            for (int i = 0; i < 3; i++)
            {
                m_HandCurler[0].SetCurl(i, m_FingerCurlLeft[i]);
            }

            for (int i = 0; i < 3; i++)
            {
                m_HandCurler[1].SetCurl(i, m_FingerCurlRight[i]);
            }
        }

        float GetAverageCombinedFingersCurl(int hand)
        {
            if (m_HandSubsystem == null) return 0f;

            float totalCurl = 0f;
            int validFingers = 0;

            // Average the curl of middle (2), ring (3), and little (4) fingers
            for (int finger = 2; finger < 5; finger++)
            {
                float curl = GetAverageFingerCurl(hand, finger);
                if (curl > 0f)
                {
                    totalCurl += curl;
                    validFingers++;
                }
            }

            return validFingers > 0 ? totalCurl / validFingers : 0f;
        }

        void SyncControllerTracking()
        {
            //TODO: Sync Controller Input and map to hand poses
            if (IsOwner)
            {
                SetNetworkControllerFingerSync();
                if (m_UpdateHandsLocally)
                {
                    GetNetworkedControllerFingerSync();
                }
            }
            else
            {
                GetNetworkedControllerFingerSync();
            }
        }

        void SetNetworkControllerFingerSync()
        {
            SetLocalNetworkFingerCurl(0, 0, m_ThumbTouchProperties[0].action?.ReadValue<float>() ?? 0.0f);
            SetLocalNetworkFingerCurl(0, 1, m_TriggerInputProperties[0].action?.ReadValue<float>() ?? 0.0f);
            SetLocalNetworkFingerCurl(0, 2, m_GripInputProperties[0].action?.ReadValue<float>() ?? 0.0f);

            SetLocalNetworkFingerCurl(1, 0, m_ThumbTouchProperties[1].action?.ReadValue<float>() ?? 0.0f);
            SetLocalNetworkFingerCurl(1, 1, m_TriggerInputProperties[1].action?.ReadValue<float>() ?? 0.0f);
            SetLocalNetworkFingerCurl(1, 2, m_GripInputProperties[1].action?.ReadValue<float>() ?? 0.0f);
        }

        void SetLocalNetworkFingerCurl(int hand, int finger, float value)
        {
            if (hand == 0)
            {
                if (finger < m_FingerCurlLeft.Count && Mathf.Abs(m_FingerCurlLeft[finger] - value) > m_MinUpdateDelta)
                    m_FingerCurlLeft[finger] = value;
            }
            else
            {
                if (finger < m_FingerCurlRight.Count && Mathf.Abs(m_FingerCurlRight[finger] - value) > m_MinUpdateDelta)
                    m_FingerCurlRight[finger] = value;
            }
        }

        void GetNetworkedControllerFingerSync()
        {
            GetNetworkCurlLimited();
        }

        void CheckHandTrackingStatus()
        {
            // Check if hands are being tracked
            bool leftHandTracked = m_HandSubsystem.leftHand.isTracked;
            bool rightHandTracked = m_HandSubsystem.rightHand.isTracked;
            
            // Check if controllers are being tracked
            bool controllersActive = false;
            var devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand, devices);
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, devices);
            
            foreach (var device in devices)
            {
                if (device.isValid && !device.name.ToLower().Contains("hand"))
                {
                    controllersActive = true;
                    break;
                }
            }
            
            // Switch modes based on what's tracked
            if ((leftHandTracked || rightHandTracked) && !controllersActive)
            {
                if (trackingType != XRInputModalityManager.InputMode.TrackedHand)
                {
                    Utils.Log("[XRHandPoseReplicator] Switching to hand tracking mode");
                    SwapToHands();
                }
            }
            else if (controllersActive && !leftHandTracked && !rightHandTracked)
            {
                if (trackingType != XRInputModalityManager.InputMode.MotionController)
                {
                    Utils.Log("[XRHandPoseReplicator] Switching to controller mode");
                    SwapToControllers();
                }
            }
        }

        void SwapToHands()
        {
            SetTrackingType(XRInputModalityManager.InputMode.TrackedHand);
        }

        void SwapToControllers()
        {
            SetTrackingType(XRInputModalityManager.InputMode.MotionController);
        }

        public void SetTrackingType(XRInputModalityManager.InputMode trackingType)
        {
            m_TrackingType.Value = trackingType;
            if (trackingType == XRInputModalityManager.InputMode.MotionController)
            {
                m_LeftHandOrigin = m_LeftControllerTransformReference;
                m_RightHandOrigin = m_RightControllerTransformReference;
            }
            else
            {
                m_LeftHandOrigin = m_LeftHandTransformReference;
                m_RightHandOrigin = m_RightHandTransformReference;
            }

            XRINetworkPlayer.LocalPlayer.SetHandOrigins(m_LeftHandOrigin, m_RightHandOrigin);
        }

        void ResetHandsToStart()
        {
            for (int i = 0; i < m_HandCurler.Length; i++)
            {
                m_HandCurler[i].transform.localPosition = m_handTrackedStartPose[i].position;
                m_HandCurler[i].transform.localRotation = m_handTrackedStartPose[i].rotation;
            }
        }

        void SetHandsToControllerOffset()
        {
            for (int i = 0; i < m_HandCurler.Length; i++)
            {
                m_HandCurler[i].transform.localPosition = m_HandControllerOffsets[i];
                m_HandCurler[i].transform.localRotation = Quaternion.Euler(m_HandControllerEulerOffsets[i]);
            }
        }
    }
}
