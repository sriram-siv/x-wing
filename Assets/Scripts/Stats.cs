using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using Photon.Pun;

public class Stats : MonoBehaviour
{

    int initiative;
    int hull;
    int shield;
    int force;
    int _charge;
    public int charge { get { return _charge; }}

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

    void Update()
    {
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
        initiative += i;
        UpdateStats();
    }

    [PunRPC]
    public void SetForce(int value)
    {
        force += value;

        if (force > 9)
        {
            force = 0;
        }
        if (force < 0)
        {
            force = 9;
        }
        UpdateStats();
    }

    [PunRPC]
    public void SetHull(int value)
    {
        hull += value;
        if (hull > 13)
        {
            hull = 0;
        }
        if (hull < 0)
        {
            hull = 13;
        }

        if (value == - 1)
        {
            var explosion = Instantiate(hitVFX, transform.position, Quaternion.identity);
            Destroy(explosion, 1f);
        }
        UpdateStats();
    }

    [PunRPC]
    public void SetShield(int value)
    {
        shield += value;
        if (shield > 9)
        {
            shield = 0;
        }
        if (shield < 0)
        {
            shield = 9;
        }
        UpdateStats();
    }

    [PunRPC]
    public void SetCharge(int change)
    {
        _charge += change;
        _charge += 10;
        _charge %= 10;

        UpdateStats();
    }

    public int GetHull()
    {
        return hull;
    }

    public int GetShield()
    {
        return shield;
    }

    public int GetForce()
    {
        return force;
    }

    [PunRPC]
    public void SetAllStats(int newHull, int newShield, int newForce)
    {
        hull = newHull;
        shield = newShield;
        force = newForce;

        UpdateStats();
    }
}
