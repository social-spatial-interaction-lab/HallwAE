using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace XRMultiplayer
{
    public class CharacterHeightResetter : MonoBehaviour
    {
        [SerializeField] Vector2 m_MinMaxHeight = new Vector2(0.0f, 0.5f);
        // XR origin should be fixed to 0 to match with tracking origin mode==floor in AR
        [SerializeField] float m_ResetHeight = 0.0f;
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
            newPosition.y = m_ResetHeight;        // Only modify Y value (height)

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