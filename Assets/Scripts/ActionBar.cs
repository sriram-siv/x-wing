using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ActionBar : MonoBehaviour
{
    bool _barActive = false;
    public bool barActive { get { return _barActive; }}
    bool _barMouseOver = false;
    public bool barMouseOver { get { return _barMouseOver; }}

    Ship attachedShip;
    [SerializeField] Text pilotName;
    [SerializeField] GameObject mainTab;

    [Header("Barrel Roll")]
    [SerializeField] GameObject barrelTab;
    [SerializeField] Dropdown barrelDirection;
    [SerializeField] Dropdown barrelPosition;

    [Header("Boost")]
    [SerializeField] GameObject boostTab;
    [SerializeField] Dropdown boostDirection;

    [Header("Cloak")]
    [SerializeField] GameObject cloakTab;
    [SerializeField] Dropdown cloakDirection;
    [SerializeField] Dropdown cloakPosition;
    [SerializeField] Dropdown cloakTemplate;

    [Header("Device")]
    [SerializeField] GameObject deviceTab;
    [SerializeField] Dropdown deviceType;
    [SerializeField] Dropdown deviceSpeed;
    [SerializeField] Dropdown deviceDirection;

    [Header("Stats")]
    [SerializeField] GameObject statsTab;
    [SerializeField] Text hullLabel;
    [SerializeField] Text shieldLabel;
    [SerializeField] Text forceLabel;
    [SerializeField] Text chargeLabel;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (attachedShip != null)
        {
            Controls();
        }
    }

    private void Controls()
    {
        bool actions = Input.GetKeyDown(KeyCode.A);
        bool stats = Input.GetKeyDown(KeyCode.S);

        
        if (actions)
        {
            if (mainTab.activeSelf)
            {
                ToggleActionBar(1);
                OpenTab(null);
            }
            else
            {
                ToggleActionBar(2);
                OpenTab(mainTab);
            }
        }
        if (stats)
        {
            if (statsTab.activeSelf)
            {
                ToggleActionBar(1);
                OpenTab(null);
            }
            else
            {
                ToggleActionBar(2);
                OpenTab(statsTab);
            }
        }
    }

    // States: 0 hidden, 1 showing only title (controls), 2 showing full bar
    public void ToggleActionBar(int state)
    {
        _barActive = state == 2
            ? true
            : false;

        GetComponent<RectTransform>().anchoredPosition = _barActive
            ? new Vector2(0, 10)
            : state == 1
                ? new Vector2(0, -80)
                : new Vector2(0, -200);

        pilotName.text = state == 2
            ? attachedShip.name
            : "A : Actions    S : Stats    Enter : Execute Move From Dial";

        if (state == 1) { OpenTab(null); }
    }

    public void AttachShip(Ship ship)
    {
        attachedShip = ship;
    }

    public void PerformAction(string action)
    {
        int increment = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? -1
            : 1;

        attachedShip.gameObject.GetPhotonView().RPC(
            "AdjustTokens", 
            RpcTarget.AllBuffered, 
            action, 
            increment
        );
    }

    public void TargetLock()
    {
        attachedShip.ToggleTargetLock();
    }

    public void Cloak()
    {
        if (!attachedShip.GetCloakState())
        {
            attachedShip.Cloak();
        }
        else
        {
            OpenTab(cloakTab);
        }
    }

    public void Decloak()
    {
        int[] vals = { cloakTemplate.value, cloakDirection.value, cloakPosition.value };
        attachedShip.Decloak(vals);
    }

    public void BarrelRoll()
    {
        attachedShip.BarrelRoll(barrelDirection.value, barrelPosition.value);
        OpenTab(mainTab);
    }

    public void Boost()
    {
        attachedShip.Boost(boostDirection.value);
    }

    public void Device()
    {
        int[] vals = { deviceSpeed.value, deviceDirection.value, deviceType.value };
        attachedShip.DropBomb(vals);
    }

    public void IncreaseStat(Text stat)
    {
        Stats stats = attachedShip.GetComponent<Stats>();
        int maxCount = 9;

        switch (stat.name)
        {
            case "Hull Count":
                stats.SetHull(1);
                maxCount = 13;
                break;
            case "Shield Count":
                stats.SetShield(1);
                break;
            case "Force Count":
                stats.SetForce(1);
                break;
            case "Charge Count":
                stats.SetCharge(1);
                break;
        }

        int count = (int.Parse(stat.text) + (maxCount + 2)) % (maxCount + 1);
        stat.text = count.ToString();
    }
    public void DecreaseStat(Text stat)
    {
        Stats stats = attachedShip.GetComponent<Stats>();
        int maxCount = 9;

        switch (stat.name)
        {
            case "Hull Count":
                stats.SetHull(-1);
                maxCount = 13;
                break;
            case "Shield Count":
                stats.SetShield(-1);
                break;
            case "Force Count":
                stats.SetForce(-1);
                break;
            case "Charge Count":
                stats.SetCharge(-1);
                break;
        }

        int count = (int.Parse(stat.text) + maxCount) % (maxCount + 1);
        stat.text = count.ToString();
    }

    public void OpenTab(GameObject openTab)
    {
        mainTab.SetActive(false);
        barrelTab.SetActive(false);
        boostTab.SetActive(false);
        cloakTab.SetActive(false);
        deviceTab.SetActive(false);
        statsTab.SetActive(false);

        barrelDirection.value = 0;
        barrelPosition.value = 0;
        boostDirection.value = 0;
        cloakTemplate.value = 0;
        cloakDirection.value = 0;
        cloakPosition.value = 0;
        deviceType.value = 0;
        deviceSpeed.value = 0;
        deviceDirection.value = 0;

        if (openTab != null)
        {
            openTab.SetActive(true);
        }

        if (openTab == statsTab)
        {
            UpdateStats();
        }

    }

    public void OpenBarrelTab()
    {
        OpenTab(barrelTab);
    }

    public void UpdateStats()
    {
        Stats stats = attachedShip.GetComponent<Stats>();

        hullLabel.text = stats.GetHull().ToString();
        shieldLabel.text = stats.GetShield().ToString();
        forceLabel.text = stats.GetForce().ToString();
        chargeLabel.text = stats.charge.ToString();
    }

    private void OnMouseOver()
    {
        _barMouseOver = true;
    }

    private void OnMouseExit()
    {
        _barMouseOver = false;
    }
}
