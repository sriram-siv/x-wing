using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Photon;
using Photon.Pun;

public class Ship : MonoBehaviour
{
  // String References for Finding GameObjects
  const string SHIPS = "Ships";

  // Config Params
  [SerializeField] GameObject shipBase;
  [SerializeField] GameObject shipBody;
  // TODO Whats this?
  Sprite[] shipSprites;
  [SerializeField] GameObject nameLabel;
  [SerializeField] GameObject selectMarker;
  [SerializeField] Token[] tokens;
  [SerializeField] GameObject firingArc;
  [SerializeField] GameObject firingArcInternal;
  [SerializeField] GameObject stackSpawn;
  [SerializeField] GameObject templateSpawn;
  [SerializeField] GameObject statsLabel;
  [SerializeField] GameObject coordsLabel;
  [SerializeField] GameObject targetLabel;
  [SerializeField] GameObject targetInput;
  [SerializeField] GameObject displayContainer;
  [SerializeField] GameObject basicTokens;
  [SerializeField] GameObject moveIndicator;
  // 
  [SerializeField] GameObject deathVFX;
  [SerializeField] PhotonView relay;
  // 

  Sprite[] colorSchemes;
  Sprite[] arcs = new Sprite[4];
  Sprite[] arcsInterior = new Sprite[4];
  List<GameObject> tokenStack = new List<GameObject>();

  // For executing moves manually
  int velocity = 1;

  // Ship Status
  bool shipActive = false;
  bool shipMoving = false;
  bool arcActive = false;
  int arcType = 1;
  int arcDirection = 0;
  bool locked = false;
  bool _isCloaked = false;
  public bool isCloaked { get { return _isCloaked; } }

  // Movement Flags
  int templateNumber = -1;
  int rotationPerformed = 0;
  int barrelDirection = -1;
  int barrelEndPos = 0;
  int lastMoveStress = 0;
  bool flipTemplate = false;
  bool dropTemplateFromFront = false;
  bool cancelTemplateDrop = false;

  // Collision Detection
  int currentCollisions = 0;
  Vector2 lastSafePosition;
  float lastSafeRotation;

  // Misc
  bool cancelExecuteMove = false;
  bool _mouseOver = false;
  public bool mouseOver { get { return _mouseOver; } }
  Vector3 mouseToCenter;
  public bool highlight { set { selectMarker.SetActive(value); } }

  ShipConfig.DialMove dialMove = new ShipConfig.DialMove()
  {
    speed = 0,
    maneuver = ShipConfig.Maneuver.NONE,
    direction = 0,
  };
  ShipTypeConfigs.Values shipTypeConfig;
  string playerName;
  string uniqueID;
  public bool ownShip = false;

  // cached references
  GameController controller;
  Menu menu;
  ActionBar actionBar;
  PhotonView photonView;

  void Start()
  {
    controller = FindObjectOfType<GameController>();
    menu = FindObjectOfType<Menu>();
    actionBar = FindObjectOfType<ActionBar>();
    photonView = gameObject.GetComponent<PhotonView>();
    playerName = photonView.Owner.NickName;
  }

  void Update()
  {
    RotateTokens();
    UpdateCoords();

    if (shipActive && !shipMoving && !targetInput.activeSelf && relay != null)
    {
      Controls();
      ExecuteMovement();
      ToggleTokens();
      AdjustStats();
    }

    if (!relay)
    {
      Debug.Log("searching for relay");
      relay = FindObjectOfType<RelayDevice>().gameObject.GetPhotonView();
    }
  }

  // MOVEMENT
  private void ExecuteMovement()
  {
    if (menu.isManualMode)
    {
      ManualMovement();
      return;
    }
    if (arcActive) return;
    // Movement Controls
    string message = FindObjectOfType<Loader>().GetPlayerName() + " performed a manual move (" + name + ")";

    float angle = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
        ? 90f
        : 45f;

    if (Input.GetKeyDown(KeyCode.DownArrow))
    {
      KTurn();
      relay.RPC("SendAlertMessage", RpcTarget.AllBuffered, message, 5);
    }
    else if (Input.GetKeyDown(KeyCode.UpArrow))
    {
      StartCoroutine(Forward(velocity));
      relay.RPC("SendAlertMessage", RpcTarget.AllBuffered, message, 5);
    }
    else if (Input.GetKeyDown(KeyCode.LeftArrow))
    {
      StartCoroutine(Turn(angle, Mathf.Clamp(velocity - 1, 0, 2), 1));
      relay.RPC("SendAlertMessage", RpcTarget.AllBuffered, message, 5);
    }
    else if (Input.GetKeyDown(KeyCode.RightArrow))
    {
      StartCoroutine(Turn(angle, Mathf.Clamp(velocity - 1, 0, 2), -1));
      relay.RPC("SendAlertMessage", RpcTarget.AllBuffered, message, 5);
    }
  }

