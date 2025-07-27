using System;
using UnityEngine;

namespace XRMultiplayer
{
    public class CharacterResetter : MonoBehaviour
    {
        [SerializeField] float m_ResetDistance = 15.0f;
        [SerializeField] Vector3 offlinePosition = new Vector3(0, .5f, -12.0f);
        [SerializeField] Vector3 onlinePosition = new Vector3(0, .15f, 0);
        Vector3 m_ResetPosition;
        Camera m_MainCamera;

        private void Start()
        {
            XRINetworkGameManager.Connected.Subscribe(UpdateResetPosition);
            // m_TeleportationProvider = GetComponentInChildren<TeleportationProvider>();
            m_MainCamera = FindCenterEyeCamera();
            if (m_MainCamera == null)
            {
                Utils.LogError("CenterEyeAnchor Camera with tag 'MainCamera' not found under XR Origin! CharacterResetter cannot function.");
            }
            m_ResetPosition = offlinePosition;
            ResetPlayer();
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

        void UpdateResetPosition(bool connected)
        {
            if (connected)
            {
                m_ResetPosition = onlinePosition;
            }
            else
            {
                m_ResetPosition = offlinePosition;
                ResetPlayer();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Mathf.Abs(transform.position.x) > m_ResetDistance || Mathf.Abs(transform.position.z) > m_ResetDistance)
            {
                ResetPlayer();
            }
        }

        public void ResetPlayer()
        {
            ResetPlayer(m_ResetPosition);
        }

        void ResetPlayer(Vector3 destination)
        {
            if (m_MainCamera == null)
            {
                Utils.LogError("Cannot reset player position, Main Camera reference is missing.");
                return;
            }

            Vector3 cameraLocalPosition = m_MainCamera.transform.localPosition;

            Vector3 targetOriginPosition = destination - cameraLocalPosition;

            transform.position = targetOriginPosition;
            transform.rotation = Quaternion.identity;

            Utils.Log($"Player position reset. Origin moved to {transform.position}.");
        }

        [ContextMenu("Set Player To Online Position")]
        void SetPlayerToOnlinePosition()
        {
            ResetPlayer(onlinePosition);
        }

        [ContextMenu("Set Player To Offline Position")]
        void SetPlayerToOfflinePosition()
        {
            ResetPlayer(offlinePosition);
        }
    }
}
