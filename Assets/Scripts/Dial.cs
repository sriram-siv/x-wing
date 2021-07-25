using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class Dial : LinkParent
{
  [SerializeField] GameObject title;
  [SerializeField] GameObject moves;
  [SerializeField] GameObject marker;
  [SerializeField] GameObject highlight;
  [SerializeField] GameObject upgrades;
  [SerializeField] GameObject upgradeTitle;
  [SerializeField] GameObject ability;
  [SerializeField] GameObject abilityShipDisplay;
  [SerializeField] GameObject renameInput;
  [SerializeField] GameObject damage;
  [SerializeField] GameObject moveButtons;
  [SerializeField] GameObject damageCard;

  public Transform damageContainer { get { return damage.transform; } }

  bool _isSelected = false;
  public bool isSelected { get { return _isSelected; } }
  bool dialActive = false;
  bool _mouseOver = false;
  public bool mouseOver { get { return _mouseOver; } }

  Vector2 moveSelect;
  Vector3 mouseToCenter;
  Vector3 _screenPosition;
  public Vector3 screenPosition { get { return _screenPosition; } }

  int totalUpgrades = 0;
  bool _ownShip = false;
  public bool ownShip { get { return _ownShip; } }

  Ship attachedShip;
  ShipConfig.DialMove[] dialMoves;

  void Start()
  {
    if (!ownShip) return;

    if (attachedShip != null)
      attachedShip.SetMoveFromDial(dialMoves[0]);
    else
      Debug.Log("Attached ship not found");
  }

  void Update()
  {
    if (ownShip && isSelected && dialActive) MoveCursor();
  }

  override public void AttachChildCollider(LinkedDialCollider child)
  {
    child.trigger.AddListener(SetMouseOver);
  }

  private void CascadeDials(float zIndex, Dial topDial)
  {
    Dial[] allDials = FindObjectsOfType<Dial>();
    foreach (Dial dial in allDials)
    {
      dial.Deselect();
      if (dial.transform.position.z == zIndex && dial != topDial)
      {
        dial.transform.position += new Vector3(0, 0, 0.3f);
        CascadeDials(zIndex + 0.3f, dial);
      }
    }
  }

  private void MoveCursor()
  {
    if (Input.GetKeyDown(KeyCode.LeftArrow))
    {
      SetMove(moveSelect.x - 1, moveSelect.y);
    }
    if (Input.GetKeyDown(KeyCode.RightArrow))
    {
      SetMove(moveSelect.x + 1, moveSelect.y);
    }
    if (Input.GetKeyDown(KeyCode.UpArrow))
    {
      SetMove(moveSelect.x, moveSelect.y + 1);
    }
    if (Input.GetKeyDown(KeyCode.DownArrow))
    {
      SetMove(moveSelect.x, moveSelect.y - 1);
    }
  }

  public void SetMove(float x, float y)
  {
    moveSelect = new Vector2(
      Mathf.Clamp(x, 0, 6),
      Mathf.Clamp(y, 0, 4)
    );
    float selectedMove = (moveSelect.y * 7) + moveSelect.x;
    attachedShip.SetMoveFromDial(dialMoves[(int)selectedMove]);
    Vector3 markerOrigin = new Vector3(-4.1f, -6.32f, -0.1f);
    Vector3 markerMoveVector = new Vector3(1.65f * moveSelect.x, 2.05f * moveSelect.y);
    marker.transform.localPosition = markerOrigin + markerMoveVector;
    highlight.SetActive(true);
  }

  private void OnMouseDown()
  {
    Select();
  }

  // TODO use getter
  public Ship GetAttachedShip()
  {
    return attachedShip;
  }

  public void Select()
  {
    DamageDeck[] cards = FindObjectsOfType<DamageDeck>();
    foreach (DamageDeck card in cards)
    {
      card.Deselect();
    }

    mouseToCenter = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
    mouseToCenter.z = Camera.main.transform.position.z + 10;
    transform.position = new Vector3(transform.position.x, transform.position.y, -10);
    CascadeDials(transform.position.z, this);

    _isSelected = true;
    dialActive = true;
    upgrades.SetActive(true);
    damage.SetActive(true);
    highlight.SetActive(true);

    AnchorUpgradesDisplay();
    UpgradeCard[] upgradeCards = FindObjectsOfType<UpgradeCard>();
    foreach (UpgradeCard upgrade in upgradeCards)
    {
      upgrade.ResetCardPosition();
      ability.SetActive(false);
    }

    attachedShip.highlight = true;
  }

  public void Deselect()
  {
    _isSelected = false;
    upgrades.SetActive(false);
    damage.SetActive(false);
    attachedShip.highlight = false;
    highlight.SetActive(false);
  }

  public void AnchorUpgradesDisplay()
  {
    upgrades.transform.position = new Vector3(
        Camera.main.transform.position.x,
        Camera.main.transform.position.y - Camera.main.orthographicSize,
        -10);

    float ratio = (float)Screen.width / (float)Screen.height;
    float refRatio = 1920f / 1080f;
    upgrades.transform.localScale = Vector3.one / (refRatio / ratio);
  }

  private void OnMouseDrag()
  {
    Vector3 trans = Camera.main.ScreenToWorldPoint(new Vector3(
            Input.mousePosition.x, Input.mousePosition.y, 0));
    trans -= mouseToCenter;
    transform.position = trans;

    AnchorUpgradesDisplay();
  }

  private void OnMouseEnter()
  {
    _mouseOver = true;
    attachedShip.highlight = true;
  }

  private void OnMouseExit()
  {
    _mouseOver = false;
    attachedShip.highlight = isSelected;
  }

  public void SetMouseOver(bool state)
  {
    _mouseOver = state;
    attachedShip.highlight = state;
  }

  public void SetDialActive(bool state)
  {
    dialActive = state;
    attachedShip.highlight = isSelected;
  }

  private void Destroy()
  {
    if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl))
    {
      Destroy(gameObject);
    }
  }

  // public void AddUpgrade(Sprite card, int chargeValue, int sides)
  // {
  //     upgrades.transform.GetChild(totalUpgrades).GetComponent<UpgradeCard>().Initialize(card, sides, chargeValue);
  //     totalUpgrades++;
  // }

  public void InitializeDial(Loader.Pilot pilot, int player)
  {
    title.GetComponent<TMP_Text>().text = pilot.name;
    upgradeTitle.GetComponent<TMP_Text>().text = pilot.name;
    ability.GetComponent<TMP_Text>().text = pilot.ability;
    abilityShipDisplay.GetComponent<TMP_Text>().text = pilot.config.ability;
    dialMoves = pilot.config.moves;
    name = pilot.name + "_dial";

    attachedShip = Array.Find(
      FindObjectsOfType<Ship>(),
      s => s.GetUniqueID() == pilot.uniqueID
    );

    if (player == 1) _ownShip = true;

    GetComponent<SpriteRenderer>().sprite = pilot.config.dial[0];
    moves.GetComponent<SpriteRenderer>().sprite = pilot.config.dial[1];

    foreach (string[] type in pilot.upgrades.all)
    {
      foreach (string card in type)
      {
        Sprite cardImg = Resources.Load<Sprite>("Upgrades/" + card);

        if (cardImg == null)
        {
          Debug.Log("Upgrade card not available - " + card);
          continue;
        }

        int[] upgradeInfo = FindObjectOfType<Loader>().GetUpgradeInfo(card);

        upgrades.transform.GetChild(totalUpgrades).GetComponent<UpgradeCard>().Initialize(cardImg, upgradeInfo);
        totalUpgrades++;

        if (card == "hullupgrade")
        {
          attachedShip.GetComponent<Stats>().SetHull(1);
        }
        if (card == "shieldupgrade")
        {
          attachedShip.GetComponent<Stats>().SetShield(1);
        }
      }
    }
  }

  public void UpdateUpgrades(List<Loader.UsedUpgrade> updatedUpgrades)
  {
    foreach (Loader.UsedUpgrade updatedUpgrade in updatedUpgrades)
    {
      // TODO save an Array of upgrade transforms on init so we can use find here
      for (int i = 0; i < upgrades.transform.childCount; i++)
      {
        GameObject upgrade = upgrades.transform.GetChild(i).gameObject;
        if (upgrade.name == updatedUpgrade.name)
        {
          upgrade.GetComponent<UpgradeCard>().ReceiveUpdate(updatedUpgrade);
        }
      }
    }
  }

  public void UpdateDamage(List<Loader.DamageSaveData> updatedDamage)
  {
    foreach (Loader.DamageSaveData damageData in updatedDamage)
    {
      bool exists = false;
      foreach (DamageDeck card in damage.GetComponentsInChildren<DamageDeck>())
      {
        if (card.GetIndex() == damageData.cardIndex)
        {
          if (damageData.flipped != card.GetFlipped())
          {
            card.Flip();
          }
          exists = true;
        }
      }

      if (!exists)
      {
        GameObject newDamage = Instantiate(damageCard) as GameObject;

        newDamage.GetComponent<DamageDeck>().CardFinder(damageData.cardIndex);

        newDamage.transform.parent = damage.transform;

        int cardNum = (damage.transform.childCount - 1) % 21; // max cards that can fit in 3 rows
        float yPos = Mathf.Floor(cardNum / 7) * 20;
        Vector3 cardPos = new Vector3((cardNum % 7) * 15, yPos);
        newDamage.transform.localPosition = cardPos;

        newDamage.transform.localScale = Vector3.one;


        if (damageData.flipped)
        {
          newDamage.GetComponent<DamageDeck>().Flip();
        }
      }

    }
  }

  public void RenameDial(string newName)
  {
    title.GetComponent<TMP_Text>().text = newName;
    upgradeTitle.GetComponent<TMP_Text>().text = newName;
    name = newName + "_dial";
  }

  public void UpdateDialName()
  {
    string shipName = attachedShip.name;

    title.GetComponent<TMP_Text>().text = shipName;
    upgradeTitle.GetComponent<TMP_Text>().text = shipName;
    name = shipName + "_dial";
  }

  public void UpdateScreenPosition()
  {
    _screenPosition = Camera.main.WorldToScreenPoint(transform.position);
  }
}