  private void KTurn()
  {
    controller.LogMove(uniqueID, transform.position, transform.eulerAngles.z);

    int angle = 180;
    // Turn the ship 90deg if the shift key is pressed
    if (Input.GetKey(KeyCode.LeftShift)) { angle = 90; }
    if (Input.GetKey(KeyCode.RightShift)) { angle = -90; }

    ResetMovementFlags();
    transform.Rotate(0, 0, angle);
  }

  /// <param name="direction">0: Forward, 1: left, 2: right</param>
  public void Boost(int direction)
  {
    if (direction == 0) StartCoroutine(Forward(1));
    if (direction == 1) StartCoroutine(Turn(45f, 0, 1));
    if (direction == 2) StartCoroutine(Turn(45f, 0, -1));
  }

  public void BarrelRoll(string direction, string position)
  {
    controller.LogMove(uniqueID, transform.position, transform.eulerAngles.z);
    ResetMovementFlags();

    float lateralMovement = -shipTypeConfig.barrel[0];
    if (direction == "right") lateralMovement = shipTypeConfig.barrel[0];

    // End position adjustment
    float adjust = 0;
    if (position == "forward") adjust = shipTypeConfig.barrel[1];
    if (position == "back") adjust = -shipTypeConfig.barrel[1];

    transform.Translate(new Vector3(lateralMovement, adjust));

    CheckForSafety();

    templateNumber = 0;
    barrelDirection = direction == "left" ? 1 : 2; // 0 = forward as per cloaking
    if (position == "forward") barrelEndPos = 1;
    if (position == "back") barrelEndPos = 2;
  }

  public void Cloak()
  {
    if (isCloaked)
    {
      actionBar.ToggleBar("cloak");
    }
    else
    {
      _isCloaked = true;
      gameObject.GetPhotonView().RPC("ApplyCloakEffect", RpcTarget.AllBuffered, true);
    }
  }

  public void Decloak(int[] vals)
  {
    int curve = vals[0];
    int direction = vals[1];
    int position = vals[2];

    // TODO prevent this in ActionBar
    // ensure bank template isn't used for any calculations beyond small ships
    if (shipTypeConfig.size != "small") { curve = 0; }

    float angle = 0;
    if (curve == 1) angle = 45;
    if (curve == 2) angle = -45;

    // Decloak forward
    if (direction == 0)
    {
      int speed = shipTypeConfig.size == "small" ? 2 : 1;
      switch (curve)
      {
        case 0:
          StartCoroutine(Forward(speed));
          break;
        case 1:
          StartCoroutine(Turn(45, 1, 1, 0));
          break;
        case 2:
          StartCoroutine(Turn(45, 1, -1, 0));
          break;
        default:
          break;
      }
    }
    else
    {
      controller.LogMove(uniqueID, transform.position, transform.eulerAngles.z);
      ResetMovementFlags();

      Vector3 vector = shipTypeConfig.cloak[curve];
      if (direction == 2) vector = -vector;

      transform.Translate(vector);
      transform.Rotate(Vector3.forward * angle);

      // End pos adjustment
      float adjust = 0;
      if (position == 1) adjust = shipTypeConfig.barrel[1];
      if (position == 2) adjust = -shipTypeConfig.barrel[1];
      transform.Translate(new Vector3(0, adjust));

      CheckForSafety();

      // Flags
      templateNumber = 0;
      if (shipTypeConfig.size == "small") templateNumber = 1;
      if (curve > 0) templateNumber = 6;
      if (curve == 2) flipTemplate = true;
      barrelDirection = direction;
      // Make sure no adjustment is made for forward decloaking
      if (direction > 0) barrelEndPos = position;
    }

    photonView.RPC("ApplyCloakEffect", RpcTarget.AllBuffered, false);
  }

  [PunRPC]
  private void ApplyCloakEffect(bool cloakState)
  {
    shipBody.GetComponent<SpriteRenderer>().color = cloakState
        ? new Color32(70, 50, 255, 150)
        : new Color32(255, 255, 255, 255);
  }

  IEnumerator Forward(int moveVelocity, int endRotation = 0, bool reverse = false)
  {
    controller.LogMove(uniqueID, transform.position, transform.eulerAngles.z, lastMoveStress);
    ResetMovementFlags();
    shipMoving = true;

    // TODO remove factor4
    // But it does give extra accuracy in collisions..
    float distance = (0.1f / moveVelocity) * (shipTypeConfig.movement.forward[moveVelocity - 1] / 4);

    for (int i = 0; i < (40 * moveVelocity); i++)
    {
      Vector3 trans = controller.TransformVectorByAngle(distance, transform.eulerAngles.z + 90);
      transform.position += reverse
          ? -trans
          : trans;

      CheckForSafety();
      yield return new WaitForEndOfFrame();
    }

    transform.Rotate(0, 0, endRotation);

    rotationPerformed = endRotation;
    dropTemplateFromFront = reverse || endRotation == 180;
    templateNumber = moveVelocity - 1;

    shipMoving = false;

    if (menu.GetShowTemplates())
    {
      // TODO extract template drop into separate method and call it here and on Turn()
    }
  }

