using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

public class DamageDeck : MonoBehaviour
{
    [SerializeField] Sprite[] cards;
    [SerializeField] string[] cardTitles;

    int cardNumber;
    int cardIndex;

    bool selected = false;
    bool flipped = false;
    Vector3 mouseToCenter;
    Vector3 screenPosition;
    string pilot;
    bool isOwn = false;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        SelectCard();
    }

    void Update()
    {
        Controls();
    }

    public void CardFinder(int number)
    {
        cardIndex = number;
        if (number == -1)
        {
            cardNumber = 0;
        }
        else if (number > 27)
        {
            cardNumber = 14;
        }
        else if (number > 23)
        {
            cardNumber = 13;
        }
        else
        {
            int convert = Mathf.FloorToInt(number / 2) + 1;
            cardNumber = convert;
        }
    }

    public void SetPilot(string pilotName)
    {
        pilot = pilotName;
    }

    public void Flip()
    {
        if (flipped)
        {
            GetComponent<SpriteRenderer>().sprite = cards[0];
            transform.position += new Vector3(0, 0, 1f);
            flipped = false;
        }
        else
        {
            GetComponent<SpriteRenderer>().sprite = cards[cardNumber];
            transform.position += new Vector3(0, 0, -1f);
            flipped = true;

            string message = FindObjectOfType<Loader>().GetPlayerName() + " took a critical hit   [" + cardTitles[cardNumber] + "]";
            FindObjectOfType<RelayDevice>().gameObject.GetPhotonView().RPC("SendAlertMessage", RpcTarget.AllBuffered, message, 5);
        }

        FindObjectOfType<RelayDevice>().gameObject.GetPhotonView().RPC(
        "UpdateDamage", RpcTarget.AllBuffered,
        FindObjectOfType<GameController>().GetPlayerNumber(),
        pilot,
        cardIndex,
        flipped
        );
    }

    private void CascadeCards(float zIndex, DamageDeck topCard)
    {
        DamageDeck[] allCards = FindObjectsOfType<DamageDeck>();
        foreach (DamageDeck card in allCards)
        {
            card.Deselect();
            if (card.transform.position.z == zIndex && card != topCard)
            {
                card.transform.position += new Vector3(0, 0, 0.1f);
                CascadeCards(zIndex + 0.1f, card);
            }
        }
    }

    private void ReshuffleCard()
    {
        // Set deck bool to false
        FindObjectOfType<Menu>().ReshuffleDamage(cardIndex);

        // Update relay
        FindObjectOfType<RelayDevice>().gameObject.GetPhotonView().RPC(
            "DeleteDamage", RpcTarget.AllBuffered,
            FindObjectOfType<GameController>().GetPlayerNumber(),
            pilot,
            cardIndex
        );

        // Delete
        string dial = pilot + "_dial";
        GameObject.Find(dial).GetComponent<Dial>().Select();    
        Destroy(this.gameObject);
    }

    private void SelectCard()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, -30);
        mouseToCenter = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        mouseToCenter.z = Camera.main.transform.position.z + 30;
        CascadeCards(transform.position.z, this);
        selected = true;
    }

    private void OnMouseDown()
    {
        SelectCard();
    }

    private void OnMouseDrag()
    {
        Vector3 trans = Camera.main.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x, Input.mousePosition.y, 0));
        trans -= mouseToCenter;
        transform.position = trans;
    }

    private void OnMouseOver()
    {
        GetComponentInParent<Dial>().SetMouseOver(true);
        GetComponentInParent<Dial>().SetDialActive(false);
    }

    private void OnMouseExit()
    {
        GetComponentInParent<Dial>().SetMouseOver(false);
    }

    public void Deselect()
    {
        selected = false;
    }

    public void SetScreenPosition()
    {
        screenPosition = Camera.main.WorldToScreenPoint(transform.position);
    }

    public Vector3 GetScreenPosition()
    {
        return screenPosition;
    }

    public int GetIndex()
    {
        return cardIndex;
    }

    public bool GetFlipped()
    {
        return flipped;
    }

    public string GetPilot()
    {
        return pilot;
    }

    public void SetAsOwn()
    {
        isOwn = true;
    }

    private void Controls()
    {
        bool flip = Input.GetKeyDown(KeyCode.Space) && selected && isOwn;
        bool reshuffle = Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl) && selected && isOwn;

        if (flip) { Flip(); }
        if (reshuffle) { ReshuffleCard(); }
    }
}
