using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using Photon.Pun;

public class Stats : MonoBehaviour
{
  int initiative;
  int _hull;
  public int hull { get { return _hull; } }
  int _shield;
  public int shield { get { return _shield; } }
  int _force;
  public int force { get { return _force; } }
  int _charge;
  public int charge { get { return _charge; } }

  [SerializeField] GameObject initiativeLabel;
  [SerializeField] GameObject hullLabel;
  [SerializeField] GameObject shieldLabel;
  [SerializeField] GameObject forceLabel;
  [SerializeField] GameObject chargeLabel;

  [SerializeField] GameObject hitVFX;

  void Start()
  {
    UpdateStats();
  }

  private void UpdateStats()
  {
    initiativeLabel.GetComponent<TMP_Text>().text = initiative.ToString();
    hullLabel.GetComponent<TMP_Text>().text = hull.ToString();
    shieldLabel.GetComponent<TMP_Text>().text = shield.ToString("0;; ");
    forceLabel.GetComponent<TMP_Text>().text = force.ToString("0;; ");
    chargeLabel.GetComponent<TMP_Text>().text = _charge.ToString("0;; ");
  }

  public void SetInitiative(int i)
  {
    initiative = i;
    UpdateStats();
  }

  [PunRPC]
  public void SetForce(int change)
  {
    _force = loopCount(force + change, 9);
    UpdateStats();
  }

  // Rename all to Adjust...(change) and create Set...(value) methods alongside 
  [PunRPC]
  public void SetHull(int change)
  {
    _hull = loopCount(hull + change, 13);
    if (change == -1)
    {
      // TODO Make a method on the ship?
      var explosion = Instantiate(hitVFX, transform.position, Quaternion.identity);
      Destroy(explosion, 1f);
    }
    UpdateStats();
  }

  [PunRPC]
  public void SetShield(int change)
  {
    _shield = loopCount(shield + change, 9);
    UpdateStats();
  }

  [PunRPC]
  public void SetCharge(int change)
  {
    _charge = loopCount(charge + change, 9);
    UpdateStats();
  }

  [PunRPC]
  public void SetAllStats(int newHull, int newShield, int newForce)
  {
    _hull = newHull;
    _shield = newShield;
    _force = newForce;

    UpdateStats();
  }

  // TODO move to static helper class
  // Returns number between 0-max where overflowing values loop back to other end of range
  // (20, 10) => 0
  // (-1, 5) => 5
  private int loopCount(int value, int max)
  {
    if (value < 0) value += (max + 1);
    return value % (max + 1);
  }
}
