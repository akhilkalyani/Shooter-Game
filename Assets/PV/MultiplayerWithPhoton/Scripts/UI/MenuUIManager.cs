using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace PV.Multiplayer
{
    /// <summary>
    /// Manages the UI for the main menu, profile, deathmatch, and room functionalities.
    /// Handles player connection, room creation, and player readiness.
    /// </summary>
    public class MenuUIManager : MonoBehaviour
    {
        public static MenuUIManager Instance;

        [Header("General")]
        public GameObject mainUI;
        public GameObject profileUI;
        public GameObject deathmatchUI;
        public GameObject roomUI;
        public GameObject settingsUI;
        public TextMeshProUGUI feedbackMessage;

        [Header("Profile")]
        public TMP_InputField playerNameField;

        [Header("Deathmatch")]
        public GameObject roomListPanel;
        public GameObject createRoomPanel;
        [Tooltip("Object containing the list of rooms.")]
        public GameObject roomList;
        public GameObject noRoomMessage;
        public GameObject roomItemPrefab;
        [Tooltip("Container to hold all room items.")]
        public Transform roomItemContainer;
        public TMP_InputField roomNameField;
        public TextMeshProUGUI gameTimeText;
        public TextMeshProUGUI maxPlayersText;
        public Slider gameTimeSlider;
        public Slider maxPlayersSlider;
        public Toggle lockCursor;

        [Header("Room")]
        [Tooltip("Container to hold all player items in the room.")]
        public Transform playerItemContainer;
        public GameObject playerItemPrefab;
        public Image readyButton;
        [Tooltip("Color of the ready button when the player is ready.")]
        public Color readyColor = Color.white;
        [Tooltip("Color of the ready button when the player is not ready.")]
        public Color notReadyColor = Color.white;

        // Dictionaries to store room information, UI items and player UI items.
        private Dictionary<string, RoomInfo> _roomInfos = new();
        private Dictionary<string, RoomItem> _roomItems = new();
        private Dictionary<int, PlayerItem> _playerItems = new();

        private int _localID = -1;
        private readonly float _toggleDelay = 1f; // Time to wait before toggling the ready status.
        private float _lastToggleTime = 0f; // The last time we toggle the ready status.

        private const string DEFAULT_NAME = "Noobie";
        private const string READY_KEY = "IsReady"; // Key for ready property of player.
        private const string GAME_TIME = "GameTime"; // Key for game time property of room.
        private const string LOCK_CURSOR = "LockCursor"; // Key for lock cursor during game.

        private Hashtable _playerProps; // Cached player custom properties.
        private readonly WaitForSeconds _waitForCountdown = new(1); // Delay for countdown.
        private bool _canLoadLevel = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Load player name from PlayerPrefs if it exists.
            if (PlayerPrefs.HasKey("PlayerName"))
            {
                playerNameField.text = PlayerPrefs.GetString("PlayerName");
            }

            // Initial feedback message
            feedbackMessage.text = "Connecting...";
            feedbackMessage.gameObject.SetActive(true);

            // Hide all UI panels initially
            mainUI.SetActive(false);
            profileUI.SetActive(false);
            deathmatchUI.SetActive(false);
            roomUI.SetActive(false);
            settingsUI.SetActive(false);

            // Set the initial color of the ready button
            readyButton.color = notReadyColor;

            // Handle Cursor's lock state.
            if (PlayerPrefs.HasKey(LOCK_CURSOR))
            {
                if (PlayerPrefs.GetInt(LOCK_CURSOR) == 1)
                {
                    lockCursor.isOn = true;
                    Cursor.lockState = CursorLockMode.Confined;
                }
                else
                {
                    lockCursor.isOn = false;
                }
            }
            else
            {
                lockCursor.isOn = true;
                Cursor.lockState = CursorLockMode.Confined;
                PlayerPrefs.SetInt(LOCK_CURSOR, 1);
            }

            CheckRoomList();
            SavePlayerName(); // Insures that there will be a player name.
        }

        /// <summary>
        /// Saves the player's name to Photon and PlayerPrefs.
        /// </summary>
        public void SavePlayerName()
        {
            if (!string.IsNullOrEmpty(playerNameField.text))
            {
                PhotonNetwork.NickName = playerNameField.text;
            }
            else
            {
                PhotonNetwork.NickName = DEFAULT_NAME;
                playerNameField.text = DEFAULT_NAME;
            }

            PlayerPrefs.SetString("PlayerName", PhotonNetwork.NickName);
        }

        /// <summary>
        /// Opens the profile UI to allow the player manage profile.
        /// </summary>
        public void OpenProfile()
        {
            mainUI.SetActive(false);
            profileUI.SetActive(true);
        }

        /// <summary>
        /// Closes the profile UI and returns to the main UI.
        /// </summary>
        public void CloseProfile()
        {
            profileUI.SetActive(false);
            mainUI.SetActive(true);
        }

        /// <summary>
        /// Opens the deathmatch UI where the player can interact with available rooms.
        /// </summary>
        public void OpenDeathmatch()
        {
            mainUI.SetActive(false);

            roomListPanel.SetActive(true);
            createRoomPanel.SetActive(false);
            deathmatchUI.SetActive(true);
        }

        /// <summary>
        /// Closes the deathmatch UI and returns to the main UI.
        /// </summary>
        public void CloseDeathmatch()
        {
            deathmatchUI.SetActive(false);
            roomListPanel.SetActive(true);
            createRoomPanel.SetActive(false);

            mainUI.SetActive(true);
        }

        /// <summary>
        /// Opens the settings UI to allow the player manage settings.
        /// </summary>
        public void OpenSettings()
        {
            mainUI.SetActive(false);
            settingsUI.SetActive(true);
        }

        /// <summary>
        /// Closes the settings UI and returns to the main UI.
        /// </summary>
        public void CloseSettings()
        {
            settingsUI.SetActive(false);
            mainUI.SetActive(true);
        }

        public void OnLockCursorChanged(bool lockCursor)
        {
            PlayerPrefs.SetInt(LOCK_CURSOR, lockCursor ? 1 : 0);

            Cursor.lockState = lockCursor ? CursorLockMode.Confined : CursorLockMode.None;
        }

        /// <summary>
        /// Creates a new room if the room name is valid.
        /// </summary>
        public void CreateRoom()
        {
            if (!string.IsNullOrEmpty(roomNameField.text))
            {
                // If this room already exist then return. Photon does not support identical rooms.
                if (_roomInfos.Count > 0 && _roomInfos.ContainsKey(roomNameField.text))
                {
                    return;
                }

                // Create the room.
                NetworkManager.Instance.CreateRoom(roomNameField.text);
            }
            else
            {
                Debug.LogError("Room creation failed! Room name empty!");
            }
        }

        /// <summary>
        /// Updates the room list UI with the latest room information.
        /// </summary>
        public void UpdateRoomList(List<RoomInfo> roomList)
        {
            for (int i = 0; i < roomList.Count; i++)
            {
                // If no cached rooms exist, add them.
                if (_roomInfos.Count <= 0)
                {
                    // If room is being removed then continue to next room.
                    if (roomList[i].RemovedFromList) continue;

                    AddRoom(roomList[i]);
                }
                else
                {
                    // Handle room removal or update.
                    if (roomList[i].RemovedFromList)
                    {
                        if (_roomInfos.ContainsKey(roomList[i].Name))
                        {
                            _roomInfos.Remove(roomList[i].Name);

                            _roomItems.Remove(roomList[i].Name, out RoomItem roomItem);
                            Destroy(roomItem.gameObject); // Destroy the room item UI
                        }
                    }
                    else if (!_roomInfos.ContainsKey(roomList[i].Name))
                    {
                        AddRoom(roomList[i]);
                    }
                    else if(_roomInfos.ContainsKey(roomList[i].Name))
                    {
                        // Update existing room info
                        _roomInfos[roomList[i].Name] = roomList[i];

                        // Disable the room item if full or closed, else enable it.
                        if (!roomList[i].IsOpen || roomList[i].PlayerCount >= roomList[i].MaxPlayers)
                        {
                            _roomItems[roomList[i].Name].Disable();
                        }
                        else
                        {
                            _roomItems[roomList[i].Name].Enable();
                        }
                        _roomItems[roomList[i].Name].SetPlayerRatio($"{roomList[i].PlayerCount} / {roomList[i].MaxPlayers}");
                    }
                }
            }

            CheckRoomList();
        }

        /// <summary>
        /// Checks if any rooms are available and shows the appropriate message.
        /// </summary>
        private void CheckRoomList()
        {
            bool hasRooms = _roomItems.Count > 0;
            roomList.SetActive(hasRooms);
            noRoomMessage.SetActive(!hasRooms);
        }

        /// <summary>
        /// Adds a new room to the room list UI.
        /// </summary>
        private void AddRoom(RoomInfo room)
        {
            RoomItem roomItem = Instantiate(roomItemPrefab, roomItemContainer).GetComponent<RoomItem>();
            roomItem.InitItem(room.Name, $"{room.PlayerCount} / {room.MaxPlayers}", () => JoinRoom(room.Name));
            if (!room.IsOpen || room.PlayerCount >= room.MaxPlayers)
            {
                roomItem.Disable();
            }

            _roomInfos[room.Name] = room;
            _roomItems[room.Name] = roomItem;
        }

        /// <summary>
        /// Joins the specified room if it's available and not full.
        /// </summary>
        private void JoinRoom(string roomName)
        {
            if (_roomInfos.ContainsKey(roomName))
            {
                if (!_roomInfos[roomName].IsOpen)
                {
                    Debug.Log("Game already started.");
                    // TODO: use UI element to show message to player.
                    return;
                }
                else if (_roomInfos[roomName].PlayerCount >= _roomInfos[roomName].MaxPlayers)
                {
                    Debug.Log("Room is full.");
                    // TODO: use UI element to show message to player.
                    return;
                }

                ShowFeedback("Joining room...");
                NetworkManager.Instance.JoinRoom(roomName);
            }
        }

        public void LeaveRoom()
        {
            if (!NetworkManager.Instance.isLeaving)
            {
                ShowFeedback("Leaving room...");
                NetworkManager.Instance.isLeaving = true;
                PhotonNetwork.LeaveRoom();
            }
        }

        /// <summary>
        /// Displays a feedback message while performing an action.
        /// </summary>
        public void ShowFeedback(string message)
        {
            mainUI.SetActive(false);
            profileUI.SetActive(false);
            deathmatchUI.SetActive(false);
            roomUI.SetActive(false);

            feedbackMessage.text = message;
            feedbackMessage.gameObject.SetActive(true);
        }

        /// <summary>
        /// Handles different actions based on various network events.
        /// </summary>
        public void OnNetworkEvent(NetworkEvent networkEvent)
        {
            switch (networkEvent)
            {
                case NetworkEvent.JoinedLobby: OnJoinedLobby();
                    break;
                case NetworkEvent.JoinedRoom: OnJoinedRoom();
                    break;
                case NetworkEvent.CreatedRoom: OnCreatedRoom();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Actions to take when the player joins a lobby.
        /// </summary>
        public void OnJoinedLobby()
        {
            feedbackMessage.gameObject.SetActive(false);
            mainUI.SetActive(true);

            // Reset player information if not the first time joining the lobby, e.g., When player leaves a room.
            if (_localID != -1)
            {
                _localID = -1;

                foreach (PlayerItem item in _playerItems.Values)
                {
                    Destroy(item.gameObject);
                }
                _playerItems.Clear();

                readyButton.color = notReadyColor;
                _playerProps[READY_KEY] = false;
                PhotonNetwork.LocalPlayer.SetCustomProperties(_playerProps);
            }

            // Set default max players property for any room.
            NetworkManager.Instance.SetMaxPlayers(4);
        }

        /// <summary>
        /// Actions to take when the player joins a room.
        /// </summary>
        public void OnJoinedRoom()
        {
            // Disable active UI elements.
            feedbackMessage.gameObject.SetActive(false);

            // Create all players list in current room.
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!_playerItems.ContainsKey(player.ActorNumber))
                {
                    CreatePlayerItem(player.ActorNumber);
                }
            }

            // Cache local player's ID and set initial properties.
            _localID = PhotonNetwork.LocalPlayer.ActorNumber;
            _playerProps = new() { { READY_KEY, false } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(_playerProps);

            if (PhotonNetwork.IsMasterClient)
            {
                // Store game time for current room.
                PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable() { { GAME_TIME, (int)gameTimeSlider.value } });
            }

            // Enable the room UI.
            roomUI.SetActive(true);
        }

        /// <summary>
        /// Handles actions when the room is created.
        /// </summary>
        public void OnCreatedRoom()
        {
            // Clear room info if the player create / host a room.
            foreach (var room in _roomItems.Values)
            {
                Destroy(room.gameObject);
            }

            _roomInfos.Clear();
            _roomItems.Clear();

            CheckRoomList();

            // Creation of player item UI in room
            _localID = PhotonNetwork.LocalPlayer.ActorNumber;
            PlayerItem playerItem = Instantiate(playerItemPrefab, playerItemContainer).GetComponent<PlayerItem>();
            playerItem.InitItem(PhotonNetwork.NickName);
            _playerItems[_localID] = playerItem;

            _playerProps = new() { { READY_KEY, false } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(_playerProps);
        }

        /// <summary>
        /// Handles actions when a player enters the room.
        /// </summary>
        public void OnPlayerEnter(int actorNumber)
        {
            if (!_playerItems.ContainsKey(actorNumber))
            {
                CreatePlayerItem(actorNumber);
            }
        }

        /// <summary>
        /// Handles actions when a player leaves the room.
        /// </summary>
        public void OnPlayerLeft(Player player)
        {
            if (_playerItems.ContainsKey(player.ActorNumber))
            {
                player.SetCustomProperties(new Hashtable());
                RemovePlayerItem(player.ActorNumber);
            }

            CheckAllPlayersReady();
        }

        /// <summary>
        /// Checks if all players in the room are ready. If all players are ready, the game can start.
        /// </summary>
        private void CheckAllPlayersReady()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.CustomProperties.ContainsKey(READY_KEY) && _playerItems.ContainsKey(p.ActorNumber))
                {
                    if (!(bool)p.CustomProperties[READY_KEY])
                    {
                        if (_canLoadLevel)
                        {
                            _canLoadLevel = false;
                            if (!roomUI.activeSelf)
                            {
                                // Disable feedback UI.
                                feedbackMessage.gameObject.SetActive(false);
                                // Enable Room UI.
                                roomUI.SetActive(true);
                            }
                        }
                        return;
                    }
                }
            }

            // Return if not enough players.
            if (_playerItems.Count < 2)
            {
                return;
            }

            // Return if the player is about to leave.
            if (NetworkManager.Instance.isLeaving)
            {
                return;
            }

            if (!_canLoadLevel)
            {
                StartCoroutine(LoadLevelDelay());
            }
            else if (PhotonNetwork.IsMasterClient)
            {
                StartGame();
            }
        }

        public void OnGameTimeChanged()
        {
            // Update the game time text when the slider value changes.
            gameTimeText.text = gameTimeSlider.value.ToString();
        }

        public void OnMaxPlayersChanged()
        {
            // Update the max player text when the slider value changes.
            maxPlayersText.text = maxPlayersSlider.value.ToString();
            NetworkManager.Instance.SetMaxPlayers((int)maxPlayersSlider.value);
        }

        /// <summary>
        /// Starts the game by locking the room and loading the game scene.
        /// </summary>
        private void StartGame()
        {
            // Close the room so no more players can join and load the game scene.
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.LoadLevel(1);
        }

        /// <summary>
        /// Coroutine to delay level loading and show a countdown.
        /// </summary>
        IEnumerator LoadLevelDelay()
        {
            ShowFeedback("Loading Level In 3...");
            yield return _waitForCountdown;

            ShowFeedback("Loading Level In 2...");
            yield return _waitForCountdown;

            ShowFeedback("Loading Level In 1...");
            yield return _waitForCountdown;

            ShowFeedback("Loading Level...");
            _canLoadLevel = true;
            // Ensure players are ready before loading
            CheckAllPlayersReady();
        }

        /// <summary>
        /// Handles updates to the player’s ready status when their properties change.
        /// </summary>
        public void OnPlayerPropsUpdate(int actorNumber, Hashtable props)
        {
            if (props.ContainsKey(READY_KEY))
            {
                if (_playerItems.ContainsKey(actorNumber))
                {
                    _playerItems[actorNumber].SetStatus((bool)props[READY_KEY]);
                }
                else
                {
                    CreatePlayerItem(actorNumber);
                }

                CheckAllPlayersReady();
            }
        }

        public void ToggleReadyStatus()
        {
            if (_localID == -1 || Time.time - _lastToggleTime < _toggleDelay)
            {
                return;
            }

            _lastToggleTime = Time.time;

            if (_playerProps.ContainsKey(READY_KEY))
            {
                SetLocalReadyStatus(!(bool)_playerProps[READY_KEY]);
            }
            else
            {
                SetLocalReadyStatus(false);
            }
        }

        /// <summary>
        /// Sets the local player's ready status and updates their UI.
        /// </summary>
        private void SetLocalReadyStatus(bool isReady)
        {
            _playerProps[READY_KEY] = isReady;
            PhotonNetwork.LocalPlayer.SetCustomProperties(_playerProps);

            readyButton.color = isReady ? readyColor : notReadyColor;
            _playerItems[_localID].SetStatus(isReady);
        }

        /// <summary>
        /// Creates a new UI element to display the player in the players list of current room.
        /// </summary>
        private void CreatePlayerItem(int actorNumber)
        {
            PlayerItem playerItem = Instantiate(playerItemPrefab, playerItemContainer).GetComponent<PlayerItem>();
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            playerItem.InitItem(player != null ? player.NickName : DEFAULT_NAME);

            if (player.CustomProperties.ContainsKey(READY_KEY))
            {
                playerItem.SetStatus((bool)player.CustomProperties[READY_KEY]);
            }

            _playerItems[actorNumber] = playerItem;
        }

        /// <summary>
        /// Removes the player’s UI item from the players list of current room.
        /// </summary>
        private void RemovePlayerItem(int actorNumber)
        {
            _playerItems.Remove(actorNumber, out PlayerItem item);
            Destroy(item.gameObject);
        }

        /// <summary>
        /// Handles errors by displaying the appropriate UI.
        /// </summary>
        public void OnError()
        {
            // Checking if gameobjects exist, since this function also gets called when disconnected.
            // Which could also be triggered on application close. In that case gameobjects do not exist anymore.
            if (feedbackMessage != null)
            {
                // Disable all UI elements and enable only main UI
                feedbackMessage.gameObject.SetActive(false);
                profileUI.SetActive(false);
                roomUI.SetActive(false);
                deathmatchUI.SetActive(false);

                mainUI.SetActive(true);
            }
        }

        /// <summary>
        /// Quits the application.
        /// </summary>
        public void Quit()
        {
            Application.Quit();
        }
    }
}