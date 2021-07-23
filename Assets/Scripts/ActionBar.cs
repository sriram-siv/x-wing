using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ActionBar : MonoBehaviour
{
  bool _barActive = false;
  public bool barActive { get { return _barActive; } }
  bool _barMouseOver = false;
  public bool barMouseOver { get { return _barMouseOver; } }

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
        OpenTab();
      }
      else
      {
        ToggleActionBar(2);
        OpenTab("main");
      }
    }
    if (stats)
    {
      if (statsTab.activeSelf)
      {
        ToggleActionBar(1);
        OpenTab();
      }
      else
      {
        ToggleActionBar(2);
        OpenTab("stats");
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

    if (state == 1) { OpenTab(); }
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
    if (!attachedShip.isCloaked)
    {
      attachedShip.Cloak();
    }
    else
    {
      OpenTab("cloak");
    }
  }

  public void Decloak()
  {
    int[] vals = { cloakTemplate.value, cloakDirection.value, cloakPosition.value };
    attachedShip.Decloak(vals);
  }

  public void BarrelRoll()
  {
    string direction = barrelDirection.options[barrelDirection.value].text;
    string position = barrelPosition.options[barrelPosition.value].text;
    attachedShip.BarrelRoll(direction, position);
    OpenTab("main");
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

  private class AdjustStatsProps
  {
    public string stat;
    public int change;
  }

  // TODO Remove wraparound on count
  public void AdjustStats(string jsonProps)
  {
    AdjustStatsProps props = JsonUtility.FromJson<AdjustStatsProps>(jsonProps);

    PhotonView photonView = attachedShip.GetComponent<PhotonView>();
    Stats stats = attachedShip.GetComponent<Stats>();
    int maxCount = 9;
    if (props.stat == "hull") maxCount = 13;
    Text label = hullLabel;

    switch (props.stat)
    {
      case "hull":
        photonView.RPC("SetHull", RpcTarget.AllBuffered, props.change);
        label = hullLabel;
        break;
      case "shield":
        photonView.RPC("SetShield", RpcTarget.AllBuffered, props.change);
        label = shieldLabel;
        break;
      case "force":
        photonView.RPC("SetForce", RpcTarget.AllBuffered, props.change);
        label = forceLabel;
        break;
      case "charge":
        photonView.RPC("SetCharge", RpcTarget.AllBuffered, props.change);
        label = chargeLabel;
        break;
    }

    int count = (int.Parse(label.text) + props.change) % (maxCount + 1);
    if (count == -1) count = maxCount;
    label.text = count.ToString();
  }

  private struct Tab
  {
    public string name;
    public GameObject tab;
  }

  public void OpenTab(string openTab = "")
  {
    Tab[] tabs = new Tab[]{
      new Tab(){ name = "main", tab = mainTab },
      new Tab(){ name = "barrel", tab = barrelTab },
      new Tab(){ name = "boost", tab = boostTab },
      new Tab(){ name = "cloak", tab = cloakTab },
      new Tab(){ name = "device", tab = deviceTab },
      new Tab(){ name = "stats", tab = statsTab },
    };

    foreach (Tab tab in tabs)
    {
      tab.tab.SetActive(tab.name == openTab);
    }

    barrelDirection.value = 0;
    barrelPosition.value = 0;
    boostDirection.value = 0;
    cloakTemplate.value = 0;
    cloakDirection.value = 0;
    cloakPosition.value = 0;
    deviceType.value = 0;
    deviceSpeed.value = 0;
    deviceDirection.value = 0;

    if (openTab == "stats")
    {
      UpdateStats();
    }
  }

  public void UpdateStats()
  {
    Stats stats = attachedShip.GetComponent<Stats>();

    hullLabel.text = stats.hull.ToString();
    shieldLabel.text = stats.shield.ToString();
    forceLabel.text = stats.force.ToString();
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
