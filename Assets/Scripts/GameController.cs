using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System;
using Photon.Pun;

public class GameController : MonoBehaviour
{
  // config params
  const string VELOCITY_DISPLAY = "velocity display";

  [SerializeField] TMP_Text alertDisplay;
  [SerializeField] TMP_Text opponentReadyDisplay;

  [SerializeField] GameObject player1SpawnPosition;
  [SerializeField] GameObject player2SpawnPosition;
  GameObject spawnPosition;
  [SerializeField] GameObject asteroidSpawn;
  [SerializeField] GameObject debrisSpawn;

  // state variables

  List<MoveLog> moveLog = new List<MoveLog>();

  bool[] damageTaken = new bool[33];

  Vector3 rectStart; //DragSelect code

  [SerializeField] List<string> playerList = new List<string>();
  bool refreshList = true;
  int playerNumber;

  List<string> messages = new List<string>();

  ActionBar actionBar;
  EventSystem eventSystem;

  void Start()
  {
    if (!PhotonNetwork.IsConnected)
    {
      FindObjectOfType<Menu>().QuitToMainMenu();
      return;
    }

    if (PhotonNetwork.IsMasterClient)
    {
      for (int i = 1; i < 13; i++)
      {
        GameObject asteroid = PhotonNetwork.InstantiateSceneObject("Hazard",
            asteroidSpawn.transform.position, transform.rotation, 0);
        asteroid.GetPhotonView().RPC("SetHazardImage", RpcTarget.AllBuffered, "Asteroid", i);

        asteroidSpawn.transform.position += new Vector3(0, -7);
      }
      for (int i = 1; i < 7; i++)
      {
        GameObject debris = PhotonNetwork.InstantiateSceneObject("Hazard",
            debrisSpawn.transform.position, transform.rotation, 0);
        debris.GetPhotonView().RPC("SetHazardImage", RpcTarget.AllBuffered, "Debris", i);

        debrisSpawn.transform.position += new Vector3(0, -7);
      }
      for (int i = 1; i < 7; i++)
      {
        GameObject gascloud = PhotonNetwork.InstantiateSceneObject("Hazard",
            debrisSpawn.transform.position, transform.rotation, 0);
        gascloud.GetPhotonView().RPC("SetHazardImage", RpcTarget.AllBuffered, "Gascloud", i);

        debrisSpawn.transform.position += new Vector3(0, -7);
      }

      GameObject relayDevice = PhotonNetwork.InstantiateSceneObject("RelayDevice",
          transform.position, transform.rotation, 0);

      relayDevice.GetPhotonView().RPC("SetPlaymat", RpcTarget.AllBuffered, FindObjectOfType<Loader>().GetPlaymat());
      relayDevice.GetPhotonView().RPC(
          "CacheSquad", RpcTarget.AllBuffered, JsonUtility.ToJson(FindObjectOfType<Loader>().GetSquad()), false);

      spawnPosition = player1SpawnPosition;
      playerNumber = 1;
    }

    else
    {
      spawnPosition = player2SpawnPosition;
      playerNumber = 2;
    }

    Loader.XWSquad squad = FindObjectOfType<Loader>().GetSquad();

    foreach (Loader.Pilot pilot in squad.pilots)
    {
      GameObject newShip = PhotonNetwork.Instantiate("Ship",
              spawnPosition.transform.position, spawnPosition.transform.rotation, 0);

      newShip.GetPhotonView().RPC("ConfigureShip", RpcTarget.AllBuffered, JsonUtility.ToJson(pilot));
      newShip.GetComponent<Ship>().ownShip = true;

      FindObjectOfType<Menu>().arcColor = pilot.faction;

      spawnPosition.transform.position += new Vector3(0, 8);

      FindObjectOfType<Menu>().LoadDials(pilot, 1); // Always one as the first hand is yours no matter what player number you are
    }

    StartCoroutine(SetAlertMessage("setup mode - press alt to begin match", 0, false));
    opponentReadyDisplay.text = "";

    // Populate player list
    Photon.Realtime.Player[] photonList = PhotonNetwork.PlayerList;
    for (int i = 0; i < photonList.Count(); i++)
    {
      playerList.Add(photonList[i].NickName);
    }

    actionBar = FindObjectOfType<ActionBar>();
    eventSystem = FindObjectOfType<EventSystem>();
  }

