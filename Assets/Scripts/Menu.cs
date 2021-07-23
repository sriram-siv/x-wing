using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.IO;

public class Menu : MonoBehaviour
{
  [Header("Menus")]
  [SerializeField] GameObject menu;
  [SerializeField] GameObject squadMenu;
  [SerializeField] GameObject templateDisplay;
  [SerializeField] GameObject controlsDisplay;
  [SerializeField] GameObject diceDisplay;
  [SerializeField] GameObject optionsDisplay;
  [SerializeField] GameObject hand;
  [SerializeField] GameObject ownHand;
  [SerializeField] GameObject oppHand;
  [SerializeField] GameObject saveDisplay;
  [SerializeField] InputField fileName;
  [SerializeField] GameObject loadDisplay;
  [SerializeField] InputField loadName;
  [SerializeField] Text fileList;
  [SerializeField] GameObject confirmSave;
  [SerializeField] GameObject confirmDelete;
  [SerializeField] GameObject confirmQuit;

  [Space(10)]
  [Header("Buttons")]
  [SerializeField] GameObject squadButton;
  [SerializeField] GameObject damageButton;
  [SerializeField] GameObject switchHandsButton;
  [SerializeField] GameObject rangeButton;
  [SerializeField] GameObject menuButton;
  [SerializeField] GameObject backButton;
  [SerializeField] GameObject diceButton;
  [SerializeField] GameObject readyButton;

  [Space(10)]
  [Header("Dice")]
  [SerializeField] Dropdown diceAmount;
  [SerializeField] GameObject dice;
  [SerializeField] GameObject diceMarkers;

  [Space(10)]
  [Header("Objects")]
  [SerializeField] GameObject dial;
  [SerializeField] GameObject damage;
  [SerializeField] AudioClip diceSFX;
  [SerializeField] GameObject guides;
  [SerializeField] GameObject rangeFinder;
  [SerializeField] GameObject opponentRangeFinder;
  [SerializeField] GameObject dialSpawn;

  // state variables

  bool menuVisible = false;
  bool handVisible = false;
  bool manualModeOn = true;
  bool rangeModeOn = false;
  int zoom = -3;
  bool damageHover = false;
  bool readyToActivate = false;

  int dialCount = 0;
  bool opponentDialsLoaded = false;
  int visibleDials = 0;

  // DAMAGE

  [SerializeField] bool[] damageTaken = new bool[33];
  int remainingCardsInDeck = 33;

  // SCROLL VARIABLES
  float scrollX = 47.111f;
  float scrollY = 26.5f;
  float minX = 47.111f;
  float maxX = 47.111f;
  float minY = 37f;
  float maxY = 56f;
  float scrollSpeed = 3;

  // DICE RESULTS
  int maxDice = 6;
  string[] attackResults = new string[6];
  string[] defenseResults = new string[6];

  // OPTIONS
  bool autoStress = true;
  bool soundFX = false;
  bool rangeInPX = false;
  bool showTemplates = false;
  public int arcColor = 0;
  [SerializeField] Dropdown playerDropdown;
  [SerializeField] Dropdown arcColorDropdown;


  [SerializeField] Loader.SaveFile savedGame;
  [SerializeField] Loader.SaveFile readData;
  string SAVE_FOLDER;

  // Cached references

  PhotonView relay;
  GameController controller;

  void Start()
  {
    SAVE_FOLDER = Application.dataPath + "/Saves/";
    controller = FindObjectOfType<GameController>();
  }

  void Update()
  {
    Controls();
    ScrollPlayArea();

    if (rangeModeOn) { DrawRange(); }

    if (relay == null)
    {
      try
      {
        relay = FindObjectOfType<RelayDevice>().gameObject.GetPhotonView();
      }
      catch
      {
        Debug.Log("Relay not available yet");
      }
    }
  }

  public void ToggleMenu()
  {
    if (rangeModeOn) { RangeToggle(); }

    bool startingState = menuVisible;

    ExitMenu();

    if (startingState == false)
    {
      menuVisible = true;
      menu.SetActive(true);
      menuButton.transform.GetChild(0).GetComponent<Image>().color = Color.green;
    }
  }

  /// <summary>
  /// Close main menu tab if open when a ship is selected
  /// </summary>
  public void CloseMainMenu()
  {
    menuVisible = false;
    menu.SetActive(false);
    menuButton.transform.GetChild(0).GetComponent<Image>().color = Color.white;
  }

  public void SetupMode()
  {
    manualModeOn = !manualModeOn;
    guides.SetActive(manualModeOn);

    foreach (Ship ship in FindObjectsOfType<Ship>())
    {
      ship.ManualMode();
    }
    foreach (Hazards hazard in FindObjectsOfType<Hazards>())
    {
      bool withinMapX = hazard.transform.position.x > 0 && hazard.transform.position.x < 92;
      bool withinMapY = hazard.transform.position.y > 0 && hazard.transform.position.y < 92;
      if (!withinMapX || !withinMapY)
      {
        hazard.transform.localScale = manualModeOn ? Vector3.one : Vector3.zero;
      }
    }

    string message = manualModeOn
        ? "setup mode - press alt to resume match"
        : "";
    StartCoroutine(controller.SetAlertMessage(message, 0, false));
  }

