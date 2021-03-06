﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Photon;
using Photon.Pun;

public class Ship : MonoBehaviour
{
    // config params

    [SerializeField] GameObject shipBase;
    [SerializeField] GameObject shipBody;
    Sprite[] shipSprites;
    [SerializeField] GameObject nameLabel;
    [SerializeField] GameObject selectMarker;

    Sprite[] arcs = new Sprite[4];
    Sprite[] arcsInterior = new Sprite[4];
    [SerializeField] GameObject firingArc;
    [SerializeField] GameObject firingArcInternal;
    [SerializeField] Token[] tokens;

    List<GameObject> tokenStack = new List<GameObject>();
    [SerializeField] GameObject stackSpawn;
    [SerializeField] GameObject templateSpawn;

    [SerializeField] GameObject statsLabel;
    [SerializeField] GameObject coordsLabel;
    [SerializeField] GameObject targetLabel;
    [SerializeField] GameObject targetInput;
    [SerializeField] GameObject displayContainer;
    [SerializeField] GameObject basicTokens;
    [SerializeField] GameObject moveIndicator;

    [SerializeField] GameObject deathVFX;

    const string SHIPS = "Ships";

    float shipSize;

    // state variables

    int velocity = 1;
    int arcType = 1;
    int arcDirection = 0;

    bool shipActive = false;
    bool shipMoving = false;
    bool arcActive = false;
    bool statsToggle = true;

    int rotationPerformed = 0;
    bool reversePerformed = false;
    int barrelDirection = -1;
    int barrelEndPos = 0;
    bool cancelTemplateDrop = false;
    bool locked = false;
    bool cloaked = false;

    int currentCollisions = 0;
    Vector2 lastSafePosition = new Vector2(0, 0);
    float lastSafeRotation;
    int lastMoveStress = 0;
    GameObject dropTemplate;
    int templateNumber;
    bool flipTemplate = false;
    bool cancelExecuteMove = false;

    bool manualMode = true;
    bool mouseOver = false;
    Vector3 mouseToCenter;

    ShipConfig.DialMove dialMove = new ShipConfig.DialMove() {
        speed = 0,
        maneuver = ShipConfig.Maneuver.NONE,
        direction = 0,
    };

    string playerName;
    [SerializeField] string uniqueID;
    public bool ownShip = false;

    Sprite[] colorSchemes;

    // cached references

    GameController controller;
    Menu menu;
    ShipMenu shipMenu;
    PhotonView photonView;
    [SerializeField] PhotonView relay;

    void Start()
    {
        controller = FindObjectOfType<GameController>();
        menu = FindObjectOfType<Menu>();
        shipMenu = FindObjectOfType<ShipMenu>();

        photonView = gameObject.GetComponent<PhotonView>();
        playerName = photonView.Owner.NickName;
    }