  // Speed parameter is input as 1 less than move speed
  IEnumerator Turn(float angle, int speed, int direction, int endRotation = 0, bool reverse = false)
  {
    controller.LogMove(uniqueID, transform.position, transform.eulerAngles.z, lastMoveStress);
    ResetMovementFlags();
    shipMoving = true;

    // Get the turn radius for this ship size and determine the pivot and step increment
    // The pivot point starts from the front of the ship and moves smoothly to the back
    float radius = shipTypeConfig.movement.bank[Mathf.Clamp(speed, 0, 2)];
    if (angle == 90) radius = shipTypeConfig.movement.turn[Mathf.Clamp(speed, 0, 2)];
    float angleIncrement = angle / 100f;
    float pivot = shipTypeConfig.width / 2;
    if (reverse) pivot *= -1;

    for (int i = 0; i < 100; i++)
    {
      // The angle is either 90 deg forward or 90 deg back
      float pivotAngle = 90 * Mathf.Sign(pivot);
      float pivotMagnitude = Mathf.Abs(pivot);

      // This code moves the ship so that it appears to rotate around the pivot point when it is later rotated
      Vector3 pivIn = controller.TransformVectorByAngle(pivotMagnitude, pivotAngle + angleIncrement + transform.eulerAngles.z);
      Vector3 pivOut = controller.TransformVectorByAngle(pivotMagnitude, pivotAngle + transform.eulerAngles.z);
      transform.position += (pivOut - pivIn) * direction;

      // Movement along the arc (in steps of angleIncrement)
      // Length of chord and translation vector
      float incrementRadians = angleIncrement * Mathf.Deg2Rad;
      float chord = Mathf.Sin(incrementRadians / 2) * 2 * radius;
      Vector3 trans = controller.TransformVectorByAngle(chord, (180 - angleIncrement) / 2);

      if (reverse) { trans = -trans; }

      // Move and rotate
      transform.Translate(new Vector3(-trans.x * direction, trans.y));
      transform.Rotate(0, 0, angleIncrement * direction);

      // Shift the pivot along the y axis towards the other side
      float pivotAdjust = shipTypeConfig.width / 100;
      if (reverse) pivotAdjust *= -1;
      pivot -= pivotAdjust;

      CheckForSafety();
      yield return new WaitForEndOfFrame();
    }

    transform.Rotate(0, 0, endRotation * direction);

    rotationPerformed = endRotation * direction;
    dropTemplateFromFront = reverse || endRotation == 180;
    templateNumber = angle == 90
        ? speed + 8
        : speed + 5;
    if (direction == -1) flipTemplate = true;
    barrelDirection = -1;
    shipMoving = false;
  }

  private void ManualMovement()
  {
    // Rotation
    if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
    {
      Vector3 angleIncrement = Input.GetKey(KeyCode.LeftControl)
          ? Vector3.forward
          : Vector3.forward * 15;

      if (Input.GetKeyDown(KeyCode.LeftArrow))
      {
        transform.eulerAngles += angleIncrement;
      }
      else if (Input.GetKeyDown(KeyCode.RightArrow))
      {
        transform.eulerAngles -= angleIncrement;
      }
    }
    // Translation
    else if (Input.GetKeyDown(KeyCode.UpArrow))
    {
      transform.position += Vector3.up / 10;
    }
    else if (Input.GetKeyDown(KeyCode.DownArrow))
    {
      transform.position += Vector3.down / 10;
    }
    else if (Input.GetKeyDown(KeyCode.LeftArrow))
    {
      transform.position += Vector3.left / 10;
    }
    else if (Input.GetKeyDown(KeyCode.RightArrow))
    {
      transform.position += Vector3.right / 10;
    }
  }

  // CONFIG

  public void SetVelocity(int newVelocity)
  {
    velocity = newVelocity;
  }

  [PunRPC]
  private void ToggleArc()
  {
    arcActive = !arcActive;
    arcType = 1;

    firingArc.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = arcs[arcType];
    firingArc.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = arcs[arcType];

    firingArc.SetActive(arcActive);
  }

  [PunRPC]
  private void MoveArc(int direction)
  {
    arcDirection += direction + 4; // So mod operation will work on negatives
    arcDirection %= 4;

    firingArc.transform.localEulerAngles = new Vector3(0, 0, 90 * arcDirection);

    firingArcInternal.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = arcsInterior[arcDirection];
    firingArcInternal.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = arcsInterior[(arcDirection + 2) % 4];

    ToggleSideArc();
  }

  [PunRPC]
  private void ChangeArcType(int direction)
  {
    arcType += direction + 3; // So that mod operation will work with negatives 
    arcType %= 3;

    firingArc.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = arcs[arcType];
    firingArc.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = arcs[arcType];

    ToggleSideArc();
  }

  // Angles are different for broadside arcs so this method makes sure the right sprite is used
  private void ToggleSideArc()
  {
    if (arcType == 1)
    {
      firingArc.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = arcDirection % 2 == 1
          ? arcs[3]
          : arcs[1];
      firingArc.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = arcDirection % 2 == 1
          ? arcs[3]
          : arcs[1];
    }
  }

