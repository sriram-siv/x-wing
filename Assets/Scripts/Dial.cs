﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class Dial : MonoBehaviour
{
    [SerializeField] GameObject title;
    [SerializeField] GameObject moves;
    [SerializeField] GameObject marker;
    [SerializeField] GameObject upgrades;
    [SerializeField] GameObject upgradeTitle;
    [SerializeField] GameObject ability;
    [SerializeField] GameObject abilityShipDisplay;
    [SerializeField] GameObject damage;
    [SerializeField] GameObject renameInput;

    bool selected = false;
    bool dialActive = false;
    bool mouseOver = false;

    int markerX = 0;
    int markerY = 0;
    int moveSelect = 0;

    Vector3 mouseToCenter;

    Vector3 screenPosition;

    int totalUpgrades = 0;
    bool ownShip;

    [SerializeField] GameObject attachedShip;
    [SerializeField] ShipConfig.DialMove[] dialMoves;

    [SerializeField] GameObject damageCard;

    void Start()
    {
        if (ownShip)
        {
            try
            {
                attachedShip.GetComponent<Ship>().SetMoveFromDial(dialMoves[0]);
            }
            catch { Debug.Log("attached ship not found"); }
        }
    }

    
    void Update()
    {
        if (selected && dialActive)
        {
            MoveCursor();
        }
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
        float componentResizeRatio = Camera.main.orthographicSize / 41.5f; // Camera starting size at min zoom
        Vector2 markerMoveDistance = new Vector2(1.98f, 2.46f) * componentResizeRatio;
        
        if (Input.GetKeyDown(KeyCode.LeftArrow) && markerX > 0)
        {
            marker.transform.Translate(-markerMoveDistance.x, 0, 0);
            markerX--;
            moveSelect--;
            if (ownShip)
            {
                attachedShip.GetComponent<Ship>().SetMoveFromDial(dialMoves[moveSelect]);
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) && markerX < 6)
        {
            marker.transform.Translate(markerMoveDistance.x, 0, 0);
            markerX++;
            moveSelect++;
            if (ownShip)
            {
                attachedShip.GetComponent<Ship>().SetMoveFromDial(dialMoves[moveSelect]);
            }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) && markerY < 4)
        {
            marker.transform.Translate(0, markerMoveDistance.y, 0);
            markerY++;
            moveSelect += 7;
            if (ownShip)
            {
                attachedShip.GetComponent<Ship>().SetMoveFromDial(dialMoves[moveSelect]);
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) && markerY > 0)
        {
            marker.transform.Translate(0, -markerMoveDistance.y, 0);
            markerY--;
            moveSelect -= 7;
            if (ownShip)
            {
                attachedShip.GetComponent<Ship>().SetMoveFromDial(dialMoves[moveSelect]);
            }
        }

        if (moveSelect == 0)
        {
            marker.transform.localPosition = new Vector3(-4.1f, -6.32f, -0.1f);
        }
    }
    
    private void OnMouseDown()
    {
        Select();
    }

    public Ship GetAttachedShip()
    {
        return attachedShip.GetComponent<Ship>();
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

        selected = true;
        dialActive = true;
        upgrades.SetActive(true);
        damage.SetActive(true);

        AnchorUpgradesDisplay();
        UpgradeCard[] upgradeCards = FindObjectsOfType<UpgradeCard>();
        foreach (UpgradeCard upgrade in upgradeCards)
        {
            upgrade.ResetCardPosition();
            ability.SetActive(false);
        }
    }

    public void Deselect()
    {
        selected = false;
        upgrades.SetActive(false);
        damage.SetActive(false);
    }

    public void SetDialActive(bool state)
    {
        dialActive = state;
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
        mouseOver = true;
    }

    private void OnMouseExit()
    {
        mouseOver = false;
    }

    private void OnMouseOver()
    {
        
    }

    public bool CheckForMouseOver()
    {
        return mouseOver;
    }

    public void SetMouseOver(bool state)
    {
        mouseOver = state;
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
        abilityShipDisplay.GetComponent<TMP_Text>().text = pilot.config.Ability();

        dialMoves = pilot.config.Moves();

        name = pilot.name + "_dial";
        
        foreach (Ship ship in FindObjectsOfType<Ship>())
        {
            if (ship.GetUniqueID() == pilot.uniqueID)
            {
                attachedShip = ship.gameObject;
            }
        }

        ownShip = player == 1
            ? true
            : false;

        GetComponent<SpriteRenderer>().sprite = pilot.config.Dial()[0];
        moves.GetComponent<SpriteRenderer>().sprite = pilot.config.Dial()[1];

        foreach (string[] type in pilot.upgrades.GetAll())
        {
            if (type != null)
            {
                foreach (string card in type)
                {
                    Sprite cardImg = Resources.Load<Sprite>("Upgrades/" + card);
                    
                    if (cardImg == null) { continue; }

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
    }

    public void UpdateUpgrades(List<Loader.UsedUpgrade> updatedUpgrades)
    {
        foreach (Loader.UsedUpgrade updatedUpgrade in updatedUpgrades)
        {
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

    public void SetScreenPosition()
    {
        screenPosition = Camera.main.WorldToScreenPoint(transform.position);
    }

    public Vector3 GetScreenPosition()
    {
        return screenPosition;
    }

    public bool IsSelected()
    {
        return selected;
    }

    public GameObject GetDamageObj()
    {
        return damage;
    }

    public bool IsOwn()
    {
        return ownShip;
    }
}