  public void RangeToggle()
  {
    rangeModeOn = !rangeModeOn;
    rangeButton.transform.GetChild(0).GetComponent<Image>().color = rangeModeOn == true
        ? Color.blue
        : Color.white;

    if (!rangeModeOn)
    {
      LineRenderer line = rangeFinder.GetComponent<LineRenderer>();
      line.SetPosition(0, Vector3.zero);
      line.SetPosition(1, Vector3.zero);
      rangeFinder.transform.GetChild(0).gameObject.SetActive(false);


      relay.RequestOwnership();
      relay.RPC("SendRangeInfo", RpcTarget.OthersBuffered,
          Vector3.zero, Vector3.zero, rangeInPX);
    }
  }

  private void DrawRange()
  {
    LineRenderer line = rangeFinder.GetComponent<LineRenderer>();
    GameObject label = rangeFinder.transform.GetChild(0).gameObject;

    if (Input.GetMouseButtonDown(0))
    {
      Vector3 startingPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
      startingPoint.z = 0;

      line.SetPosition(0, startingPoint);
    }
    if (Input.GetMouseButton(0))
    {
      Vector3 endPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
      endPoint.z = 0;

      line.positionCount = 2;
      line.SetPosition(1, endPoint);

      float distance = rangeInPX
          ? Mathf.Ceil((line.GetPosition(0) - endPoint).magnitude * 25f)
          : Mathf.Ceil((line.GetPosition(0) - endPoint).magnitude * 0.1f);

      label.transform.position = endPoint + new Vector3(0, 2, 0);

      label.GetComponent<TMP_Text>().text = rangeInPX
          ? distance.ToString() + " px"
          : "Range " + distance.ToString();

      label.SetActive(distance > 0 ? true : false);


      relay.RequestOwnership();
      relay.RPC("SendRangeInfo", RpcTarget.OthersBuffered,
          line.GetPosition(0), endPoint, rangeInPX);
    }
  }

  public void DrawOpponentRange(Vector3 start, Vector3 end, bool px)
  {
    LineRenderer line = opponentRangeFinder.GetComponent<LineRenderer>();
    GameObject label = opponentRangeFinder.transform.GetChild(0).gameObject;

    line.positionCount = 2;
    line.SetPosition(0, start);
    line.SetPosition(1, end);

    float distance = px
            ? Mathf.Ceil((line.GetPosition(0) - line.GetPosition(1)).magnitude * 25f)
            : Mathf.Ceil((line.GetPosition(0) - line.GetPosition(1)).magnitude * 0.1f);

    label.transform.position = end + new Vector3(0, 2, 0);

    label.GetComponent<TMP_Text>().text = px
        ? distance.ToString() + " px"
        : "Range " + distance.ToString();

    label.SetActive(distance > 0 ? true : false);
  }

  public void OpenHand()
  {
    // Disables tab hotkey inside certain menus
    if (backButton.activeSelf) { return; }

    if (rangeModeOn) { RangeToggle(); }

    bool startingState = hand.activeSelf;

    ExitMenu();

    if (startingState == false)
    {
      handVisible = true;
      hand.SetActive(true);
      squadMenu.SetActive(true);
      squadButton.transform.GetChild(0).GetComponent<Image>().color = Color.green;

      if (visibleDials == 1) { SwitchHand(); }

      Dial[] dials = FindObjectsOfType<Dial>();
      foreach (Dial dial in dials)
      {
        dial.Deselect();
        dial.GetAttachedShip().ToggleMoveIndicator(true);
      }

    }
  }

  public void ExitMenu()
  {
    hand.SetActive(false);
    diceDisplay.SetActive(false);
    templateDisplay.SetActive(false);
    controlsDisplay.SetActive(false);
    optionsDisplay.SetActive(false);
    saveDisplay.SetActive(false);
    loadDisplay.SetActive(false);
    confirmSave.SetActive(false);
    confirmDelete.SetActive(false);
    confirmQuit.SetActive(false);

    menu.SetActive(false);
    squadMenu.SetActive(false);

    backButton.SetActive(false);
    squadButton.SetActive(true);
    diceButton.SetActive(true);
    menuButton.SetActive(true);

    diceButton.transform.GetChild(0).GetComponent<Image>().color = Color.white;
    menuButton.transform.GetChild(0).GetComponent<Image>().color = Color.white;
    squadButton.transform.GetChild(0).GetComponent<Image>().color = Color.white;

    damageHover = false;
    handVisible = false;
    menuVisible = false;

    Ship[] ships = FindObjectsOfType<Ship>();
    foreach (Ship ship in ships)
    {
      ship.DeselectShip();
      ship.ToggleMoveIndicator(false);
    }

    FindObjectOfType<ActionBar>().ToggleActionBar(0);
    FindObjectOfType<ActionBar>().AttachShip(null);

  }

