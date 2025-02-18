using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer
{
    /// <summary>
    /// A simple example of how to setup a player appearance menu,
    /// utilize the bindable variables, AND use PlayerPrefs for persistence.
    /// </summary>
    public class PlayerAppearanceMenu : MonoBehaviour
    {
        [Header("Color Setup")]
        [SerializeField] Color[] m_PlayerColors;
        [SerializeField] Image m_PlayerIconColor;

        [Header("Name Field")]
        [SerializeField] TMP_InputField m_PlayerNameInputField;

        // PlayerPrefs keys
        private const string USER_NAME_KEY = "UserName";
        void Awake()
        {
            XRINetworkGameManager.LocalPlayerName.Value = PlayerPrefs.GetString(USER_NAME_KEY, "Player");

            // Loading the saved color 
            float r = PlayerPrefs.GetFloat("UserColor_R", 1f);
            float g = PlayerPrefs.GetFloat("UserColor_G", 1f);
            float b = PlayerPrefs.GetFloat("UserColor_B", 1f);
            float a = PlayerPrefs.GetFloat("UserColor_A", 1f);
            Color savedColor = new Color(r, g, b, a);
            XRINetworkGameManager.LocalPlayerColor.Value = savedColor;

            XRINetworkGameManager.LocalPlayerName.Subscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Subscribe(SetPlayerColor);
        }

        void Start()
        {
            SetPlayerColor(XRINetworkGameManager.LocalPlayerColor.Value);
            SetPlayerName(XRINetworkGameManager.LocalPlayerName.Value);
        }

        void OnDestroy()
        {
            XRINetworkGameManager.LocalPlayerName.Unsubscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(SetPlayerColor);
        }

        /// <summary>
        /// Called by a Unity UI InputField (OnEndEdit) or a button that passes the field's text.
        /// This sets the player's name network-wide AND saves it in PlayerPrefs.
        /// </summary>
        public void SubmitNewPlayerName(string text)
        {

            XRINetworkGameManager.LocalPlayerName.Value = text;
            PlayerPrefs.SetString(USER_NAME_KEY, text);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Use this to set the player's color so it triggers the bindable variable
        /// </summary>
        /// <param name="text"></param>
        public void SetRandomColor()
        {
            List<Color> availableColors = new(m_PlayerColors);
            if (availableColors.Remove(XRINetworkGameManager.LocalPlayerColor.Value))
            {
                XRINetworkGameManager.LocalPlayerColor.Value = availableColors[Random.Range(0, availableColors.Count)];
            }
            else
            {
                XRINetworkGameManager.LocalPlayerColor.Value = m_PlayerColors[Random.Range(0, m_PlayerColors.Length)];
            }
            // Code for saving the new color
            Color newColor = XRINetworkGameManager.LocalPlayerColor.Value;
            PlayerPrefs.SetFloat("UserColor_R", newColor.r);
            PlayerPrefs.SetFloat("UserColor_G", newColor.g);
            PlayerPrefs.SetFloat("UserColor_B", newColor.b);
            PlayerPrefs.SetFloat("UserColor_A", newColor.a);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Gets called automatically whenever LocalPlayerName changes.
        /// Updates the TMP_InputField to show the current name.
        /// </summary>
        void SetPlayerName(string newName)
        {
            m_PlayerNameInputField.text = newName;
        }

        /// <summary>
        /// Gets called automatically whenever LocalPlayerColor changes.
        /// Updates the icon color in your UI.
        /// </summary>
        void SetPlayerColor(Color color)
        {
            m_PlayerIconColor.color = color;
        }
    }
}

