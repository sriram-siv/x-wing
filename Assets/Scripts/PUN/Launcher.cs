using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

using Photon.Pun;
using Photon.Realtime;

namespace Photon.Pun.Demo.PunBasics
{
    public class Launcher : MonoBehaviourPunCallbacks
    {
        [SerializeField] GameObject nameEntry;
        [SerializeField] GameObject lobbyDisplay;
        [SerializeField] GameObject roomDisplay;

        [SerializeField] byte maxPlayersPerRoom = 2;

        bool isConnecting;

        string gameVersion = "1";

        [Space(10)]
        [Header("Custom Variables")]
        public InputField playerNameField;
        public InputField roomNameField;
        [SerializeField] Text roomDisplayTitle;
        [SerializeField] GameObject roomListDisplay;
        [SerializeField] GameObject playerListDisplay;
        [SerializeField] GameObject playmatSelect;
        [SerializeField] GameObject playmatPreview;

        [Space(5)]
        public Text playerStatus;
        public Text connectionStatus;

        [Space(5)]
        public GameObject buttonLoadArena;
        public GameObject buttonJoinRoom;
        [SerializeField] GameObject buttonLeaveRoom;
        [SerializeField] GameObject buttonQuitToMenu;

        string playerName = "";
        string roomName = "";

        int playerCount = 0;

        void Start() 
        {
            PlayerPrefs.DeleteAll(); 

            Debug.Log("Connecting to Photon Network");

            buttonLoadArena.SetActive(false);
            buttonLeaveRoom.SetActive(false);

            ConnectToPhoton();
        }

        void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        // Helper Methods

        public void SetPlayerName()
        {
            playerName = playerNameField.text;
            
            if (playerName != "")
            {
                nameEntry.SetActive(false);
                lobbyDisplay.SetActive(true);

                FindObjectOfType<Loader>().SetPlayerName(playerName);
            }
        }

        public void SetRoomName(string name)
        {
            roomName = name;
        }

        public void SelectRoomFromList(GameObject room)
        {
            string name = room.transform.GetChild(0).GetComponent<Text>().text;

            if (name != "")
            {
                playerStatus.text = "";
                roomName = name;
                Debug.Log(roomName);
                JoinRoom();
            }
        }

        public void SelectPlaymat(int selectedPlaymat)
        {
            FindObjectOfType<Loader>().SetPlaymat(selectedPlaymat);
            playmatPreview.GetComponent<Image>().sprite = Resources.Load<Sprite>("Playmat/" + selectedPlaymat);
        }

        void Update()
        {
            if (PhotonNetwork.NetworkClientState.ToString() == "ConnectedToMaster" && !PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }

        // Tutorial Methods

        void ConnectToPhoton()
        {
            connectionStatus.text = "Connecting...";
            PhotonNetwork.GameVersion = gameVersion; //1
            PhotonNetwork.ConnectUsingSettings(); //2
        }

        public void JoinRoom() // this is actually create room
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.LocalPlayer.NickName = playerName;
                Debug.Log("PhotonNetwork.IsConnected! | Trying to Create/Join Room " + 
                    roomName);
                RoomOptions roomOptions = new RoomOptions() { CleanupCacheOnLeave = false };
                TypedLobby typedLobby = new TypedLobby(roomName, LobbyType.Default);
                PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, null);  // typedLobby was the third argument but preventing room list update
            }
        }

        public void LoadArena()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount > 0) // CHANGE THIS BACK
            {
                PhotonNetwork.LoadLevel("Game");
            }
            else
            {
                playerStatus.text = "Minimum 2 Players required to Load Arena!";
            }
        }

        public void QuitToMainMenu()
        {
            PhotonNetwork.Disconnect();
            Destroy(GameObject.Find("Loader"));
            SceneManager.LoadScene(0);
        }

        public void ExitCurrentRoom()
        {
            playerStatus.text = "";

            roomDisplay.SetActive(false);
            lobbyDisplay.SetActive(true);

            //buttonLeaveRoom.SetActive(false);
            //buttonQuitToMenu.SetActive(true);
        }

        // Photon Methods

        public override void OnConnected()
        {
            // 1
            base.OnConnected();
            // 2
            connectionStatus.text = "Connected to Photon!";
            connectionStatus.color = Color.green;
            buttonLoadArena.SetActive(false);
            nameEntry.SetActive(true);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            isConnecting = false;
            Debug.LogError("Disconnected. Please check your Internet connection.");
            QuitToMainMenu();
        }

        public override void OnJoinedRoom()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                buttonLoadArena.SetActive(true);
                playmatSelect.SetActive(true);
                playerStatus.text = "You are the room host";
            }
            else
            {
                playerStatus.text = "Joined room - waiting for host to start";
            }

            lobbyDisplay.SetActive(false);
            roomDisplay.SetActive(true);
            //buttonQuitToMenu.SetActive(false);
            //buttonLeaveRoom.SetActive(true);

            roomDisplayTitle.text = roomName;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerListDisplay.transform.GetChild(playerCount).GetChild(0).GetComponent<Text>().text = player.NickName;
                playerCount++;
            }
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            foreach (RoomInfo room in roomList)
            {
                int index = roomList.IndexOf(room);
                roomListDisplay.transform.GetChild(index).GetChild(0).GetComponent<Text>().text = room.Name;
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            playerListDisplay.transform.GetChild(playerCount).GetChild(0).GetComponent<Text>().text = newPlayer.NickName;
            playerCount++;
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            playerListDisplay.transform.GetChild(playerCount - 1).GetChild(0).GetComponent<Text>().text = "";
            playerCount = 0;
            
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerListDisplay.transform.GetChild(playerCount).GetChild(0).GetComponent<Text>().text = player.NickName;
                playerCount++;
            }
        }
    }
}
