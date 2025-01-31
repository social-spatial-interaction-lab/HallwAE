using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace XRMultiplayer
{
    public class CharacterHeightResetter : MonoBehaviour
    {
        [SerializeField] Vector2 m_MinMaxHeight = new Vector2(1.0f, 2.0f);
        TeleportationProvider m_TeleportationProvider;

        private void Start()
        {
            m_TeleportationProvider = GetComponentInChildren<TeleportationProvider>();
        }

        void Update()
        {
            float currentHeight = transform.position.y;
            if (currentHeight < m_MinMaxHeight.x || currentHeight > m_MinMaxHeight.y)
            {
                ResetHeight();
            }
        }

        void ResetHeight()
        {
            Vector3 newPosition = transform.position;  // Get current position
            newPosition.y = Mathf.Clamp(newPosition.y, m_MinMaxHeight.x, m_MinMaxHeight.y);        // Only modify Y value (height)

            TeleportRequest teleportRequest = new()
            {
                destinationPosition = newPosition,
                destinationRotation = transform.rotation  // Keep current rotation
            };

            if (!m_TeleportationProvider.QueueTeleportRequest(teleportRequest))
            {
                Utils.LogWarning("Failed to queue teleport request");
            }
        }
    }
} 