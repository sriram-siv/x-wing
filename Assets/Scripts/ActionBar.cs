using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ActionBar : MonoBehaviour
{
  [SerializeField] Text barLabel;

  [Header("Barrel Roll")]
  [SerializeField] Dropdown barrelDirection;
  [SerializeField] Dropdown barrelPosition;

  [Header("Boost")]
  [SerializeField] Dropdown boostDirection;

  [Header("Cloak")]
  [SerializeField] Dropdown cloakDirection;
  [SerializeField] Dropdown cloakPosition;
  [SerializeField] Dropdown cloakTemplate;

  [Header("Device")]
  [SerializeField] Dropdown deviceType;
  [SerializeField] Dropdown deviceSpeed;
  [SerializeField] Dropdown deviceDirection;

  [Header("Stats")]
  [SerializeField] Text hullLabel;
  [SerializeField] Text shieldLabel;
  [SerializeField] Text forceLabel;
  [SerializeField] Text chargeLabel;

  [SerializeField]
  ActionBarTab[] tabs;

  public Ship attachedShip;
  Vector2 barPosition { set { GetComponent<RectTransform>().anchoredPosition = value; } }
  bool _isMouseOver = false;
  public bool isMouseOver { get { return _isMouseOver; } }

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

    if (actions) ToggleBar("main");
    if (stats) ToggleBar("stats");
  }

  public void ToggleBar(string openTab)
  {
    Vector2 hidden = new Vector2(0, -200);
    Vector2 collapsed = new Vector2(0, -80);
    Vector2 expanded = new Vector2(0, 10);

    barLabel.text = "A : Actions    S : Stats    Enter : Execute Move From Dial";

    foreach (ActionBarTab tab in tabs)
    {
      if (tab.name == openTab)
      {
        bool prevState = tab.tab.activeSelf;
        tab.tab.SetActive(!prevState);
        barPosition = prevState ? collapsed : expanded;
        if (!prevState) barLabel.text = attachedShip.name;
      }
      else { tab.tab.SetActive(false); }
    }

    if (openTab == "hidden") barPosition = hidden;
    if (openTab == "collapsed") barPosition = collapsed;
    if (openTab == "stats") UpdateStats();

    ResetDropdownValues();
  }

  private void ResetDropdownValues()
  {
    barrelDirection.value = 0;
    barrelPosition.value = 0;
    boostDirection.value = 0;
    cloakTemplate.value = 0;
    cloakDirection.value = 0;
    cloakPosition.value = 0;
    deviceType.value = 0;
    deviceSpeed.value = 0;
    deviceDirection.value = 0;
  }

  public void AttachShip(Ship ship)
  {
    attachedShip = ship;
  }

  public void PerformAction(string action)
  {
    bool shiftActive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    int change = shiftActive ? -1 : 1;

    attachedShip.gameObject.GetPhotonView().RPC(
        "AdjustTokens",
        RpcTarget.AllBuffered,
        action,
        change
    );
  }

  public void TargetLock()
  {
    attachedShip.ToggleTargetLock();
  }

  public void Cloak()
  {
    if (!attachedShip.isCloaked) attachedShip.Cloak();
    else ToggleBar("cloak");
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
    ToggleBar("main");
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

  // TODO this could listen to Stats.update or something similar
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
    _isMouseOver = true;
  }

  private void OnMouseExit()
  {
    _isMouseOver = false;
  }

  [System.Serializable]
  public struct ActionBarTab
  {
    public string name;
    public GameObject tab;
  }

  private class AdjustStatsProps
  {
    public string stat;
    public int change;
  }
}