  public void ChangeControlsTab()
  {
    GameObject standard = controlsDisplay.transform.GetChild(0).gameObject;
    GameObject manual = controlsDisplay.transform.GetChild(1).gameObject;
    GameObject button = controlsDisplay.transform.GetChild(2).gameObject;

    standard.SetActive(!standard.activeSelf);
    manual.SetActive(!manual.activeSelf);

    button.GetComponentInChildren<TMP_Text>().text = standard.activeSelf
        ? "manual controls" : "standard controls";
  }

  public void LoadDials(Loader.Pilot pilot, int player)
  {
    // Get the zoom ratio and adjust the dial spawner if necessary
    // Dials also rescaled using this value
    float zoomRatio = Camera.main.orthographicSize / 41.5f;
    if (dialCount == 0)
    {
      dialSpawn.transform.localPosition *= zoomRatio;
    }

    var newDial = Instantiate(dial);
    newDial.GetComponent<Dial>().InitializeDial(pilot, player);

    newDial.transform.localScale *= zoomRatio;
    newDial.transform.parent = hand.transform.GetChild(player);
    newDial.transform.position = dialSpawn.transform.position;
    dialSpawn.transform.localPosition += new Vector3(12, 0) * zoomRatio;
    dialCount++;

    if (dialCount % 5 == 0)
    {
      dialSpawn.transform.localPosition += new Vector3(-60, -15) * zoomRatio;
    }
  }

  public void TakeDamage()
  {
    Dial activeDial = null;
    foreach (Dial dial in FindObjectsOfType<Dial>())
    {
      if (dial.IsSelected())
      {
        activeDial = dial;
      }
    }
    if (activeDial == null)
    {
      StartCoroutine(controller.SetAlertMessage("select a ship dial to assign damage to", 5, false));
      return;
    }

    GameObject newDamage = Instantiate(damage) as GameObject;

    int cardIndex = DrawDamage();
    newDamage.GetComponent<DamageDeck>().CardFinder(cardIndex);
    newDamage.GetComponent<DamageDeck>().SetPilot(activeDial.name.Replace("_dial", ""));
    newDamage.GetComponent<DamageDeck>().SetAsOwn();

    newDamage.transform.parent = activeDial.GetDamageObj().transform;

    int cardNum = (activeDial.GetDamageObj().transform.childCount - 1) % 21;
    float yPos = Mathf.Floor(cardNum / 7) * 20;
    Vector3 cardPos = new Vector3((cardNum % 7) * 15, yPos);
    newDamage.transform.localPosition = cardPos;

    newDamage.transform.localScale = Vector3.one;

    relay.RPC(
        "UpdateDamage", RpcTarget.AllBuffered,
        controller.GetPlayerNumber(),
        activeDial.name.Replace("_dial", ""),
        cardIndex,
        false
    );
  }

  private int DrawDamage()
  {
    if (remainingCardsInDeck == 0)
    {
      return -1;
    }

    int cardIndex = Random.Range(0, 33);
    while (damageTaken[cardIndex])
    {
      cardIndex = Random.Range(0, 33);
    }
    damageTaken[cardIndex] = true;
    remainingCardsInDeck--;

    return cardIndex;
  }

  public void ReshuffleDamage(int cardIndex)
  {
    damageTaken[cardIndex] = false;
    remainingCardsInDeck++;
  }

  public void SetPlayerReady()
  {
    string player = FindObjectOfType<Loader>().GetPlayerName();
    readyToActivate = !readyToActivate;

    readyButton.GetComponentInChildren<Image>().color = readyToActivate
        ? Color.green
        : Color.red;

    relay.RPC(
        "SetPlayerReady",
        RpcTarget.OthersBuffered,
        player,
        readyToActivate
    );
  }

  private void Zoom(int increase)
  {
    bool reclose = !handVisible;
    hand.SetActive(true);

    SetScreenPositions();

    Camera.main.orthographicSize -= increase;
    ChangeScrollSettings(increase);
    zoom += (int)Mathf.Sign(increase);

    ResizeComponents();
    hand.SetActive(!reclose);
  }

  private void SetScreenPositions()
  {
    foreach (Dial dial in FindObjectsOfType<Dial>())
    {
      dial.SetScreenPosition();
    }
  }

  private void ResizeComponents()
  {
    float componentResizeRatio = Camera.main.orthographicSize / 41.5f; // Camera starting size at min zoom
    float dialScale = 0.8f * componentResizeRatio;

    foreach (Dial dial in FindObjectsOfType<Dial>())
    {
      dial.transform.localScale = new Vector3(dialScale, dialScale, 1);
      dial.transform.position = Camera.main.ScreenToWorldPoint(dial.GetScreenPosition());

      dial.AnchorUpgradesDisplay();
    }
  }

  private void DeselectDials()
  {
    if (damageHover) { return; }

    Dial[] dials = FindObjectsOfType<Dial>();
    foreach (Dial dial in dials)
    {
      if (!dial.mouseOver)
      {
        dial.Deselect();
      }
    }
  }

  public bool CheckManualMode()
  {
    return manualModeOn;
  }

