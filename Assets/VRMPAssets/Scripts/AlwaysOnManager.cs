using UnityEngine;

namespace XRMultiplayer
{
    // Keeps the Quest display and proximity sensor awake for installation use (~8h sessions).
    // Sends the prox_close broadcast (software equivalent of taping the proximity sensor)
    // and sets the screen to never sleep via Unity's built-in API.
    public class AlwaysOnManager : MonoBehaviour
    {
        void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity");
                var intent = new AndroidJavaObject("android.content.Intent",
                    "com.oculus.vrpowermanager.prox_close");
                activity.Call("sendBroadcast", intent);
                Debug.Log("[AlwaysOn] prox_close broadcast sent.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AlwaysOn] prox_close broadcast failed: {e.Message}");
            }
#endif
        }
    }
}
