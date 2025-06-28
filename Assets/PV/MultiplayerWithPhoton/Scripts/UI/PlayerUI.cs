using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace PV.Multiplayer
{
    public class PlayerUI : MonoBehaviour
    {
        [Tooltip("UI slider to visually display the player's health.")]
        [SerializeField] private Slider healthSlider;

        [Tooltip("Text element to display the player's health as a numeric value.")]
        [SerializeField] private TextMeshProUGUI healthText;

        [Tooltip("The reticle GameObject for aiming visuals.")]
        [SerializeField] private GameObject reticle;
        
        // Delay duration for enabling/disabling the reticle.
        private readonly WaitForSeconds waitSeconds = new(0.15f);

        /// <summary>
        /// Updates the health UI elements to reflect the current health value.
        /// </summary>
        /// <param name="health">The current health value of the player.</param>
        public void SetHealth(int health)
        {
            healthSlider.value = health;
            healthText.text = health.ToString();
        }

        /// <summary>
        /// Enables or disables the reticle after a slight delay.
        /// </summary>
        /// <param name="enable">True to enable the reticle, false to disable it.</param>
        public void EnableReticle(bool enable)
        {
            StartCoroutine(SetReticle(enable));
        }

        IEnumerator SetReticle(bool enable)
        {
            yield return waitSeconds;
            reticle.SetActive(enable);
        }
    }
}
