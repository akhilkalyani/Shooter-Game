using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PV.Multiplayer
{
    /// <summary>
    /// Manages the multiplayer game UI, including logging events, displaying timers, showing leaderboard stats, 
    /// and synchronizing player stats across all clients using Photon callbacks.
    /// </summary>
    public class GameUIManager : MonoBehaviourPunCallbacks
    {
        public static GameUIManager Instance { get; private set; }

        [Header("Logging")]
        [Tooltip("Duration each log message stays visible.")]
        public float logDuration = 3;

        [Tooltip("Maximum number of visible log messages at a time.")]
        public int numberOfVisibleLogs = 4;

        [Tooltip("The log text UI element.")]
        public TextMeshProUGUI logText;

        [Header("Leaderboard Stats")]
        public GameObject leaderboardUI;
        [Tooltip("Button to close the leaderboard.")]
        public GameObject closeButton;
        public GameObject leavingMessage;
        [Tooltip("Prefab of ui representation of player's stats.")]
        public GameObject uiStatPrefab;
        [Tooltip("Container object of UI stats.")]
        public Transform uiStatContainer;

        [Header("Timer")]
        [Tooltip("The Time (in minutes) after the game will be over.")]
        public float gameOverTime = 5;
        [Tooltip("The Time (in seconds) till this room will be active after game over.")]
        public float exitTime = 5;
        [Tooltip("Displays the game timer countdown.")]
        public TextMeshProUGUI gameTimer;
        [Tooltip("Displays the exit timer countdown.")]
        public TextMeshProUGUI exitTimer;

        // Queue to manage log messages in the UI.
        private Queue<string> _logs = new();
        // Stores all the stats ui data mapped to player id.
        private Dictionary<int, UIStat> _stats;

        // Cached UIStat instance.
        private UIStat _uiStat;

        // Flag to check if the player is leaving the room.
        private bool _isLeaving = false;
        // Flag to check if the game is over.
        private bool _isGameOver = false;

        // Used to update the timer.
        private float _timeRemaining = 5;

        // Key for game time property of room.
        private const string GAME_TIME = "GameTime";

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _stats = new();
            // Hide leaderboard UI and leaving message at start
            leaderboardUI.SetActive(false);
            leavingMessage.SetActive(false);

            // Check if game time is set in room properties
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(GAME_TIME))
            {
                int time = (int)PhotonNetwork.CurrentRoom.CustomProperties[GAME_TIME];
                gameOverTime = time < 1 || time > 15 ? gameOverTime : time;
            }

            // Converting minutes to seconds for timer.
            _timeRemaining = gameOverTime * 60;
        }

        private void Update()
        {
            if (_isLeaving)
            {
                return;
            }

            _timeRemaining -= Time.deltaTime;

            if (_isGameOver)
            {
                exitTimer.text = Mathf.CeilToInt(_timeRemaining).ToString();
            }
            else
            {
                gameTimer.text = $"{Mathf.FloorToInt(_timeRemaining / 60):d2}:{Mathf.CeilToInt(_timeRemaining % 60):d2}";
            }

            // Handle game over and exit countdown
            if (_timeRemaining < 0)
            {
                if (_isGameOver)
                {
                    _isLeaving = true;
                    PhotonNetwork.LeaveRoom(false);
                }
                else
                {
                    _isGameOver = true;
                    _timeRemaining = exitTime;

                    gameTimer.text = "00:00";
                    closeButton.SetActive(false);
                    leavingMessage.SetActive(true);
                    leaderboardUI.SetActive(true);
                }
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            // Logs the event to the UI about the player who left.
            LogLeft(otherPlayer.NickName);
        }

        public override void OnLeftRoom()
        {
            // Return to the main menu when the local player leaves the room.
            SceneManager.LoadScene(0);
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            // When Master leaves all players also leaves.
            if (!_isLeaving)
            {
                _isLeaving = true;
                // Initiate leaving the room.
                PhotonNetwork.LeaveRoom(false);
            }
        }

        public void LeaveRoom()
        {
            if (!_isLeaving)
            {
                _isLeaving = true;
                // Initiate leaving the room.
                PhotonNetwork.LeaveRoom(false);
            }
        }

        /// <summary>
        /// Logs a message indicating a player was killed by another player.
        /// </summary>
        /// <param name="attackerName">The name of the attacking player.</param>
        /// <param name="victimName">The name of the victim player.</param>
        public void LogKilled(string attackerName, string victimName)
        {
            // Ensure only the MasterClient broadcasts the log message to all players.
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(Log), RpcTarget.All, $"{attackerName} killed {victimName}.");
            }
        }

        /// <summary>
        /// Logs a message indicating a player has spawned.
        /// </summary>
        /// <param name="playerName">The name of the spawned player.</param>
        public void LogSpawned(string playerName)
        {
            // Ensure only the MasterClient broadcasts the log message to all players.
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(Log), RpcTarget.All, $"{playerName} spawned.");
            }
        }

        /// <summary>
        /// Logs a message indicating a player has left the game.
        /// </summary>
        /// <param name="playerName">The name of the player who left.</param>
        public void LogLeft(string playerName)
        {
            // Ensure only the MasterClient broadcasts the log message to all players.
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(Log), RpcTarget.All, $"{playerName} left.");
            }
        }

        /// <summary>
        /// Handles the creation and display of a log message in the UI.
        /// </summary>
        /// <param name="message">The message to display in the log.</param>
        [PunRPC]
        private void Log(string message)
        {
            if (logText != null)
            {
                _logs.Enqueue(message);
                UpdateLog(); // Update the displayed log.

                // Calculate the destroy delay based on the number of visible logs.
                float delay = logDuration;
                if (_logs.Count > numberOfVisibleLogs)
                {
                    int diff = Mathf.FloorToInt(_logs.Count / numberOfVisibleLogs) - 1;
                    delay = logDuration * (2 + diff);
                }

                // Start a coroutine to remove the log after the calculated delay.
                StartCoroutine(RemoveLog(delay));
            }
        }

        private void UpdateLog()
        {
            // Combine all log messages into a single string and update the UI.
            logText.text = string.Join("\n", _logs.ToArray());
        }

        private IEnumerator RemoveLog(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_logs.Count > 0)
            {
                // Remove the oldest log and refresh the displayed logs.
                _logs.Dequeue();
                UpdateLog();
            }
        }

        /// <summary>
        /// Assigns and initializes player stats.
        /// </summary>
        public void SetStats(PlayerController player)
        {
            if (!_stats.ContainsKey(player.photonView.Owner.ActorNumber))
            {
                _uiStat = Instantiate(uiStatPrefab, uiStatContainer).GetComponent<UIStat>();
                _stats[player.photonView.Owner.ActorNumber] = _uiStat;
                _stats[player.photonView.Owner.ActorNumber].InitData(player);
            }
        }

        /// <summary>
        /// Updates the stats for player with given player number or id.
        /// </summary>
        /// <param name="playerNumber">The Player's unique number.</param>
        public void UpdateStats(int playerNumber)
        {
            if (_stats.Count > 0)
            {
                if (_stats.ContainsKey(playerNumber))
                {
                    _stats[playerNumber].UpdateData();
                }
            }
        }
    }
}