  public bool CheckOpenHand()
  {
    return handVisible;
  }

  public bool CheckMenuOpen()
  {
    return menuVisible;
  }

  public bool CheckPlayAreaActive()
  {
    return !handVisible &&
            !rangeModeOn &&
            !diceDisplay.activeSelf &&
            !controlsDisplay.activeSelf &&
            !templateDisplay.activeSelf &&
            !optionsDisplay.activeSelf &&
            !saveDisplay.activeSelf &&
            !loadDisplay.activeSelf &&
            !confirmSave.activeSelf &&
            !confirmDelete.activeSelf &&
            !confirmQuit.activeSelf;
  }

  public bool CheckDiceOpen()
  {
    return diceDisplay.activeSelf;
  }

  public void DamageButtonHover(bool state)
  {
    damageHover = state;
  }

  public void OpenQuitDialg()
  {
    ExitMenu();
    confirmQuit.SetActive(true);
  }

  public void QuitToMainMenu()
  {
    Destroy(GameObject.Find("Loader"));
    PhotonNetwork.Disconnect();
    SceneManager.LoadScene(0);
  }

  private void Controls()
  {
    bool zoomIn = Input.GetKeyDown(KeyCode.Equals);
    bool zoomOut = Input.GetKeyDown(KeyCode.Minus);
    bool manualMode = Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);
    bool openMenu = Input.GetKeyDown(KeyCode.Escape);
    bool toggleRange = Input.GetKeyDown(KeyCode.R);

    bool blank = Input.GetKeyDown(KeyCode.B);
    bool focus = Input.GetKeyDown(KeyCode.F);
    bool evade = Input.GetKeyDown(KeyCode.E);
    bool hit = Input.GetKeyDown(KeyCode.H);
    bool crit = Input.GetKeyDown(KeyCode.C);

    bool subMenuOpen =
        controlsDisplay.activeSelf ||
        templateDisplay.activeSelf ||
        optionsDisplay.activeSelf ||
        diceDisplay.activeSelf ||
        confirmSave.activeSelf ||
        confirmDelete.activeSelf ||
        confirmQuit.activeSelf ||
        loadDisplay.activeSelf ||
        saveDisplay.activeSelf ||
        handVisible;

