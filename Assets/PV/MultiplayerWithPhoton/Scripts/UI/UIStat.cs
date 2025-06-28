using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PV.Multiplayer
{
    /// <summary>
    /// Represents the UI element displaying player statistics.
    /// </summary>
    public class UIStat : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI playerName;
        [SerializeField] private TextMeshProUGUI kills;
        [SerializeField] private TextMeshProUGUI deaths;
        [SerializeField] private TextMeshProUGUI score;
        [SerializeField] private Image backgroundImage;

        [Header("Colors")]
        [Tooltip("Color for the local player's background.")]
        [SerializeField] private Color playerColor = Color.white;

        [SerializeField] private Color positiveScoreColor = Color.white;
        [SerializeField] private Color negativeScoreColor = Color.white;

        // Stores reference to the player's stats.
        private Stats _stats;

        [HideInInspector]
        // Stores player's identifier.
        public int playerNumber = -1;

        public void Enable()
        {
            gameObject.SetActive(true);
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Initializes the UI with the player's data.
        /// </summary>
        /// <param name="player">The player whose stats will be displayed.</param>
        public void InitData(PlayerController player)
        {
            _stats = player.stats;
            playerNumber = player.photonView.Owner.ActorNumber;
            playerName.text = player.photonView.Owner.NickName;
            kills.text = player.stats.Kills.ToString();
            deaths.text = player.stats.Deaths.ToString();
            score.text = player.stats.Score.ToString();

            // Change background color if the player is the local player.
            if (player.photonView.IsMine)
            {
                backgroundImage.color = playerColor;
            }
        }

        /// <summary>
        /// Updates the player's stats UI.
        /// </summary>
        public void UpdateData()
        {
            if (playerNumber == -1 || _stats == null)
            {
                return;
            }

            // Update kill and death counts.
            kills.text = _stats.Kills.ToString();
            deaths.text = _stats.Deaths.ToString();

            // Display score with appropriate color formatting.
            if (_stats.Score > 0)
            {
                score.text = $"+{_stats.Score}";
                score.color = positiveScoreColor;
            }
            else
            {
                score.text = _stats.Score.ToString();
                score.color = _stats.Score == 0 ? Color.black : negativeScoreColor;
            }
        }
    }
}
