using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace PV.Multiplayer
{
    /// <summary>
    /// Represents a UI item for a multiplayer room entry.
    /// </summary>
    public class RoomItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _playerRatio;
        [SerializeField] private Button _button;

        /// <summary>
        /// Initializes the room item with room details and a join button event.
        /// </summary>
        /// <param name="roomName">The name of the room.</param>
        /// <param name="playerRatio">The current player count ratio.</param>
        /// <param name="onClickHandler">The event handler for joining the room.</param>
        public void InitItem(string roomName, string playerRatio, UnityAction onClickHandler)
        {
            _name.text = roomName;
            _playerRatio.text = playerRatio;
            _button.onClick.AddListener(onClickHandler);
        }

        /// <summary>
        /// Updates the displayed player ratio.
        /// </summary>
        /// <param name="playerRatio">The updated player ratio.</param>
        public void SetPlayerRatio(string playerRatio) => _playerRatio.text = playerRatio;

        public void Enable() => _button.interactable = true;
        
        public void Disable() => _button.interactable = false;
    }
}