  void Update()
  {
    UndoMove();
    ChangeVelocity();
    DeselectAll();
    //DrawSelection();

    if (refreshList)
    {
      refreshList = false;
      StartCoroutine(UpdatePlayerList());
    }
  }

  // MOVEMENT
  // !TODO Important Why doesnt this just log the actual last rotation, rather than the change?
  public void LogMove(string shipID, Vector2 position, float rotation, int stress)
  {
    moveLog.Add(new MoveLog { ship = shipID, move = position, angle = rotation, stress = stress });
  }

  private void UndoMove()
  {
    if (Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl))
    {
      if (moveLog.Count != 0)
      {
        foreach (Ship ship in FindObjectsOfType<Ship>())
        {
          if (ship.GetUniqueID() == moveLog.Last().ship)
          {
            ship.gameObject.transform.position = moveLog.Last().move;
            ship.gameObject.transform.Rotate(0, 0, -moveLog.Last().angle);

            ship.gameObject.GetPhotonView().RPC(
                "AdjustTokens",
                RpcTarget.AllBuffered,
                "stress",
                -moveLog.Last().stress
            );

            ship.gameObject.GetPhotonView().RPC("CancelTemplateDrop", RpcTarget.AllBuffered);

            moveLog.RemoveAt(moveLog.Count() - 1);

            break;
          }
        }
      }
    }
  }

  private void ChangeVelocity()
  {
    KeyCode[] velocityKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };
    for (int i = 0; i < velocityKeys.Length; i++)
    {
      Menu menu = FindObjectOfType<Menu>();
      bool menusOpen = menu.CheckMenuOpen() || menu.CheckOpenHand() || menu.CheckDiceOpen();
      if (Input.GetKeyDown(velocityKeys[i]) && !menusOpen)
      {
        GameObject.Find(VELOCITY_DISPLAY).GetComponent<TMP_Text>().text = (i + 1).ToString();

        Ship[] ships = FindObjectsOfType<Ship>();
        foreach (Ship ship in ships)
        {
          ship.SetVelocity(i + 1);
        }
      }
    }
  }

  //OFFLINE VERSION
  /*private void LoadSquads()
  {
      Menu menu = FindObjectOfType<Menu>();
      foreach (Loader.XWSquad squad in squads)
      {
          int squadNumber = squads.IndexOf(squad);
          float positionOffsetX = -41 + (int.Parse(squad.faction) * 82);
          float positionOffsetY = -15;

          foreach (Loader.Pilot pilot in squad.pilots)
          {
              GameObject newShip = Instantiate(ship[pilot.config.Size()], transform.position, transform.rotation) as GameObject;

              // TODO pass json to ConfigureShip()

              newShip.transform.SetParent(GameObject.Find(SHIP_PARENT_NAME).transform);
              newShip.transform.localPosition = new Vector3(positionOffsetX, positionOffsetY, -1);
              positionOffsetY += 8;
              newShip.transform.Rotate(0, 0, -90 + (squadNumber * 180));

              Stats stats = newShip.GetComponent<Stats>();
              stats.SetHull(pilot.config.Hull());
              stats.SetShield(pilot.config.Shield());
              stats.SetInitiative(pilot.initiative);
              stats.SetForce(pilot.force);

              menu.LoadDials(pilot);
          }
      }
  }*/

  // MISC

  public void DeselectAll()
  {
    if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) && Input.GetMouseButtonDown(0))
    {
      if (!actionBar.isMouseOver)
      {
        Ship[] ships = FindObjectsOfType<Ship>();
        bool mouseOverShip = false;
        foreach (Ship ship in ships)
        {
          if (!ship.CheckForMouseOver())
          {
            ship.DeselectShip();
          }
          else
          {
            mouseOverShip = true;
          }
        }

        // Determine whether a dropdown menu is in use before hiding the action bar
        bool dropdownActive;
        try
        {
          eventSystem.currentSelectedGameObject.GetComponent<Dropdown>();
          dropdownActive = true;
        }
        catch { dropdownActive = false; }

        if (!mouseOverShip && !dropdownActive)
        {
          actionBar.ToggleBar("hidden");
          actionBar.attachedShip = null;

        }

        Hazards[] hazards = FindObjectsOfType<Hazards>();
        foreach (Hazards hazard in hazards)
        {
          if (!hazard.CheckForMouseOver())
          {
            hazard.Deselect();
          }
        }
        Bomb[] bombs = FindObjectsOfType<Bomb>();
        foreach (Bomb bomb in bombs)
        {
          if (!bomb.CheckForMouseOver())
          {
            bomb.Deselect();
          }
        }
      }
    }
  }

  private void DrawSelection()
  {
    LineRenderer line = GetComponent<LineRenderer>();

    if (Input.GetMouseButtonDown(0))
    {
      rectStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
      rectStart.z = -1;
    }

    if (Input.GetMouseButtonUp(0))
    {
      foreach (Ship ship in FindObjectsOfType<Ship>())
      {
        bool inWidth = ship.transform.position.x >
                        Mathf.Clamp(line.GetPosition(0).x, 0, line.GetPosition(2).x)
                        && ship.transform.position.x <
                        Mathf.Clamp(line.GetPosition(0).x, line.GetPosition(2).x, 100);
        bool inHeight = ship.transform.position.y >
                        Mathf.Clamp(line.GetPosition(0).y, 0, line.GetPosition(2).y)
                        && ship.transform.position.y <
                        Mathf.Clamp(line.GetPosition(0).y, line.GetPosition(2).y, 100);

        if (inWidth && inHeight)
        {
          Debug.Log(ship.name);
        }
      }


      rectStart = Vector3.zero;
      line.SetPositions(
          new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
    }

    if (rectStart != Vector3.zero)
    {
      float xPos = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
      float yPos = Camera.main.ScreenToWorldPoint(Input.mousePosition).y;

      line.SetPosition(0, rectStart);
      line.SetPosition(1, new Vector3(rectStart.x, yPos, -1));
      line.SetPosition(2, new Vector3(xPos, yPos, -1));
      line.SetPosition(3, new Vector3(xPos, rectStart.y, -1));
    }
  }

  public bool CheckDamageCard(int cardNumber)
  {
    if (damageTaken[cardNumber])
    {
      return true;
    }
    damageTaken[cardNumber] = true;
    return false;
  }

  public void ReshuffleDamageCard(int cardNumber)
  {
    damageTaken[cardNumber] = false;
  }

  public Vector3 TransformVectorByAngle(float xTrans, float angle)
  {
    float angleRadians = angle * Mathf.Deg2Rad;
    float x = Mathf.Cos(angleRadians) * xTrans;
    float y = Mathf.Sin(angleRadians) * xTrans;
    return new Vector3(x, y);
  }

  public IEnumerator SetAlertMessage(string message, int duration, bool log)
  {
    while (alertDisplay.text != "" && !alertDisplay.text.Contains("setup"))
    {
      yield return new WaitForEndOfFrame();
    }

    alertDisplay.text = message;
    if (log)
    {
      messages.Add(message);
    }

    yield return new WaitForSeconds(duration);
    if (duration != 0)
    {
      alertDisplay.text = "";
    }
  }

  public void SetOpponentReady(string player, bool state)
  {
    opponentReadyDisplay.text = state
        ? player + " is ready for activation"
        : "";
  }

  public void DisconnectFromNetwork()
  {
    PhotonNetwork.Disconnect();
  }

  private IEnumerator UpdatePlayerList()
  {
    List<string> updatedList = new List<string>();
    Photon.Realtime.Player[] photonList = PhotonNetwork.PlayerList;

    // Check for new players
    for (int i = 0; i < photonList.Count(); i++)
    {
      string player = photonList[i].NickName;
      updatedList.Add(player);
      if (!playerList.Contains(player))
      {
        StartCoroutine(SetAlertMessage(player + " has joined the game", 5, true));
      }
    }

    // Check for disconnected players
    for (int i = 0; i < playerList.Count(); i++)
    {
      string player = playerList[i];
      if (!updatedList.Contains(player))
      {
        StartCoroutine(SetAlertMessage(player + " has left the game", 5, true));
      }
    }

    playerList = updatedList;

    yield return new WaitForSeconds(3);
    refreshList = true;
  }

  public int GetPlayerNumber()
  {
    return playerNumber;
  }

  public void SetPlayerNumber(int num)
  {
    playerNumber = num;
  }

  public Color32 GetArcColor(int option)
  {
    Color32[] colors = {
            new Color32(120, 0, 0, 185), // RED
            new Color32(0, 120, 0, 185), // GREEN
            new Color32(110, 70, 0, 185), // YELLOW
            new Color32(0, 60, 120, 185), // BLUE
            new Color32(80, 0, 120, 185), // PURPLE
        };

    return colors[option];
  }

  private class MoveLog
  {
    public string ship;
    public Vector2 move;
    public float angle;
    public int stress;
  }
}
