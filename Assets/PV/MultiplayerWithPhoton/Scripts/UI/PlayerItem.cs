using TMPro;
using UnityEngine;

namespace PV.Multiplayer
{
    /// <summary>
    /// Represents a UI item for a player entry in the current room.
    /// </summary>
    public class PlayerItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _status;

        /// <summary>
        /// Initializes the player's name and status.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        public void InitItem(string name)
        {
            _name.text = name;
            _status.text = "Not Ready";
        }

        /// <summary>
        /// Updates the ready status of the player.
        /// </summary>
        /// <param name="isReady">Is the player ready to start the game ?</param>
        public void SetStatus(bool isReady) => _status.text = isReady ? "Ready" : "Not Ready";
    }
}
