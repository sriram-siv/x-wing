using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using Photon;
using Photon.Pun;

public class Hazards : MonoBehaviour
{
    const string HAZARDS = "Hazards";

    Vector3 mouseToCenter;
    [SerializeField] GameObject range;
    bool rangeToggle = false;
    bool selected = false;
    bool coordsToggle = true;
    bool mouseOver = false;
    string hazardType;
    int hazardId;

    [SerializeField] GameObject coordsLabel;
    [SerializeField] GameObject effectDisplay;
    [TextArea]
    [SerializeField] string[] effectsRules;
    Menu menu;

    void Start()
    {
        menu = FindObjectOfType<Menu>();
    }

    void Update()
    {
        if (selected)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                gameObject.GetPhotonView().RPC("ToggleRange", RpcTarget.AllBuffered);
            }
            Rotate();
        }
        //UpdateCoords();
    }

    public void Deselect()
    {
        selected = false;
    }

    [PunRPC]
    private void ToggleRange()
    {
        rangeToggle = !rangeToggle;
        range.SetActive(rangeToggle);
    }

    private void OnMouseDrag()
    {
        if (menu.CheckManualMode() && !menu.CheckMenuOpen() && !menu.CheckOpenHand())
        {
            Vector3 trans = Camera.main.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x, Input.mousePosition.y, 0));
            trans -= mouseToCenter;
            transform.position = new Vector3(trans.x, trans.y, -1);
        }
    }

    private void OnMouseDown()
    {
        gameObject.GetPhotonView().RequestOwnership();

        mouseToCenter = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;

        Ship[] ships = FindObjectsOfType<Ship>();
        foreach (Ship ship in ships)
        {
            ship.DeselectShip();
        }
        Hazards[] hazards = FindObjectsOfType<Hazards>();
        foreach ( Hazards hazard in hazards)
        {
            hazard.Deselect();
        }
        Bomb[] bombs = FindObjectsOfType<Bomb>();
        foreach (Bomb bomb in bombs)
        {
            bomb.Deselect();
        }
        selected = !selected;
    }

    private void OnMouseOver()
    {
        mouseOver = true;
    }

    private void OnMouseExit()
    {
        mouseOver = false;
    }

    public bool CheckForMouseOver()
    {
        return mouseOver;
    }

    private void UpdateCoords()
    {
        string x = Mathf.Round(transform.position.x).ToString();
        string y = Mathf.Round(transform.position.y).ToString();
        string angle = Mathf.Round(transform.eulerAngles.z).ToString();

        coordsLabel.GetComponent<TMP_Text>().text = x + ", " + y + ", " + angle;
    }

    private void Rotate()
    {
        if (menu.CheckManualMode())
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                transform.Rotate(0, 0, 5);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                transform.Rotate(0, 0, -5);
            }
            //coordsLabel.transform.eulerAngles = new Vector3(0, 0, 0);
        }
    }

    public void ToggleCoords()
    {
        //coordsToggle = !coordsToggle;
        //coordsLabel.SetActive(coordsToggle);
        //numberLabel.SetActive(coordsToggle);
    }

    [PunRPC]
    public void SetHazardImage(string type, int id)
    {
        hazardType = type;
        hazardId = id;
        
        GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(
            "Hazards/" + type + "/" + type + "_" + id
        );
        transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(
            "Hazards/" + type + "/" + type + "_" + id + "r"
        );

        gameObject.AddComponent<PolygonCollider2D>();


        transform.SetParent(GameObject.Find(HAZARDS).transform);
    }

    public string GetHazardType()
    {
        return hazardType;
    }

    public int GetHazardId()
    {
        return hazardId;
    }
}
