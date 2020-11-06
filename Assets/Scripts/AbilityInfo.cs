using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AbilityInfo : MonoBehaviour
{
    [SerializeField] GameObject pilotAbility;
    [SerializeField] GameObject shipAbility;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseEnter()
    {
        if (pilotAbility.GetComponent<TMP_Text>().text != null)
        {
            pilotAbility.SetActive(true);
        }
        else
        {
            shipAbility.transform.position = pilotAbility.transform.position;
        }
        
        if (shipAbility.GetComponent<TMP_Text>().text != "")
        {
            shipAbility.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        pilotAbility.SetActive(false);
        shipAbility.SetActive(false);
    }
}
