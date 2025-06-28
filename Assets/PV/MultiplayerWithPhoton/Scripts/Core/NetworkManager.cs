using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace PV.Multiplayer
{
    /// <summary>
    /// Enum to represent different network events.
    /// </summary>
    public enum NetworkEvent { JoinedLobby, JoinedRoom, CreatedRoom }

    /// <summary>
    /// NetworkManager class to handle network-related operations and Photon callbacks.
    /// </summary>
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        public static NetworkManager Instance;

        // Maximum players that can join a room.
        private int _maxPlayers = 4;

        [HideInInspector]
        public bool isLeaving = false; // Flag to track if the player is leaving the room.

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Check if the client is already connected to Photon.
            if (!PhotonNetwork.IsConnected)
            {
                // Notify listeners about the connection attempt.
                MenuUIManager.Instance.ShowFeedback("Connecting...");
                // Connect to Photon.
                PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.AutomaticallySyncScene = true;
            }
        }

        /// <summary>
        /// Sets the maximum players that can join a room.
        /// </summary>
        public void SetMaxPlayers(int maxPlayers)
        {
            _maxPlayers = maxPlayers;
        }

        /// <summary>
        /// Attempts to create and join a room with the given room name.
        /// </summary>
        /// <param name="roomName">The name of the room to create.</param>
        public void CreateRoom(string roomName)
        {
            // Notify listeners about room joining attempt.
            MenuUIManager.Instance.ShowFeedback($"Joining Room : {roomName}");
            var roomOptions = new RoomOptions()
            {
                MaxPlayers = _maxPlayers,
            };

            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, null);
        }

        /// <summary>
        /// Attempts to join an existing room with the specified room name.
        /// </summary>
        /// <param name="roomName">The name of the room to join.</param>
        public void JoinRoom(string roomName)
        {
            PhotonNetwork.JoinRoom(roomName);
        }

        public override void OnConnectedToMaster()
        {
            // Notify listeners about the connection attempt.
            MenuUIManager.Instance.ShowFeedback("Connecting to lobby...");
            // Join the lobby after connecting to the master server.
            PhotonNetwork.JoinLobby();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"Disconnected from server.\nCause : {cause}");
            // Notify listeners about the disconnection.
            MenuUIManager.Instance.OnError();
        }

        public override void OnJoinedLobby()
        {
            // Notify listeners about the successful lobby connection.
            MenuUIManager.Instance.OnNetworkEvent(NetworkEvent.JoinedLobby);
        }

        public override void OnJoinedRoom()
        {
            // Reset leaving flag when the player successfully joins a room.
            isLeaving = false;
            // Notify listeners about the successful room joining.
            MenuUIManager.Instance.OnNetworkEvent(NetworkEvent.JoinedRoom);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            // Notify listeners about the new player entering.
            MenuUIManager.Instance.OnPlayerEnter(newPlayer.ActorNumber);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            // Notify listeners about the player leaving.
            MenuUIManager.Instance.OnPlayerLeft(otherPlayer);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // Notify listeners about the updated properties of the player.
            MenuUIManager.Instance.OnPlayerPropsUpdate(targetPlayer.ActorNumber, changedProps);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            // Update the room list UI.
            MenuUIManager.Instance.UpdateRoomList(roomList);
        }

        public override void OnCreatedRoom()
        {
            // Notify listeners about the room creation event.
            MenuUIManager.Instance.OnNetworkEvent(NetworkEvent.CreatedRoom);
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            // Leave the room when the master client is switched.
            if (!isLeaving)
            {
                isLeaving = true;
                PhotonNetwork.LeaveRoom();
            }
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"Join Room Failed with return code {returnCode} and \nMessage: {message}");
            // Notify listeners about the failure.
            MenuUIManager.Instance.OnError();
        }
    }
}