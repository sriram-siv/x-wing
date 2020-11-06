using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Photon.Pun;

public class ShipMenu : MonoBehaviour
{
    [SerializeField] GameObject defaultMenu;
    [SerializeField] GameObject barrelMenu;
    [SerializeField] GameObject boostMenu;
    [SerializeField] GameObject decloakMenu;
    [SerializeField] GameObject dropMenu;
    [SerializeField] GameObject cardMenu;
    [SerializeField] GameObject dialMenu;

    [SerializeField] Text cloakLabel;

    [SerializeField] Dropdown bombSpeed;
    [SerializeField] Dropdown bombDirection;
    [SerializeField] Dropdown bombType;
    [SerializeField] Dropdown decloakTemplate;
    [SerializeField] Dropdown decloakDirection;
    [SerializeField] Dropdown decloakEndPos;
    [SerializeField] Dropdown barrelDirection;
    [SerializeField] Dropdown barrelEndPos;

    Ship activeShip;
    DamageDeck activeCard;
    Dial activeDial;

    bool mouseOver = false;

    EventSystem eventSystem;

    void Start()
    {
        eventSystem = FindObjectOfType<EventSystem>();
    }

    void Update()
    {
        if (eventSystem.currentSelectedGameObject != null)
        {
            mouseOver = true;
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            StartCoroutine(CloseMenu());
        }
    }

    public void SetShip(Ship ship)
    {
        SelectMenu(defaultMenu);
        activeShip = ship;
        cloakLabel.text = ship.GetCloakState()
            ? "decloak"
            : "cloak";
    }

    public void PositionMenu(GameObject open)
    {
        float screenToTargetResRatio = 1920f / Screen.width;

        Vector2 mousePos = new Vector2(
            Input.mousePosition.x,
            Input.mousePosition.y
        ) * screenToTargetResRatio;

        float menuHeight = open.transform.childCount * 30;
        float menuToScreenBottomBuffer = 60;
        
        Vector2 menuPos = new Vector2(
            Mathf.Clamp(mousePos.x, 0, 1920 - 210),
            Mathf.Clamp(mousePos.y, menuHeight + menuToScreenBottomBuffer, 1080)
        );

        GetComponent<RectTransform>().anchoredPosition = menuPos;
    }

    public int[] GetBombDrop()
    {
        int[] vals = { bombSpeed.value, bombDirection.value, bombType.value };
        return vals;
    }

    public int[] GetDecloak()
    {
        int[] vals = { decloakTemplate.value, decloakDirection.value, decloakEndPos.value };
        return vals;
    }

    public int[] GetBarrel()
    {
        int[] vals = { barrelDirection.value, barrelEndPos.value };
        return vals;
    }

    public void SelectOption(int option)
    {
        mouseOver = true;
        SelectMenu(null);
        activeShip.ApplyEffect(option);
    }

    public void SelectMenu(GameObject open)
    {
        defaultMenu.SetActive(false);
        barrelMenu.SetActive(false);
        boostMenu.SetActive(false);
        decloakMenu.SetActive(false);
        dropMenu.SetActive(false);
        cardMenu.SetActive(false);
        dialMenu.SetActive(false);

        bombSpeed.value = 0;
        bombDirection.value = 0;
        bombType.value = 0;

        barrelDirection.value = 0;
        barrelEndPos.value = 0;

        decloakTemplate.value = 0;
        decloakDirection.value = 0;
        decloakEndPos.value = 0;

        if (open == null) { return; }
        
        open.SetActive(true);

        UpdateCollider(open);
        PositionMenu(open);
    }

    public void OpenBarrelMenu()
    {
        SelectMenu(barrelMenu);
    }
    
    public void OpenDecloakMenu()
    {
        SelectMenu(decloakMenu);
    }

    public void OpenBoostMenu()
    {
        SelectMenu(boostMenu);
    }

    public void OpenDeviceMenu()
    {
        SelectMenu(dropMenu);
    }

    public void OpenCardMenu(DamageDeck card)
    {
        SelectMenu(cardMenu);
        activeCard = card;
    }

    public void OpenDialMenu(Dial dial)
    {
        SelectMenu(dialMenu);
        activeDial = dial;
    }

    public void ReshuffleCard()
    {
        // Get card index
        int index = activeCard.GetIndex();

        // Set deck bool to false
        FindObjectOfType<Menu>().ReshuffleDamage(index);

        // Update relay
        FindObjectOfType<RelayDevice>().gameObject.GetPhotonView().RPC(
            "DeleteDamage", RpcTarget.AllBuffered,
            FindObjectOfType<GameController>().GetPlayerNumber(),
            activeCard.GetPilot(),
            index
        );

        // Delete
        string dial = activeCard.GetPilot() + "_dial";
        Destroy(activeCard.gameObject);
        GameObject.Find(dial).GetComponent<Dial>().Select();    

        SelectMenu(null);
    }

    public void RenameShip()
    {

        SelectMenu(null);
    }

    private void UpdateCollider(GameObject menu)
    {
        int optionCount = menu.transform.childCount;

        BoxCollider2D collider = gameObject.GetComponentInChildren<BoxCollider2D>();

        collider.size = new Vector2(150, 30 * optionCount);
        collider.offset = new Vector2(75, -15 * optionCount);
        
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPosition.y < collider.size.y)
        {
            transform.position = new Vector3(transform.position.x,
                                            Camera.main.ScreenToWorldPoint(new Vector3(0, collider.size.y)).y);
        }
    }

    IEnumerator CloseMenu()
    {
        yield return new WaitForEndOfFrame();

        if (!mouseOver)
        {
            SelectMenu(null);
        }
        else
        {
            mouseOver = false;
        }
    }
}