    void Update()
    {
        RotateTokens();
        UpdateCoords();

        if (shipActive && !shipMoving && targetInput.activeSelf == false && relay != null)
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
        // Movement Controls
        if (!arcActive && !manualMode)
        {
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
        else if (manualMode)
        {
            ManualMovement();
        }
    }

    private void KTurn()
    {
        int angle = 180;
        // Turn the ship 90deg if the dhisft key is pressed
        if (Input.GetKey(KeyCode.LeftShift))
        {
            angle = 90;
        }
        else if (Input.GetKey(KeyCode.RightShift))
        {
            angle = -90;
        }

        controller.LogMove(uniqueID, transform.position, angle, 0);
        transform.Rotate(0, 0, angle);
    }

    /// <param name="direction">0: Forward, 1: left, 2: right</param>
    public void Boost(int direction)
    {
        if (direction == 0) StartCoroutine(Forward(1));
        if (direction == 1) StartCoroutine(Turn(45f, 0, 1));
        if (direction == 2) StartCoroutine(Turn(45f, 0, -1));
  }

    public void BarrelRoll(int direction, int position)
    {
        controller.LogMove(uniqueID, transform.position, 0, 0);

        float lateralMovement = direction == 0
                ? GetMovementVector("barrel", 0)
                : -GetMovementVector("barrel", 0);
        Vector3 vector = new Vector3(lateralMovement, 0);
        transform.Translate(vector);

        // End position adjustment
        float adjust = position == 0
            ? 0
            : position == 1
                ? GetMovementVector("barrel", 1)
                : -GetMovementVector("barrel", 1);
        transform.Translate(new Vector3(0, adjust));

        CheckForSafety();

        templateNumber = 0;
        barrelDirection = direction + 1; // to keep consistency between br and decloak
        barrelEndPos = position;
        rotationPerformed = 0;
    }

    public void Cloak()
    {
        if (cloaked)
        {
            shipMenu.transform.position = transform.position;
            shipMenu.SetShip(this);
            shipMenu.OpenDecloakMenu();
        }

        else
        {
            gameObject.GetPhotonView().RPC("ApplyCloakEffect", RpcTarget.AllBuffered, true);
        }
    }

    public void Decloak(int[] vals)
    {
        if (shipSize != 1) { vals[0] = 0; } // ensure bank template isn't used for any calculations
        
        float angle = vals[0] == 0
                ? 0
                : vals[0] == 1
                    ? 45
                    : -45;
        
        controller.LogMove(uniqueID, transform.position, angle, 0);

        // Decloak forward
        if (vals[1] == 0)
        {
            int speed = shipSize == 1
                ? 2
                : 1;
            switch (vals[0])
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
            Vector3 vector = vals[1] == 1
                ? GetDecloakVector(vals[0])
                : -GetDecloakVector(vals[0]);
                
            transform.Translate(vector);
            transform.Rotate(Vector3.forward * angle);

            // End pos adjustment
            float adjust = GetMovementVector("barrel", 1);
            if (vals[2] == 1)
            {
                transform.Translate(new Vector3(0, adjust));
            }
            else if (vals[2] == 2)
            {
                transform.Translate(new Vector3(0, -adjust));
            }
        }

        CheckForSafety();

        photonView.RPC("ApplyCloakEffect", RpcTarget.AllBuffered, false);

        templateNumber = shipSize == 1
            ? 1
            : 0;
        if (shipSize == 1 && vals[0] != 0)
        {
            templateNumber = 6;
        }
        flipTemplate = vals[0] == 2
            ? true
            : false;
        barrelDirection = vals[1] == 0
            ? -1
            : vals[1];
        // Make sure no adjustment is made for forward decloaking
        barrelEndPos = vals[1] == 0
            ? 0
            : vals[2];
        rotationPerformed = 0;
    }

    [PunRPC]
    private void ApplyCloakEffect(bool state)
    {
        cloaked = state;

        shipBody.GetComponent<SpriteRenderer>().color = cloaked
            ? new Color32(70, 50, 255, 150)
            : new Color32(255, 255, 255, 255);
    }

    IEnumerator Forward(int moveVelocity, int endRotation = 0, bool reverse = false)
    {
        lastSafePosition = transform.position;
        lastSafeRotation = transform.eulerAngles.z;
        controller.LogMove(uniqueID, transform.position, endRotation, lastMoveStress);
        lastMoveStress = 0;
        shipMoving = true;

        float distance = (0.1f / moveVelocity) * GetMovementVector("forward", moveVelocity - 1);

        for (int i = 0; i < (40 * moveVelocity); i++)
        {
            Vector3 trans = controller.TransformVectorByAngle(distance, transform.eulerAngles.z + 90);
            transform.position += reverse
                ? -trans
                : trans;

            yield return new WaitForEndOfFrame();
            CheckForSafety();
        }

        transform.Rotate(0, 0, endRotation);
        rotationPerformed = endRotation;

        reversePerformed = reverse;

        shipMoving = false;
        templateNumber = moveVelocity - 1;
        flipTemplate = false;
        cancelTemplateDrop = false;
        barrelDirection = -1;

        if (menu.GetShowTemplates())
        {
            // TODO extract template drop into separate method and call it here and on turn
        }
    }

    // Speed parameter is input as 1 less than move speed
    IEnumerator Turn(float angle, int speed, int direction, int endRotation = 0, bool reverse = false)
    {
        controller.LogMove(uniqueID, transform.position, (angle + endRotation) * direction, lastMoveStress);
        lastMoveStress = 0;
        shipMoving = true;

        // Get the turn radius for this ship size and determine the pivot and step increment
        // The pivot point starts from the front of the ship and moves smoothly to the back
        float radius = GetMovementVector(angle == 90 ? "turn" : "bank", Mathf.Clamp(speed, 0, 2));
        float angleIncrement = angle / 100f;
        float pivot = reverse
            ? -2 * shipSize
            : 2 * shipSize;

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
            pivot -= reverse
                ? (-4 * shipSize) / 100f
                : (4 * shipSize) / 100f;
            yield return new WaitForEndOfFrame();
            CheckForSafety();
        }

        transform.Rotate(0, 0, endRotation * direction);
        rotationPerformed = endRotation * direction;
        reversePerformed = reverse;

        shipMoving = false;
        templateNumber = angle == 90
            ? speed + 8
            : speed + 5;
        flipTemplate = direction == 1
            ? false
            : true;
        cancelTemplateDrop = false;
     barrelDirection = -1;
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

        firingArc.transform.Rotate(0, 0, 90 * direction);

        // Reset angle on full rotation
        if (firingArc.transform.eulerAngles.z % 360 == 0)
        {
            firingArc.transform.rotation = Quaternion.identity;
        }

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

    public void ApplyEffect(int i)
    {
        switch (i)
        {
            case 0:
                photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "disarmed", 0);
                break;
            case 1:
                photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "critical", 0);
                break;
            case 2:
                photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "ion", 0);
                break;
            case 3:
                photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "jam", 0);
                break;
            case 4:
                photonView.RPC("AdjustTokens", RpcTarget.AllBuffered, "tractor", 0);
                break;
            case 5:
                Cloak();
                break;
                

            case 10:
                BarrelRoll(0,0);
                break;
                
            case 16:
                StartCoroutine(Forward(1));
                break;
            case 17:
                StartCoroutine(Turn(45f, 0, 1));
                break;
            case 18:
                StartCoroutine(Turn(45f, 0, -1));
                break;
            case 19:
                DropBomb(shipMenu.GetBombDrop());
                break;
            case 20:
                //Decloak();
                break;
            default:
                Debug.Log("opened submenu");
                break;
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

        float shipHalfLength = shipSize * 2;
        float dropDistance = shipHalfLength + 5.42f;

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

            ActionBar bar = FindObjectOfType<ActionBar>();
            bar.AttachShip(this);
            bar.ToggleActionBar(1);

            Hazards[] hazards = FindObjectsOfType<Hazards>();
            foreach (Hazards hazard in hazards)
            {
                hazard.Deselect();
            }

            Bomb[] bombs = FindObjectsOfType<Bomb>();
            foreach (Bomb bomb in bombs)
            {
                bomb.Deselect();
            }
        }
    }

    [PunRPC]
    private void DestroyShip()
    {
        var explosion = Instantiate(deathVFX, transform.position, Quaternion.identity);
        Destroy(explosion, 1f);
        controller.LogMove(uniqueID, transform.position, 0f, 0);
        transform.position = new Vector3(0, -20, 0);
    }

    private void OnMouseDrag()
    {
        if (manualMode && !menu.CheckOpenHand() && !menu.CheckMenuOpen())
        {
            Vector3 trans = Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
            trans -= mouseToCenter;
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
        mouseOver = true;
    }

    public bool CheckForMouseOver()
    {
        return mouseOver;
    }

    private void OnMouseExit()
    {
        mouseOver = false;
        nameLabel.SetActive(false);
    }

    public void DeselectShip()
    {
        shipActive = false;
        selectMarker.SetActive(false);
    }

    public void ManualMode()
    {
        manualMode = !manualMode;
    }

    [PunRPC]
    public void CancelTemplateDrop()
    {
        cancelTemplateDrop = true;
    }

    public bool GetCloakState()
    {
        return cloaked;
    }

    public int GetArcDirection()
    {
        return arcDirection;
    }

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
        Vector2 initialValue = new Vector2(0, 0);
        if (lastSafePosition != initialValue && !cancelTemplateDrop)
        {
            transform.Rotate(0, 0, -rotationPerformed);

            templateSpawn.transform.localPosition = new Vector3(0, -2 * shipSize);
            templateSpawn.transform.localEulerAngles = Vector3.zero;
            
            switch (barrelDirection)
            {
                case 1:
                    templateSpawn.transform.localPosition = new Vector3(2 * shipSize, 0);
                    break;
                case 2:
                    templateSpawn.transform.localPosition = new Vector3(-2 * shipSize, 0);
                    break;
                default:
                    break;
            }
            switch (barrelEndPos)
            {
                case 1:
                    templateSpawn.transform.localPosition += 
                        Vector3.down * GetMovementVector("barrel", 1);
                    break;
                case 2:
                    templateSpawn.transform.localPosition += 
                        Vector3.up * GetMovementVector("barrel", 1);
                    break;
                default:
                    break;
            }
            if (shipSize != 1 && barrelDirection != -1)
            {
                float lateralAdjust = barrelDirection == 1
                    ? 1
                    : -1;
                templateSpawn.transform.localPosition += new Vector3( lateralAdjust, 2);
            }

            if (reversePerformed)
            {
                templateSpawn.transform.localPosition = new Vector3(0, 2 * shipSize);
                templateSpawn.transform.localEulerAngles = Vector3.forward * 180;
            }

            dropTemplate = PhotonNetwork.Instantiate("Template", 
                templateSpawn.transform.position, templateSpawn.transform.rotation, 0);
            dropTemplate.GetPhotonView().RPC("InitTemplate", RpcTarget.AllBuffered, templateNumber, flipTemplate);

            if (shipSize == 1)
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

            transform.Rotate(0, 0, rotationPerformed);

            if (lastSafePosition != new Vector2(transform.position.x, transform.position.y))
            {
                float rotation = lastSafeRotation - transform.eulerAngles.z;
                controller.LogMove(uniqueID, transform.position, rotation, 0);

                transform.position = new Vector2(lastSafePosition.x, lastSafePosition.y);
                Vector3 eulerAngles = transform.eulerAngles;
                eulerAngles.z = lastSafeRotation;
                transform.eulerAngles = eulerAngles;

                rotationPerformed = 0;
                cancelTemplateDrop = true;
            }
        }
    }

    private void ResetZ()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, -1);
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
        shipSize = pilot.config.Size();

        // Set stats from config file
        Stats stats = GetComponent<Stats>();
        stats.SetHull(pilot.config.Hull());
        stats.SetShield(pilot.config.Shield());
        stats.SetInitiative(pilot.initiative);
        stats.SetForce(pilot.force);
        stats.SetCharge(pilot.charges);

        controller = FindObjectOfType<GameController>();
        string size = controller.GetShipSize(shipSize);

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

        if (pilot.config.Arcs() == 2)
        {
            firingArc.transform.GetChild(1).gameObject.SetActive(true);
            firingArcInternal.transform.GetChild(1).gameObject.SetActive(true);
            firingArc.transform.GetChild(1).transform.localPosition += new Vector3(0, -2, 0) * shipSize;
        }


        // Configure ship size variables
        if (shipSize != 1)
        {
            selectMarker.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("UI/ship-marker-" + shipSize);
            Dictionary<string, Vector3> objectVectors = GetObjectVectors(shipSize);
            firingArc.transform.GetChild(0).localPosition = objectVectors["arc"];
            templateSpawn.transform.localPosition = objectVectors["template"];
            statsLabel.transform.localPosition = objectVectors["stats"];
            coordsLabel.transform.localPosition = objectVectors["coords"];
            stackSpawn.transform.localPosition = objectVectors["status"];
            basicTokens.transform.localPosition = objectVectors["basic"];
            tokens[4].token.transform.localPosition = objectVectors["stress"];
            tokens[2].token.transform.localPosition = objectVectors["reinforceFore"];
            tokens[3].token.transform.localPosition = objectVectors["reinforceAft"];
            targetLabel.transform.localPosition = objectVectors["target"];

            GetComponent<BoxCollider2D>().size = new Vector2(shipSize * 4, shipSize * 4);
        }


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
    private void AdjustTokens(string token, int increment)
    {
        foreach (Token token_type in tokens)
        {
            if (token_type.name == token)
            {
                if (token_type.stackable)
                {
                    token_type.count += increment;

                    int maxCount = token_type.name == "tractor" || token_type.name == "ion"
                        ? 3
                        : 9;

                    token_type.count = Mathf.Clamp(token_type.count, 0, maxCount);

                    TMP_Text countText = token_type.token.transform.GetChild(0).GetComponent<TMP_Text>();
                    GameObject countBG = token_type.token.transform.GetChild(1).gameObject;
                    
                    token_type.token.SetActive(token_type.count == 0 
                        ? false
                        : true);

                    countBG.SetActive(token_type.count > 1
                        ? true 
                        : false);

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

                    ReStackTokens();
                }
            }
        }
    }

    private void ReStackTokens()
    {
        Vector3 spawn = stackSpawn.transform.localPosition;
        int count = 0;
        foreach (GameObject token in tokenStack)
        {
            token.transform.localPosition = spawn;
            spawn += new Vector3(0, -1.3f, -0.1f);
            if (count == 2)
            {
                spawn += new Vector3(-1.9f, 3.9f, 0);
            }
            count++;
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
        ReStackTokens();
    }

    public void OpenSubMenu(string action)
    {
        shipMenu.SetShip(this);

        switch (action)
        {
            case "barrel":
                shipMenu.OpenBarrelMenu();
                break;
            case "boost":
                shipMenu.OpenBoostMenu();
                break;
            case "device":
                shipMenu.OpenDeviceMenu();
                break;
            default:
                break;
        }
    }

    private void Controls()
    {
        bool destroy = Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D);
        bool executeMove = Input.GetKeyDown(KeyCode.Return);
        bool moveToSafety = Input.GetKeyDown(KeyCode.Backspace);
        bool barrelRoll = Input.GetKeyDown(KeyCode.B);
        bool toggleCoords = Input.GetKeyDown(KeyCode.Slash);

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
            ActionBar bar = FindObjectOfType<ActionBar>();
            // TODO make this reclosable with B
            bar.ToggleActionBar(2);
            bar.OpenBarrelTab();
        }
    }

    private float GetMovementVector(string maneuver, int velocity)
    {
        // 1 and 2 refer to ship shize, change to string eventually
        Dictionary<float, Dictionary<string, float[]>> moveVectors = new Dictionary<float, Dictionary<string, float[]>>() {
            { 1, new Dictionary<string, float[]>() {
                { "forward", new float[] { 2, 3, 4, 5, 6 } },
                { "bank",    new float[] { 13.05f, 17.85f, 22.8f } },
                { "turn",    new float[] { 5.95f,  8.7f,   11.4f } },
                { "barrel",  new float[] { -8, 1 } },
            }},
            { 1.5f, new Dictionary<string, float[]>() {
                { "forward", new float[] { 2.5f, 3.5f, 4.5f, 5.5f, 6.5f } },
                { "bank",    new float[] { 15.55f, 20.4f, 25.35f } },
                { "turn",    new float[] { 7.2f, 10, 12.65f } },
                { "barrel",  new float[] { -8, 2 } },
            }},
            { 2, new Dictionary<string, float[]>() {
                { "forward", new float[] { 3, 4, 5, 6, 7 } },
                { "bank",    new float[] { 18.1f, 22.95f, 27.9f } },
                { "turn",    new float[] { 8.5f,  11.3f,  13.95f } },
                { "barrel",  new float[] { -10, 2 } },
            }},
        };

        return moveVectors[shipSize][maneuver][velocity];
    }

    private Vector3 GetDecloakVector(int template)
    {
        Dictionary<float, Vector3[]> decloakVectors = new Dictionary<float, Vector3[]>(){
            { 1, new Vector3[] {
                new Vector3(-12, 0),
                new Vector3(-12.45f, -5.15f),
                new Vector3(-12.45f, 5.15f),
            }},
            { 1.5f, new Vector3[] {
                new Vector3(-8, 0),
                new Vector3(-8, 0),
                new Vector3(-8, 0),
            }},
            { 2, new Vector3[] {
                new Vector3(-10, 0),
                new Vector3(-10, 0),
                new Vector3(-10, 0),
            }},
        };

        return decloakVectors[shipSize][template];
    }

    private Dictionary<string, Vector3> GetObjectVectors(float size)
    {
        Dictionary<float, Dictionary<string, Vector3>> objectVectors = new Dictionary<float, Dictionary<string, Vector3>>(){
            { 1.5f, new Dictionary<string, Vector3>(){
                { "arc", new Vector3(0, 3) },
                { "template", new Vector3(0, -3) },
                { "stats", new Vector3(0, 1.1f) },
                { "coords", new Vector3(0, -1.7f) },
                { "status", new Vector3(-4.5f, 2) },
                { "basic", new Vector3(0, -1.1f) },
                { "stress", new Vector3(-4.5f, 3.7f) },
                { "reinforceFore", new Vector3(4.5f, 1.5f) },
                { "reinforceAft", new Vector3(4.5f, -1.5f) },
                { "target", new Vector3(0, 5) },
            }},
            { 2, new Dictionary<string, Vector3>(){
                { "arc", new Vector3(0, 4) },
                { "template", new Vector3(0, -4) },
                { "stats", new Vector3(0, 2) },
                { "coords", new Vector3(0, -1.7f) },
                { "status", new Vector3(-5.5f, 2) },
                { "basic", new Vector3(0, -2.2f) },
                { "stress", new Vector3(-5.5f, 4) },
                { "reinforceFore", new Vector3(5.5f, 1.5f) },
                { "reinforceAft", new Vector3(5.5f, -1.5f) },
                { "target", new Vector3(0, 6) },
            }},
        };

        return objectVectors[size];
    }
}