  private void ToggleTokens()
  {
    if (Input.GetKeyDown(KeyCode.Space))
    {
      photonView.RPC("ToggleArc", RpcTarget.AllBuffered);
    }
    if (arcActive)
    {
      if (Input.GetKeyDown(KeyCode.UpArrow))
      {
        photonView.RPC("ChangeArcType", RpcTarget.AllBuffered, 1);
      }
      else if (Input.GetKeyDown(KeyCode.DownArrow))
      {
        photonView.RPC("ChangeArcType", RpcTarget.AllBuffered, -1);
      }
      else if (Input.GetKeyDown(KeyCode.LeftArrow))
      {
        photonView.RPC("MoveArc", RpcTarget.AllBuffered, 1);
      }
      else if (Input.GetKeyDown(KeyCode.RightArrow))
      {
        photonView.RPC("MoveArc", RpcTarget.AllBuffered, -1);
      }
    }

    int increment = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
        ? -1
        : 1;

    if (Input.GetKeyDown(KeyCode.F))
    {
      photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "focus", increment);
    }
    else if (Input.GetKeyDown(KeyCode.E))
    {
      photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "evade", increment);
    }
    /*else if (Input.GetKeyDown(KeyCode.S))
    {
        photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "stress", increment);
    }*/
    else if (Input.GetKeyDown(KeyCode.C))
    {
      photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "calculate", increment);
    }

    if (Input.GetKeyDown(KeyCode.T))
    {
      ToggleTargetLock();
    }

    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.X))
    {
      photonView.RPC("ClearTokens", RpcTarget.AllBuffered);
    }
  }

  public void ToggleTargetLock()
  {
    locked = !locked;
    if (locked)
    {
      targetInput.SetActive(true);
      InputField input = targetInput.GetComponentInChildren<InputField>();
      input.Select();
      input.ActivateInputField();
    }
    else
    {
      photonView.RPC("SyncTargetLock", RpcTarget.AllBuffered, "/null");
    }
  }

  // This is a separate method so that it can be called from the InputField
  public void SetTargetLock(string target)
  {
    targetInput.SetActive(false);
    cancelExecuteMove = true;

    if (target == "")
    {
      locked = false;
      return;
    }

    photonView.RPC("SyncTargetLock", RpcTarget.AllBuffered, target);
    targetInput.GetComponentInChildren<InputField>().text = "";
  }
  // TODO can this be rolled into the above method?
  [PunRPC]
  private void SyncTargetLock(string target)
  {
    if (target == "/null") { target = ""; }

    targetLabel.GetComponentInChildren<TMP_Text>().text = target;
    targetLabel.SetActive(target == ""
        ? false : true);
  }

  public void DropBomb(int[] dropOptions)
  {
    // Cancel bomb drop if invalid template selected
    if (dropOptions[0] > 2 && dropOptions[1] > 0) { return; }

    var newBomb = PhotonNetwork.Instantiate("Bomb", transform.position, transform.rotation, 0);

    float shipHalfLength = shipTypeConfig.width / 2;
    float dropDistance = shipHalfLength + 5.42f;

    // TODO use normal movement on bomb
    Vector3 trans = controller.TransformVectorByAngle(dropDistance, transform.eulerAngles.z + 270);
    newBomb.transform.position += trans;

    newBomb.GetComponent<Bomb>().DropPosition(dropOptions);
  }

  private void RotateTokens()
  {
    displayContainer.transform.eulerAngles = Vector3.zero;
  }

  private void AdjustStats()
  {
    // TODO is this ref unused?
    Stats stats = GetComponent<Stats>();
    if (Input.GetKeyDown(KeyCode.Comma))
    {
      if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
      {
        gameObject.GetPhotonView().RPC("SetShield", RpcTarget.AllBuffered, -1);
      }
      else if (Input.GetKey(KeyCode.LeftControl))
      {
        gameObject.GetPhotonView().RPC("SetForce", RpcTarget.AllBuffered, -1);
      }
      else
      {
        gameObject.GetPhotonView().RPC("SetHull", RpcTarget.AllBuffered, -1);
      }
    }
    if (Input.GetKeyDown(KeyCode.Period))
    {
      if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
      {
        gameObject.GetPhotonView().RPC("SetShield", RpcTarget.AllBuffered, 1);
      }
      else if (Input.GetKey(KeyCode.LeftControl))
      {
        gameObject.GetPhotonView().RPC("SetForce", RpcTarget.AllBuffered, 1);
      }
      else
      {
        gameObject.GetPhotonView().RPC("SetHull", RpcTarget.AllBuffered, 1);
      }
    }
  }

  private void UpdateCoords()
  {
    string x = transform.position.x.ToString("n1");
    string y = transform.position.y.ToString("n1");
    string angle = Mathf.Round(transform.eulerAngles.z).ToString();

    coordsLabel.transform.GetChild(0).GetComponent<TMP_Text>().text = x + ", " + y + ", " + angle;
  }

  public void ToggleCoords()
  {
    coordsLabel.SetActive(!coordsLabel.activeSelf);
  }

  private void SelectShip()
  {
    if (menu.CheckPlayAreaActive())
    {
      menu.CloseMainMenu();

      if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
      {
        Ship[] otherShips = FindObjectsOfType<Ship>();
        foreach (Ship ship in otherShips)
        {
          ship.DeselectShip();
        }
      }
      shipActive = true;
      selectMarker.SetActive(true);

      actionBar.attachedShip = this;
      actionBar.ToggleBar("collapsed");

      Hazards[] hazards = FindObjectsOfType<Hazards>();
      foreach (Hazards hazard in hazards) { hazard.Deselect(); }

      Bomb[] bombs = FindObjectsOfType<Bomb>();
      foreach (Bomb bomb in bombs) { bomb.Deselect(); }
    }
  }

  [PunRPC]
  private void DestroyShip()
  {
    var explosion = Instantiate(deathVFX, transform.position, Quaternion.identity);
    Destroy(explosion, 1f);
    controller.LogMove(uniqueID, transform.position, transform.eulerAngles.z);
    transform.position = new Vector3(0, -20, 0);
  }

  private void OnMouseDrag()
  {
    if (menu.isManualMode && !menu.CheckOpenHand() && !menu.CheckMenuOpen())
    {
      Vector3 trans =
        Camera.main.ScreenToWorldPoint(Input.mousePosition) - mouseToCenter;
      // Rounds to 1 decimal point
      float xPos = Mathf.Round(trans.x * 10) / 10;
      float yPos = Mathf.Round(trans.y * 10) / 10;
      transform.position = new Vector3(xPos + 0.0111f, yPos, -1);
    }
  }

  private void OnMouseDown()
  {
    SelectShip();
    mouseToCenter = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
    gameObject.GetPhotonView().RequestOwnership();
  }

  private void OnMouseOver()
  {
    nameLabel.SetActive(true);
    _mouseOver = true;
  }

  private void OnMouseExit()
  {
    _mouseOver = false;
    nameLabel.SetActive(false);
  }

  public void DeselectShip()
  {
    shipActive = false;
    selectMarker.SetActive(false);
  }

  // TODO use getter
  public int GetArcDirection()
  {
    return arcDirection;
  }
  // TODO use getter
  public string GetUniqueID()
  {
    return uniqueID;
  }

  [PunRPC]
  private void RenameShip(string newName)
  {
    gameObject.name = newName;
    nameLabel.GetComponent<TMP_Text>().text = newName;
  }

  [PunRPC]
  public void LoadShipState(string data)
  {
    Loader.ShipSaveData shipData = JsonUtility.FromJson<Loader.ShipSaveData>(data);

    // Load all tokens and status effects
    for (int i = 0; i < tokens.Length; i++)
    {
      if (!tokens[i].stackable && shipData.tokenCount[i] == 1)
      {
        gameObject.GetPhotonView().RPC("AdjustTokens", RpcTarget.AllBuffered, tokens[i].name, 0);
      }
      else if (tokens[i].stackable && shipData.tokenCount[i] > 0)
      {
        gameObject.GetPhotonView().RPC("AdjustTokens", RpcTarget.AllBuffered, tokens[i].name, shipData.tokenCount[i]);
      }
    }

    // Load stats
    GetComponent<Stats>().SetAllStats(shipData.hull, shipData.shield, shipData.force);

    // Load cloak state
    if (shipData.cloakState)
    {
      Cloak();
    }

    // Load arc direction
    gameObject.GetPhotonView().RPC("MoveArc", RpcTarget.AllBuffered, shipData.arcDirection);

    // Load target lock
    if (shipData.targetLock != "")
    {
      gameObject.GetPhotonView().RPC("SyncTargetLock", RpcTarget.AllBuffered, shipData.targetLock);
    }
  }

  public int[] GetTokenCount()
  {
    int[] vals = new int[tokens.Length];

    for (int i = 0; i < tokens.Length; i++)
    {
      if (tokens[i].stackable)
      {
        vals[i] = tokens[i].count;
      }
      // TODO can count be added to non-stacking tokens?
      else // Represent active state as 1 (true) or 0 (false)
      {
        vals[i] = tokens[i].token.activeSelf
            ? 1
            : 0;
      }
    }

    return vals;
  }

  public string GetTargetLock()
  {
    return targetLabel.GetComponentInChildren<TMP_Text>().text;
  }

  // TODO use getter
  public Sprite[] GetColorShemes()
  {
    return colorSchemes;
  }

  public void ChangeColorScheme(int newColor)
  {
    shipBody.GetComponent<SpriteRenderer>().sprite = colorSchemes[newColor];
  }

  // COLLISION

  private void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.GetComponent<Ship>())
    {
      currentCollisions++;
    }
  }

  private void OnTriggerExit2D(Collider2D collision)
  {
    if (collision.GetComponent<Ship>())
    {
      currentCollisions--;
    }
  }

  private void CheckForSafety()
  {
    if (currentCollisions <= 0)
    {
      lastSafePosition = new Vector2(transform.position.x, transform.position.y);
      lastSafeRotation = transform.eulerAngles.z;
    }
  }

  [PunRPC]
  private void MoveToSafety()
  {
    if (templateNumber == -1) return;

    templateSpawn.transform.localPosition = new Vector3(0, -0.5f) * shipTypeConfig.width;
    templateSpawn.transform.localEulerAngles = Vector3.zero;

    switch (barrelDirection)
    {
      case 1:
        templateSpawn.transform.localPosition =
          new Vector3(0.5f, 0) * shipTypeConfig.width;
        break;
      case 2:
        templateSpawn.transform.localPosition =
          new Vector3(-0.5f, 0) * shipTypeConfig.width;
        break;
      default:
        break;
    }
    switch (barrelEndPos)
    {
      case 1:
        templateSpawn.transform.localPosition +=
            Vector3.down * shipTypeConfig.barrel[1];
        break;
      case 2:
        templateSpawn.transform.localPosition +=
            Vector3.up * shipTypeConfig.barrel[1];
        break;
      default:
        break;
    }
    // TODO refactor this to be more clear that it links to above blocks
    if (shipTypeConfig.size != "small" && barrelDirection != -1)
    {
      float lateralAdjust = barrelDirection == 1
          ? 1
          : -1;
      templateSpawn.transform.localPosition += new Vector3(lateralAdjust, 2);
    }

    if (dropTemplateFromFront)
    {
      templateSpawn.transform.localPosition = new Vector3(0, 0.5f) * shipTypeConfig.width;
      templateSpawn.transform.localEulerAngles = Vector3.forward * 180;
    }

    GameObject dropTemplate = PhotonNetwork.Instantiate("Template",
        templateSpawn.transform.position, templateSpawn.transform.rotation, 0);
    dropTemplate.GetPhotonView().RPC("InitTemplate", RpcTarget.AllBuffered, templateNumber, flipTemplate);

    if (shipTypeConfig.size == "small")
    {
      switch (barrelDirection)
      {
        case 1:
          dropTemplate.transform.Rotate(Vector3.forward * 90);
          break;
        case 2:
          dropTemplate.transform.Rotate(Vector3.forward * -90);
          break;
        default:
          break;
      }
    }

    if (lastSafePosition != new Vector2(transform.position.x, transform.position.y))
    {
      controller.LogMove(uniqueID, transform.position, transform.eulerAngles.z, 0);
      ResetMovementFlags();

      float rotation = lastSafeRotation - transform.eulerAngles.z;
      transform.position = lastSafePosition;
      transform.eulerAngles = new Vector3(
        transform.eulerAngles.x,
        transform.eulerAngles.y,
        lastSafeRotation
      );

      cancelTemplateDrop = true;
    }
  }

  public void SetMoveFromDial(ShipConfig.DialMove move)
  {
    dialMove = move;

    if (dialMove.difficulty == "") { dialMove.difficulty = "white"; }

    moveIndicator.GetComponent<SpriteRenderer>().sprite = dialMove.maneuver == ShipConfig.Maneuver.NONE
        ? null
        : Resources.Load<Sprite>(
            "Indicators/" + dialMove.maneuver + "-" + dialMove.difficulty.ToString().ToLower());
    moveIndicator.GetComponent<SpriteRenderer>().flipX = dialMove.direction == -1
        ? true
        : false;
  }

  // TODO use setter
  public void ToggleMoveIndicator(bool state)
  {
    moveIndicator.SetActive(state);
  }

  // CLASSES

  [Serializable]
  public class Token
  {
    public GameObject token;
    public string name;
    public bool resetOnRoundEnd;
    public bool stackable; // Badly named at the moment, as it means they can be held as mulitples but they dont exist in the token stack
    public int count = 0;
    public bool statusEffect;
  }

  // PHOTON METHODS

  [PunRPC]
  public void ConfigureShip(string json)
  {
    Loader.Pilot pilot = JsonUtility.FromJson<Loader.Pilot>(json);
    pilot.config = FindObjectOfType<Loader>().GetConfigFile(pilot.ship, pilot.name);

    colorSchemes = pilot.config.colorSchemes;


    gameObject.name = pilot.name;
    nameLabel.GetComponent<TMP_Text>().text = pilot.name;
    uniqueID = pilot.uniqueID;

    // Set stats from config file
    Stats stats = GetComponent<Stats>();
    stats.SetHull(pilot.config.hull);
    stats.SetShield(pilot.config.shield);
    stats.SetInitiative(pilot.initiative);
    stats.SetForce(pilot.force);
    stats.SetCharge(pilot.charges);

    controller = FindObjectOfType<GameController>();
    string size = pilot.config.size;

    shipBase.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Bases/base-" + size);
    shipBody.GetComponent<SpriteRenderer>().sprite = colorSchemes[0]; //Resources.Load<Sprite>("Ships/" + pilot.config.name);

    // Get all firing arcs
    arcs[0] = Resources.Load<Sprite>("Arcs/arc-" + "bullseye");
    arcs[1] = Resources.Load<Sprite>("Arcs/arc-front-" + size);
    arcs[2] = Resources.Load<Sprite>("Arcs/arc-full-" + size);
    arcs[3] = Resources.Load<Sprite>("Arcs/arc-side-" + size);

    arcsInterior[0] = Resources.Load<Sprite>("Arcs/Interior/arc-" + size + "_0");
    arcsInterior[1] = Resources.Load<Sprite>("Arcs/Interior/arc-" + size + "_1");
    arcsInterior[2] = Resources.Load<Sprite>("Arcs/Interior/arc-" + size + "_2");
    arcsInterior[3] = Resources.Load<Sprite>("Arcs/Interior/arc-" + size + "_3");

    firingArcInternal.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = arcsInterior[0];
    firingArcInternal.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = arcsInterior[2];

    // Select firing arc color
    Color32 arcColor = controller.GetArcColor(pilot.faction);

    firingArc.transform.GetChild(0).GetComponent<SpriteRenderer>().color = arcColor;
    firingArc.transform.GetChild(1).GetComponent<SpriteRenderer>().color = arcColor;
    firingArcInternal.transform.GetChild(0).GetComponent<SpriteRenderer>().color = arcColor;
    firingArcInternal.transform.GetChild(1).GetComponent<SpriteRenderer>().color = arcColor;

    if (pilot.config.arcs == 2)
    {
      firingArc.transform.GetChild(1).gameObject.SetActive(true);
      firingArcInternal.transform.GetChild(1).gameObject.SetActive(true);
      firingArc.transform.GetChild(1).transform.localPosition += new Vector3(0, -0.5f) * shipTypeConfig.width;
    }

    // TODO move this up to top of block
    shipTypeConfig = JsonUtility.FromJson<ShipTypeConfigs>(
        Resources.Load<TextAsset>("Config/ShipTypeConfigs").ToString()
    )[size];

    // Configure ship size variables
    selectMarker.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("UI/ship-marker-" + shipTypeConfig.size);
    firingArc.transform.GetChild(0).localPosition = shipTypeConfig.objects.arc;
    templateSpawn.transform.localPosition = shipTypeConfig.objects.template;
    statsLabel.transform.localPosition = shipTypeConfig.objects.stats;
    coordsLabel.transform.localPosition = shipTypeConfig.objects.coords;
    stackSpawn.transform.localPosition = shipTypeConfig.objects.status;
    basicTokens.transform.localPosition = shipTypeConfig.objects.basic;
    tokens[4].token.transform.localPosition = shipTypeConfig.objects.stress;
    tokens[2].token.transform.localPosition = shipTypeConfig.objects.reinforceFore;
    tokens[3].token.transform.localPosition = shipTypeConfig.objects.reinforceAft;
    targetLabel.transform.localPosition = shipTypeConfig.objects.target;

    GetComponent<BoxCollider2D>().size = new Vector2(shipTypeConfig.width, shipTypeConfig.width);

    transform.SetParent(GameObject.Find(SHIPS).transform);
  }

  [PunRPC]
  private void ChangeArcColor(byte[] color)
  {
    Color32 arcColor = new Color32(color[0], color[1], color[2], color[3]);
    firingArc.transform.GetChild(0).GetComponent<SpriteRenderer>().color = arcColor;
    firingArc.transform.GetChild(1).GetComponent<SpriteRenderer>().color = arcColor;
    firingArcInternal.transform.GetChild(0).GetComponent<SpriteRenderer>().color = arcColor;
    firingArcInternal.transform.GetChild(1).GetComponent<SpriteRenderer>().color = arcColor;
  }

  [PunRPC]
  private void AdjustTokens(string token_name, int increment)
  {
    Token token_type = Array.Find(tokens, token => token.name == token_name);

    if (token_type.stackable)
    {
      token_type.count += increment;
      int maxCount = 9;
      if (token_type.name == "tractor" || token_type.name == "ion") maxCount = 3;
      token_type.count = Mathf.Clamp(token_type.count, 0, maxCount);

      TMP_Text countText = token_type.token.transform.GetChild(0).GetComponent<TMP_Text>();
      GameObject countBG = token_type.token.transform.GetChild(1).gameObject;

      token_type.token.SetActive(token_type.count == 0
          ? false
          : true);
      countBG.SetActive(token_type.count > 1);
      countText.text = token_type.count > 1
          ? token_type.count.ToString()
          : "";
    }
    else
    {
      token_type.token.SetActive(!token_type.token.activeSelf);
    }

    if (token_type.statusEffect)
    {
      // Second condition stops the token being added if the count is being incremented on an already existing token
      if (token_type.token.activeSelf && !tokenStack.Contains(token_type.token))
      {
        tokenStack.Add(token_type.token);
      }
      else if (!token_type.token.activeSelf)
      {
        tokenStack.Remove(token_type.token);
      }

      RestackTokens();
    }
  }

  private void RestackTokens()
  {
    Vector3 spawn = stackSpawn.transform.localPosition;
    int count = 0;
    foreach (GameObject token in tokenStack)
    {
      token.transform.localPosition = spawn;
      count++;
      spawn += new Vector3(0, -1.3f, -0.1f);
      if (count == 3)
      {
        spawn += new Vector3(-1.9f, 3.9f, 0);
      }
    }
  }

  [PunRPC]
  public void ClearTokens()
  {
    foreach (Token token_type in tokens)
    {
      if (token_type.resetOnRoundEnd)
      {
        token_type.token.SetActive(false);
        token_type.count = 0;
        tokenStack.Remove(token_type.token);
      }
    }
    RestackTokens();
  }

  // TODO move controls definitions to separate file - scriptable object?
  private void Controls()
  {
    bool destroy = Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D);
    bool executeMove = Input.GetKeyDown(KeyCode.Return);
    bool moveToSafety = Input.GetKeyDown(KeyCode.Backspace);
    bool barrelRoll = Input.GetKeyDown(KeyCode.B);
    bool toggleCoords = Input.GetKeyDown(KeyCode.Slash);

    int ionThreshold = 1;
    if (shipTypeConfig.width == 6) ionThreshold = 2;
    if (shipTypeConfig.width == 8) ionThreshold = 3;
    ShipConfig.DialMove cachedMove = dialMove;

    if (toggleCoords)
    {
      ToggleCoords();
    }

    if (destroy)
    {
      photonView.RPC("DestroyShip", RpcTarget.AllBuffered);
    }

    if (executeMove)
    {
      if (cancelExecuteMove)
      {
        cancelExecuteMove = false;
        return;
      }

      if (tokens[7].count >= ionThreshold)
      {
        dialMove.difficulty = "blue";
        dialMove.maneuver = ShipConfig.Maneuver.FORWARD;
        dialMove.speed = 1;
        photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "ion", -tokens[7].count);
      }

      if (menu.GetAutoStress())
      {
        switch (dialMove.difficulty)
        {
          case "blue":
            if (tokens[4].count > 0)
            {
              lastMoveStress = -1;
            }
            photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "stress", -1);
            break;
          case "red":
            if (tokens[4].count > 0)
            {
              string messageIllegal = FindObjectOfType<Loader>().GetPlayerName() + " peformed a red maneuver while stressed (" + name + ")";
              relay.RPC("SendAlertMessage", RpcTarget.AllBuffered, messageIllegal, 5);
            }
            photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "stress", 1);
            lastMoveStress = 1;
            break;
          default:
            lastMoveStress = 0;
            break;
        }
      }

      switch (dialMove.maneuver)
      {
        case ShipConfig.Maneuver.STOP:
          break;
        case ShipConfig.Maneuver.FORWARD:
          StartCoroutine(Forward(dialMove.speed));
          break;
        case ShipConfig.Maneuver.BANK:
          StartCoroutine(Turn(45, dialMove.speed - 1, dialMove.direction, 0));
          break;
        case ShipConfig.Maneuver.TURN:
          StartCoroutine(Turn(90, dialMove.speed - 1, dialMove.direction, 0));
          break;
        case ShipConfig.Maneuver.SEGNOR:
          StartCoroutine(Turn(45, dialMove.speed - 1, dialMove.direction, 180));
          break;
        case ShipConfig.Maneuver.TALLON:
          StartCoroutine(Turn(90, dialMove.speed - 1, dialMove.direction, 90));
          break;
        case ShipConfig.Maneuver.KTURN:
          StartCoroutine(Forward(dialMove.speed, 180));
          break;
        case ShipConfig.Maneuver.REVERSE:
          StartCoroutine(Forward(dialMove.speed, 0, true));
          break;
        case ShipConfig.Maneuver.REVERSE_BANK:
          StartCoroutine(Turn(45, dialMove.speed - 1, -dialMove.direction, 0, true));
          break;
        default:
          Debug.Log("No valid move selected from dial");
          break;
      }

      if (dialMove.maneuver != ShipConfig.Maneuver.NONE)
      {
        string messageManeuver = name + " performed the maneuver " + dialMove.speed + " " + dialMove.maneuver;
        FindObjectOfType<RelayDevice>().gameObject.GetPhotonView().RPC(
            "SendAlertMessage",
            RpcTarget.AllBuffered,
            messageManeuver,
            5
        );
      }

    }

    if (moveToSafety)
    {
      photonView.RPC("MoveToSafety", RpcTarget.AllBuffered);
    }

    if (barrelRoll)
    {
      actionBar.ToggleBar("barrel");
    }

    dialMove = cachedMove;
  }

  [PunRPC]
  public void ResetMovementFlags()
  {
    templateNumber = -1;
    rotationPerformed = 0;
    barrelDirection = -1;
    barrelEndPos = 0;
    lastMoveStress = 0;
    flipTemplate = false;
    dropTemplateFromFront = false;
  }
}
