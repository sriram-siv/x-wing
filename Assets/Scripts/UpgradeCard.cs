using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class UpgradeCard : MonoBehaviour
{
  [SerializeField] GameObject attachedDial;
  [SerializeField] GameObject charge;
  [SerializeField] GameObject force;

  [SerializeField] Sprite[] sideImages = new Sprite[2];
  [SerializeField] Sprite[] chargeImages;
  [SerializeField] Sprite[] forceImages;
  int chargeAmount;
  int chargeMax;
  int forceAmount;
  int forceMax;
  int sides;
  int currentSide = 0;
  bool selected = false;

  void Update()
  {
    Controls();
  }

  private void OnMouseEnter()
  {
    // Pop up
    transform.localPosition += new Vector3(0, 12);
    transform.localScale = Vector3.one;

    // Resize collider
    GetComponent<BoxCollider2D>().size = new Vector2(19.5f, 25f);
    GetComponent<BoxCollider2D>().offset = Vector2.zero;

    // Bring forward all components in layer order to create overlap with adjacent cards
    SpriteRenderer[] layers = GetComponentsInChildren<SpriteRenderer>();
    foreach (SpriteRenderer layer in layers)
    {
      layer.sortingOrder += 5;
    }

    if (charge.activeSelf)
    {
      if (charge.transform.GetChild(0).gameObject.activeSelf == false)
      {
        charge.transform.GetChild(0).gameObject.SetActive(true);
        GetComponentInChildren<MeshRenderer>().sortingOrder += 5;
        charge.transform.GetChild(0).gameObject.SetActive(false);
      }
      else
      {
        GetComponentInChildren<MeshRenderer>().sortingOrder += 5;
      }
    }
    if (force.activeSelf)
    {
      if (force.transform.GetChild(0).gameObject.activeSelf == false)
      {
        force.transform.GetChild(0).gameObject.SetActive(true);
        GetComponentInChildren<MeshRenderer>().sortingOrder += 5;
        force.transform.GetChild(0).gameObject.SetActive(false);
      }
      else
      {
        GetComponentInChildren<MeshRenderer>().sortingOrder += 5;
      }
    }

    // Deactive dial so that up and down arrows can adjust the charge value
    if (chargeMax > 0 || forceMax > 0)
    {
      attachedDial.GetComponent<Dial>().SetDialActive(false);
    }

    // Keep dial active, useful for drawing damage and other things
    attachedDial.GetComponent<Dial>().SetMouseOver(true);
    selected = true;
  }

  private void OnMouseExit()
  {
    ResetCardPosition();

    attachedDial.GetComponent<Dial>().SetDialActive(true);

    attachedDial.GetComponent<Dial>().SetMouseOver(true);

    selected = false;
  }

  public void ResetCardPosition()
  {
    transform.localScale = new Vector3(0.7f, 0.7f);
    transform.localPosition = new Vector3(transform.localPosition.x, 3.3f);

    GetComponent<BoxCollider2D>().size = new Vector2(19.5f, 12.5f);
    GetComponent<BoxCollider2D>().offset = new Vector2(0, 6.5f);

    // Reset all sprite layers
    GetComponent<SpriteRenderer>().sortingOrder = 10;

    charge.GetComponent<SpriteRenderer>().sortingOrder = 11;
    Transform container = charge.transform.GetChild(0);
    container.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 12;
    container.GetChild(1).GetComponent<SpriteRenderer>().sortingOrder = 13;
    container.GetComponentInChildren<MeshRenderer>().sortingOrder = 14;

    force.GetComponent<SpriteRenderer>().sortingOrder = 11;
    container = force.transform.GetChild(0);
    container.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 12;
    container.GetChild(1).GetComponent<SpriteRenderer>().sortingOrder = 13;
    container.GetComponentInChildren<MeshRenderer>().sortingOrder = 14;
  }

  public void Initialize(Sprite card, int[] upgradeInfo)
  {
    GetComponent<SpriteRenderer>().sprite = card;

    name = card.name;
    gameObject.name = name;
    sideImages[0] = card;
    sides = upgradeInfo[0];
    chargeAmount = upgradeInfo[1];
    chargeMax = upgradeInfo[1];
    forceAmount = upgradeInfo[2];
    forceMax = upgradeInfo[2];

    if (sides == 2)
    {
      sideImages[1] = Resources.Load<Sprite>("Upgrades/" + card.name + "_1");
    }
    if (chargeAmount > 0)
    {
      charge.SetActive(true);

      if (chargeAmount > 1)
      {
        charge.transform.GetChild(0).gameObject.SetActive(true);
        charge.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = chargeAmount.ToString();
      }
    }
    if (forceAmount > 0)
    {
      force.SetActive(true);

      if (forceAmount > 1)
      {
        force.transform.GetChild(0).gameObject.SetActive(true);
        force.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = forceAmount.ToString();
      }
    }
  }

  private void Flip()
  {
    if (!attachedDial.GetComponent<Dial>().ownShip) { return; }

    currentSide = (currentSide + 1) % 2;
    GetComponent<SpriteRenderer>().sprite = sideImages[currentSide];

    UpdateUpgrade();
  }

  private void AdjustCharge(int change)
  {
    if (!attachedDial.GetComponent<Dial>().ownShip) { return; }

    chargeAmount = Mathf.Clamp(chargeAmount + change, 0, chargeMax);
    forceAmount = Mathf.Clamp(forceAmount + change, 0, forceMax);

    charge.GetComponent<SpriteRenderer>().sprite = chargeImages[(int)Mathf.Clamp01(chargeAmount)];
    charge.transform.GetChild(0).gameObject.SetActive(chargeAmount > 1 ? true : false);
    if (charge.transform.GetChild(0).gameObject.activeSelf)
    {
      charge.GetComponentInChildren<TMP_Text>().text = chargeAmount.ToString();
    }

    force.GetComponent<SpriteRenderer>().sprite = forceImages[(int)Mathf.Clamp01(forceAmount)];
    force.transform.GetChild(0).gameObject.SetActive(forceAmount > 1 ? true : false);
    if (force.transform.GetChild(0).gameObject.activeSelf)
    {
      force.GetComponentInChildren<TMP_Text>().text = forceAmount.ToString();
    }

    UpdateUpgrade();
  }

  // Outgoing
  private void UpdateUpgrade()
  {
    Loader.UsedUpgrade updatedUpgrade = new Loader.UsedUpgrade()
    {
      name = name,
      remainingCharges = chargeAmount,
      remainingForce = forceAmount,
      currentSide = currentSide,
    };

    FindObjectOfType<RelayDevice>().gameObject.GetPhotonView().RPC(
        "UpdateUpgrade",
        RpcTarget.AllBuffered,
        FindObjectOfType<GameController>().GetPlayerNumber(),
        attachedDial.name.Replace("_dial", ""),
        JsonUtility.ToJson(updatedUpgrade)
    );
  }

  // Incoming
  public void ReceiveUpdate(Loader.UsedUpgrade update)
  {
    currentSide = update.currentSide;
    GetComponent<SpriteRenderer>().sprite = sideImages[currentSide];

    chargeAmount = update.remainingCharges;
    forceAmount = update.remainingForce;

    AdjustCharge(0);
  }

  private void Controls()
  {
    if (!selected) return;

    bool increaseCharge = Input.GetKeyDown(KeyCode.UpArrow);
    bool decreaseCharge = Input.GetKeyDown(KeyCode.DownArrow);
    bool flip = Input.GetKeyDown(KeyCode.Space) && sides == 2;

    if (increaseCharge) AdjustCharge(1);
    if (decreaseCharge) AdjustCharge(-1);
    if (flip) Flip();
  }
}