    KeyCode[] diceKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6 };

    if (diceDisplay.activeSelf)
    {
      for (int i = 0; i < diceKeys.Length; i++)
      {
        if (Input.GetKeyDown(diceKeys[i]))
        {
          diceAmount.value = i;
        }
      }
    }

    if (zoomIn && zoom < 3)
    {
      Zoom(5);
    }
    if (zoomOut && zoom > -3)
    {
      Zoom(-5);
    }
    if (manualMode)
    {
      relay.RPC("SetupMode", RpcTarget.AllBuffered);
    }
    if (openMenu)
    {
      if (subMenuOpen)
      {
        ExitMenu();
      }
      else
      {
        ToggleMenu();
      }
    }

    if (toggleRange) { RangeToggle(); }
    if (Input.GetKeyDown(KeyCode.Tab)) { OpenHand(); }
    if (Input.GetMouseButtonDown(0)) { DeselectDials(); }
    if (subMenuOpen && openMenu) { ExitMenu(); }

    if (blank) { ModifyDice("blank"); }
    if (focus) { ModifyDice("focus"); }
    if (evade) { ModifyDice("evade"); }
    if (hit) { ModifyDice("hit"); }
    if (crit) { ModifyDice("crit"); }
  }

  private void ScrollPlayArea()
  {
    if (Input.GetKey(KeyCode.LeftControl))
    {
      scrollX = Mathf.Clamp(Camera.main.transform.position.x + Input.GetAxis(
          "Mouse ScrollWheel") * scrollSpeed, 20f, 74.5f);
    }
    else
    {
      Loader loader = FindObjectOfType<Loader>();
      float xChange = Input.mouseScrollDelta.x;
      if (loader.isWebGL) xChange *= -1;

      scrollX = Mathf.Clamp(Camera.main.transform.position.x -
          (xChange / scrollSpeed), minX, maxX);
      scrollY = Mathf.Clamp(Camera.main.transform.position.y +
          Input.GetAxis("Mouse ScrollWheel") * scrollSpeed, minY, maxY);
    }
    Camera.main.transform.position = new Vector3(scrollX, scrollY, -50);
    templateDisplay.transform.position = new Vector2(scrollX, scrollY);
    hand.transform.position = new Vector2(scrollX, scrollY);
  }

  public void ChangeScrollSettings(float change)
  {
    float aspectRatio = (float)Screen.width / (float)Screen.height;
    // This adjusts the min/max scroll to allow the right space for the header and footer displays
    float borderAdjust = Mathf.Sign(change) / 2;

    minY -= change - borderAdjust;
    maxY += change - borderAdjust;

    if (Camera.main.orthographicSize * aspectRatio < 47.111f)
    {
      minX -= change * aspectRatio;
      maxX += change * aspectRatio;
      minX = Mathf.Clamp(minX, Camera.main.orthographicSize * aspectRatio, 100);
      maxX = Mathf.Clamp(maxX, 0, 94.222f - Camera.main.orthographicSize * aspectRatio);
    }
    else
    {
      minX = 47.111f;
      maxX = 47.111f;
    }
  }

  public void ToggleSubmenu(GameObject submenu)
  {
    ExitMenu();

    squadButton.SetActive(false);
    diceButton.SetActive(false);
    menuButton.SetActive(false);
    backButton.SetActive(true);

    submenu.SetActive(true);

    arcColorDropdown.value = arcColor;
    playerDropdown.value = controller.GetPlayerNumber();

    if (!controlsDisplay.transform.GetChild(0).gameObject.activeSelf)
    {
      ChangeControlsTab();
    }
  }

  public void ToggleDice()
  {
    bool startingState = diceDisplay.activeSelf;
    ExitMenu();

    if (startingState == false)
    {
      diceDisplay.SetActive(true);
      diceButton.transform.GetChild(0).GetComponent<Image>().color = Color.green;
    }
  }

  public void RollAttackDice()
  {
    for (int i = 0; i < maxDice; i++)
    {
      diceMarkers.transform.GetChild(i).localScale = Vector3.zero;
      if (i <= diceAmount.value)
      {
        string result = GetDiceResult("attack");
        dice.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Dice/attack_" + result);
        dice.transform.GetChild(i).localScale = Vector3.one;

        attackResults[i] = result;
      }
      else
      {
        dice.transform.GetChild(i).localScale = Vector3.zero;

        attackResults[i] = "null";
      }
    }
    relay.RPC("SendDiceInfo", RpcTarget.OthersBuffered, "attack", attackResults);

    GetComponent<AudioSource>().PlayOneShot(diceSFX);
  }

  public void RollDefenseDice()
  {
    for (int i = maxDice; i < dice.transform.childCount; i++)
    {
      diceMarkers.transform.GetChild(i).localScale = Vector3.zero;
      if (i <= diceAmount.value + maxDice)
      {
        string result = GetDiceResult("defense");
        dice.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Dice/defense_" + result);
        dice.transform.GetChild(i).localScale = Vector3.one;

        defenseResults[i - maxDice] = result;
      }
      else
      {
        dice.transform.GetChild(i).localScale = Vector3.zero;

        defenseResults[i - maxDice] = "null";
      }
    }

    relay.RPC("SendDiceInfo", RpcTarget.OthersBuffered, "defense", defenseResults);

    GetComponent<AudioSource>().PlayOneShot(diceSFX);
  }

  public void SelectDice(int diceNumber)
  {
    Transform marker = diceMarkers.transform.GetChild(diceNumber);
    marker.localScale = marker.localScale == Vector3.zero
        ? Vector3.one
        : Vector3.zero;

    if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
    {
      for (int i = 0; i < diceMarkers.transform.childCount; i++)
      {
        if (i == diceNumber) { continue; }

        diceMarkers.transform.GetChild(i).localScale = Vector3.zero;
      }
    }
  }

  private void ModifyDice(string face)
  {
    for (int i = 0; i < dice.transform.childCount; i++)
    {
      // If the dice has been selected it will have a marker with scale of one
      if (diceMarkers.transform.GetChild(i).localScale == Vector3.one)
      {
        if (i < maxDice) // use maxDice
        {
          if (face == "evade") { continue; } // This face is not valid on attack dice
          dice.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Dice/attack_" + face);
          attackResults[i] = face;
        }
        else
        {
          if (face == "hit" || face == "crit") { continue; } // These faces are not valid on defense dice
          dice.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Dice/defense_" + face);
          defenseResults[i - maxDice] = face;
        }
      }
    }

    relay.RPC("SendDiceInfo", RpcTarget.OthersBuffered, "attack", attackResults);
    relay.RPC("SendDiceInfo", RpcTarget.OthersBuffered, "defense", defenseResults);
  }

  public void RerollDice()
  {
    for (int i = 0; i < dice.transform.childCount; i++)
    {
      if (diceMarkers.transform.GetChild(i).localScale == Vector3.one)
      {
        string type = i < maxDice
            ? "attack"
            : "defense";

        string result = GetDiceResult(type);
        dice.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Dice/" + type + "_" + result);
        dice.transform.GetChild(i).localScale = Vector3.one;
        if (i < maxDice)
        {
          attackResults[i] = result;
        }
        else
        {
          defenseResults[i - maxDice] = result;
        }
      }
    }

    GetComponent<AudioSource>().PlayOneShot(diceSFX);
    relay.RPC("SendDiceInfo", RpcTarget.OthersBuffered, "attack", attackResults);
    relay.RPC("SendDiceInfo", RpcTarget.OthersBuffered, "defense", defenseResults);
  }

  public void UseFocus(string type)
  {
    string[] results = type == "attack"
        ? attackResults
        : defenseResults;

    for (int i = 0; i < maxDice; i++)
    {
      if (results[i] == "focus")
      {
        results[i] = type == "attack"
            ? "hit"
            : "evade";
      }
    }
    relay.RPC("SendDiceInfo", RpcTarget.AllBuffered, type, results);
  }

  public void ReceiveDiceInfo(string type, string[] results)
  {
    int start = type == "attack" ? 0 : maxDice;
    int end = type == "attack" ? maxDice : dice.transform.childCount;

    int counter = 0;

    for (int i = start; i < end; i++)
    {
      dice.transform.GetChild(i).GetComponent<Image>().sprite =
          Resources.Load<Sprite>("Dice/" + type + "_" + results[counter]);
      dice.transform.GetChild(i).localScale = Vector3.one;

      if (results[counter] == "null")
      {
        dice.transform.GetChild(i).localScale = Vector3.zero;
      }

      if (type == "attack")
      {
        attackResults[counter] = results[counter];
      }
      else
      {
        defenseResults[counter] = results[counter];
      }

      counter++;
    }
  }

  private string GetDiceResult(string type)
  {
    Dictionary<string, Dictionary<int, string>> faces = new Dictionary<string, Dictionary<int, string>>(){
            { "attack", new Dictionary<int, string>(){
                { 0, "blank" },
                { 1, "blank" },
                { 2, "focus" },
                { 3, "focus" },
                { 4, "hit" },
                { 5, "hit" },
                { 6, "hit" },
                { 7, "crit" },
                } },
            { "defense", new Dictionary<int, string>(){
                { 0, "blank" },
                { 1, "blank" },
                { 2, "blank" },
                { 3, "focus" },
                { 4, "focus" },
                { 5, "evade" },
                { 6, "evade" },
                { 7, "evade" },
                } },
        };

    int result = Random.Range(0, 8);

    return faces[type][result];
  }

  public void SwitchHand()
  {
    visibleDials = (visibleDials + 1) % 2;
    if (!opponentDialsLoaded && visibleDials == 1) { LoadOpponentDials(); }

    ownHand.SetActive(!ownHand.activeSelf);
    oppHand.SetActive(!oppHand.activeSelf);

    UpdateUpgrades();

    foreach (Dial dial in FindObjectsOfType<Dial>())
    {
      dial.Deselect();
    }

    switchHandsButton.GetComponentInChildren<TMP_Text>().text = visibleDials == 0
        ? "opponent"
        : "my squad";

    damageButton.SetActive(visibleDials == 0 ? true : false);
    readyButton.SetActive(visibleDials == 0 ? true : false);
  }

  public void ClearOppoonentDials(bool state)
  {
    opponentDialsLoaded = state;
  }

  private void LoadOpponentDials()
  {
    int playerNumber = controller.GetPlayerNumber();

    // Get squad from relay
    Loader.XWSquad squad = relay.GetComponent<RelayDevice>().GetSquad(playerNumber == 1 ? 2 : 1);
    if (squad.pilots.Length == 0) { return; }

    // Load dials into oppHand
    dialCount = 0;
    dialSpawn.transform.localPosition = new Vector3(-25, 15, -1);
    foreach (Loader.Pilot pilot in squad.pilots)
    {
      LoadDials(pilot, 2);
    }

    // Set opponentsDialsLoaded to true
    opponentDialsLoaded = true;
  }

  public void UpdateUpgrades()
  {
    if (visibleDials == 1)
    {
      int playerNumber = controller.GetPlayerNumber();
      Loader.Pilot[] pilots = relay.GetComponent<RelayDevice>().GetSquad(playerNumber == 1 ? 2 : 1).pilots;

      foreach (Dial dial in FindObjectsOfType<Dial>())
      {
        dial.UpdateDialName();
      }

      foreach (Loader.Pilot pilot in pilots)
      {
        GameObject.Find(pilot.name + "_dial").GetComponent<Dial>().UpdateUpgrades(pilot.upgrades.usedUpgrades);
        GameObject.Find(pilot.name + "_dial").GetComponent<Dial>().UpdateDamage(pilot.damage);
      }
    }
  }

  public void OpenSaveDisplay()
  {
    bool startingState = saveDisplay.activeSelf;
    ExitMenu();
    if (startingState == false)
    {
      saveDisplay.SetActive(true);
      fileName.text = "";
      fileName.GetComponent<InputField>().Select();
      fileName.GetComponent<InputField>().ActivateInputField();
    }
  }

  public void ConfirmSave()
  {
    if (File.Exists(SAVE_FOLDER + "/" + fileName.text + ".txt"))
    {
      confirmSave.SetActive(true);
    }
    else
    {
      SaveGame();
    }
  }

  public void SaveGame()
  {
    if (fileName.text == "")
    {
      StartCoroutine(controller.SetAlertMessage("please enter a name for the save", 5, false));
      return;
    }

    List<Loader.ShipSaveData> ships = new List<Loader.ShipSaveData>();

    foreach (Ship ship in FindObjectsOfType<Ship>())
    {
      Stats stats = ship.GetComponent<Stats>();

      Loader.ShipSaveData data = new Loader.ShipSaveData()
      {
        name = ship.name,
        id = ship.GetUniqueID(),
        position = ship.transform.position,
        angle = ship.transform.eulerAngles.z,
        hull = stats.hull,
        shield = stats.shield,
        force = stats.force,
        arcDirection = ship.GetArcDirection(),
        tokenCount = ship.GetTokenCount(),
        targetLock = ship.GetTargetLock(),
        cloakState = ship.isCloaked,
      };

      ships.Add(data);
    }

    List<Loader.HazardSaveData> hazardData = new List<Loader.HazardSaveData>();

    foreach (Hazards hazard in FindObjectsOfType<Hazards>())
    {
      Loader.HazardSaveData data = new Loader.HazardSaveData()
      {
        type = hazard.GetHazardType(),
        id = hazard.GetHazardId(),
        position = hazard.transform.position,
        angle = hazard.transform.eulerAngles.z,
      };

      hazardData.Add(data);
    }

    List<Loader.DeviceSaveData> deviceData = new List<Loader.DeviceSaveData>();

    foreach (Bomb device in FindObjectsOfType<Bomb>())
    {
      Loader.DeviceSaveData data = new Loader.DeviceSaveData()
      {
        type = device.GetDeviceType(),
        position = device.GetPosition(),
        angle = device.GetRotation(),
      };

      deviceData.Add(data);
    }

    savedGame = new Loader.SaveFile()
    {
      squad_1 = relay.GetComponent<RelayDevice>().GetSquad(1),
      squad_2 = relay.GetComponent<RelayDevice>().GetSquad(2),
      shipData = ships,
      hazards = hazardData,
      devices = deviceData,
    };

    string json = JsonUtility.ToJson(savedGame);

    if (!Directory.Exists(SAVE_FOLDER))
    {
      Directory.CreateDirectory(SAVE_FOLDER);
    }

    File.WriteAllText(SAVE_FOLDER + "/" + fileName.text.ToLower() + ".txt", json);

    StartCoroutine(controller.SetAlertMessage("game saved", 5, false));
    fileName.text = "";
    saveDisplay.SetActive(false);
    confirmSave.SetActive(false);
  }

  public void ToggleLoadMenu()
  {
    bool startingState = loadDisplay.activeSelf;

    ExitMenu();


    if (startingState == false)
    {
      loadName.text = "";
      fileList.text = "";

      if (!Directory.Exists(SAVE_FOLDER))
      {
        Directory.CreateDirectory(SAVE_FOLDER);
      }
      DirectoryInfo info = new DirectoryInfo(SAVE_FOLDER);
      FileInfo[] files = info.GetFiles();

      if (files.Length == 0)
      {
        StartCoroutine(controller.SetAlertMessage("no save files found on the system", 5, false));
        return;
      }

      foreach (FileInfo file in files)
      {
        if (file.Extension == ".txt")
        {
          fileList.text += '\n' + file.Name.Replace(".txt", "");
        }
      }

      loadDisplay.SetActive(true);

      loadName.GetComponent<InputField>().Select();
      loadName.GetComponent<InputField>().ActivateInputField();
    }

  }

  public void LoadGame()
  {
    string path = SAVE_FOLDER + "/" + loadName.text.ToLower() + ".txt";
    if (!File.Exists(path))
    {
      StartCoroutine(controller.SetAlertMessage("no file found with that name", 5, false));
      return;
    }
    loadDisplay.SetActive(false);

    string file = File.ReadAllText(path);

    try
    {
      readData = JsonUtility.FromJson<Loader.SaveFile>(file);
    }
    catch
    {
      StartCoroutine(controller.SetAlertMessage("invalid save file", 5, false));
      return;
    }

    // Delete all existing game prefabs
    relay.RPC(
        "DeleteAllObjects",
        RpcTarget.AllBuffered
    );

    // Load squads into game

    for (int i = 1; i < 3; i++)
    {
      Loader.XWSquad squad = i == 1
          ? readData.squad_1
          : readData.squad_2;

      foreach (Loader.Pilot pilot in squad.pilots)
      {
        GameObject newShip = PhotonNetwork.Instantiate("Ship",
                Vector3.zero, Quaternion.identity, 0);

        newShip.GetPhotonView().RPC("ConfigureShip", RpcTarget.AllBuffered, JsonUtility.ToJson(pilot));

        if (i == 1)
        {
          // Always one as the first hand is yours no matter what player number you are
          LoadDials(pilot, 1);
        }
      }

      // Save squads to relay 

      relay.RPC("CacheSquad", RpcTarget.AllBuffered, JsonUtility.ToJson(readData.squad_1), true);
      relay.RPC("CachePlayer2", RpcTarget.AllBuffered, JsonUtility.ToJson(readData.squad_2));
    }

    // Load in damage deck

    damageTaken = readData.squad_1.damage;
    remainingCardsInDeck = 33;
    foreach (bool card in damageTaken)
    {
      if (card) { remainingCardsInDeck--; }
    }


    // Update ships

    foreach (Loader.ShipSaveData shipData in readData.shipData)
    {
      foreach (Ship ship in FindObjectsOfType<Ship>())
      {
        if (ship.GetUniqueID() == shipData.id)
        {
          ship.gameObject.transform.position = shipData.position;
          ship.gameObject.transform.eulerAngles = new Vector3(0, 0, shipData.angle);

          ship.gameObject.GetPhotonView().RPC("LoadShipState", RpcTarget.AllBuffered, JsonUtility.ToJson(shipData));
          Debug.Log("Loaded " + shipData.name);
        }
      }
    }


    // Update upgrades

    OpenHand();
    foreach (Loader.Pilot pilot in readData.squad_1.pilots)
    {
      GameObject.Find(pilot.name + "_dial").GetComponent<Dial>().UpdateUpgrades(pilot.upgrades.usedUpgrades);
      GameObject.Find(pilot.name + "_dial").GetComponent<Dial>().UpdateDamage(pilot.damage);
    }
    OpenHand();

    // Load in hazards

    foreach (Loader.HazardSaveData hazardData in readData.hazards)
    {
      GameObject newHazard = PhotonNetwork.Instantiate("Hazard",
                  Vector3.zero, Quaternion.identity, 0);

      newHazard.transform.position = hazardData.position;
      newHazard.transform.eulerAngles = new Vector3(0, 0, hazardData.angle);

      newHazard.GetPhotonView().RPC("SetHazardImage", RpcTarget.AllBuffered, hazardData.type, hazardData.id);
    }


    // Load in devices

    foreach (Loader.DeviceSaveData deviceData in readData.devices)
    {
      GameObject newDevice = PhotonNetwork.Instantiate("Bomb",
                  Vector3.zero, Quaternion.identity, 0);

      newDevice.transform.position = deviceData.position;
      newDevice.transform.eulerAngles = new Vector3(0, 0, deviceData.angle);

      newDevice.GetPhotonView().RPC("ChangeType", RpcTarget.AllBuffered, deviceData.type);
    }


  }

  public void DeleteSave()
  {
    string path = SAVE_FOLDER + "/" + loadName.text.ToLower() + ".txt";

    if (File.Exists(path))
    {
      loadDisplay.SetActive(false);
      confirmDelete.SetActive(true);
    }
    else
    {
      StartCoroutine(controller.SetAlertMessage("file not found", 5, false));
    }
  }

  public void ConfirmDeleteSave()
  {
    string path = SAVE_FOLDER + "/" + loadName.text.ToLower() + ".txt";
    string meta = SAVE_FOLDER + "/" + loadName.text.ToLower() + ".txt.meta";
    File.Delete(path);
    File.Delete(meta);
    confirmDelete.SetActive(false);
    StartCoroutine(controller.SetAlertMessage("file deleted", 5, false));
    ToggleLoadMenu();
  }

  // OPTIONS

  public void AutoStress(bool state)
  {
    autoStress = state;
  }

  public void RangeUnit(bool state)
  {
    rangeInPX = state;
  }

  public void ShowTemplates(bool state)
  {
    showTemplates = state;
  }

  public void ChangePlayer(int playerNumber)
  {
    // Delete all dials in both hands
    for (int i = 0; i < ownHand.transform.childCount; i++)
    {
      Destroy(ownHand.transform.GetChild(i).gameObject);
    }
    for (int i = 0; i < oppHand.transform.childCount; i++)
    {
      Destroy(oppHand.transform.GetChild(i).gameObject);
    }

    Ship[] ships = FindObjectsOfType<Ship>();

    // Remove ownership of current ships
    foreach (Ship ship in ships)
    {
      ship.ownShip = false;
    }

    // If observer, dont load dials
    if (playerNumber == 0) { return; }

    controller.SetPlayerNumber(playerNumber);
    opponentDialsLoaded = false;
    dialCount = 0;
    dialSpawn.transform.localPosition = new Vector3(-25, 15);

    // Load in own dials
    Loader.XWSquad squad = relay.GetComponent<RelayDevice>().GetSquad(playerNumber);
    foreach (Loader.Pilot pilot in squad.pilots)
    {
      LoadDials(pilot, 1);

      foreach (Ship ship in ships)
      {
        if (ship.GetUniqueID() == pilot.uniqueID)
        {
          ship.ownShip = true;
        }
      }
    }
  }

  public void ChangeArcs(int option)
  {
    arcColor = option;

    Color32 color = controller.GetArcColor(option);

    byte[] colorByte = {
            color.r,
            color.g,
            color.b,
            color.a,
        };

    foreach (Ship ship in FindObjectsOfType<Ship>())
    {
      if (ship.GetComponent<Ship>().ownShip)
      {
        ship.gameObject.GetPhotonView().RPC(
            "ChangeArcColor",
            RpcTarget.AllBuffered,
            colorByte
        );
      }
    }
  }

  // REQUEST OPTIONS

  public bool GetAutoStress()
  {
    return autoStress;
  }

  public bool GetShowTemplates()
  {
    return showTemplates;
  }

  // Classes

  [System.Serializable]
  public class SubMenu
  {
    public string name;
    public GameObject menuObject;
  }
}