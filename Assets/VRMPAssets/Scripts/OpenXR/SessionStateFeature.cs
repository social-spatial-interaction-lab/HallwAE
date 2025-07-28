using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace XRMultiplayer
{
    /// <summary>
    /// Custom OpenXR Feature to handle session state changes for proper app exit handling on Quest devices.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "XR Multiplayer Session State Handler",
                   BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
                   Company = "XRMultiplayer",
                   Desc = "Handles OpenXR session state changes for proper cleanup on app exit",
                   Version = "1.0.0",
                   FeatureId = featureId)]
#endif
    public class SessionStateFeature : OpenXRFeature
    {
        public const string featureId = "com.xrmultiplayer.openxr.feature.sessionstate";

        // XrSessionState values from OpenXR specification
        private const int XR_SESSION_STATE_IDLE = 1;
        private const int XR_SESSION_STATE_READY = 2;
        private const int XR_SESSION_STATE_SYNCHRONIZED = 3;
        private const int XR_SESSION_STATE_VISIBLE = 4;
        private const int XR_SESSION_STATE_FOCUSED = 5;
        private const int XR_SESSION_STATE_STOPPING = 6;
        private const int XR_SESSION_STATE_LOSS_PENDING = 7;
        private const int XR_SESSION_STATE_EXITING = 8;

        /// <summary>
        /// Called when the OpenXR session is exiting
        /// </summary>
        /// <param name="xrSession">Handle of the xrSession</param>
        protected override void OnSessionExiting(ulong xrSession)
        {
            base.OnSessionExiting(xrSession);
            
            Utils.Log($"OpenXR Session is exiting - user requested app closure (Session: {xrSession})");
            
            // Trigger cleanup for XRINetworkGameManager
            var gameManager = FindFirstObjectByType<XRINetworkGameManager>();
            if (gameManager != null)
            {
                gameManager.ShutDown();
            }
        }

        /// <summary>
        /// Called when the session is about to be lost
        /// </summary>
        /// <param name="xrSession">Handle of the xrSession</param>
        protected override void OnSessionLossPending(ulong xrSession)
        {
            base.OnSessionLossPending(xrSession);
            
            Utils.Log($"OpenXR Session loss pending - system is reclaiming resources (Session: {xrSession})");
            
            // Also trigger cleanup when session is about to be lost
            var gameManager = FindFirstObjectByType<XRINetworkGameManager>();
            if (gameManager != null)
            {
                gameManager.ShutDown();
            }
        }

        protected override void OnSessionStateChange(int oldState, int newState)
        {
            base.OnSessionStateChange(oldState, newState);

            // Log state changes for debugging
            Utils.Log($"OpenXR Session State Changed: {GetStateName(oldState)} -> {GetStateName(newState)}");
        }

        private string GetStateName(int state)
        {
            switch (state)
            {
                case XR_SESSION_STATE_IDLE: return "IDLE";
                case XR_SESSION_STATE_READY: return "READY";
                case XR_SESSION_STATE_SYNCHRONIZED: return "SYNCHRONIZED";
                case XR_SESSION_STATE_VISIBLE: return "VISIBLE";
                case XR_SESSION_STATE_FOCUSED: return "FOCUSED";
                case XR_SESSION_STATE_STOPPING: return "STOPPING";
                case XR_SESSION_STATE_LOSS_PENDING: return "LOSS_PENDING";
                case XR_SESSION_STATE_EXITING: return "EXITING";
                default: return $"UNKNOWN({state})";
            }
        }

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            Utils.Log("SessionStateFeature instance created");
            return base.OnInstanceCreate(xrInstance);
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            Utils.Log("SessionStateFeature instance destroyed");
            base.OnInstanceDestroy(xrInstance);
        }
    }
}