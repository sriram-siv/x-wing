using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Bomb : MonoBehaviour
{
    [SerializeField] Sprite[] bombTypes;
    [SerializeField] Sprite[] rangeTypes;
    [SerializeField] string[] bombEffects;
    [SerializeField] Sprite clusterSide;
    [SerializeField] Sprite clusterSideRange;
    [SerializeField] GameObject token;
    [SerializeField] GameObject range;
    [SerializeField] GameObject effectDisplay;
    [SerializeField] GameObject vFX;

    PhotonView photonView;

    bool selected = false;
    bool mouseOver = false;
    int type;

    void Start() 
    { 
        photonView = GetComponent<PhotonView>();
    }

    void Update()
    {
        Controls();
    }

    private void Controls()
    {
        if (selected && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D))
        {
            photonView.RPC("Detonate", RpcTarget.AllBuffered);
        }
        if (selected && Input.GetKeyDown(KeyCode.Space))
        {
            photonView.RPC("ToggleRange", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    private void Detonate()
    {
        var explosion = Instantiate(vFX, transform.GetChild(0).position, Quaternion.identity);
        Destroy(explosion, 1f);
        Destroy(gameObject);
    }

    [PunRPC]
    private void ToggleRange()
    {
        range.SetActive(!range.activeSelf);
    }

    private void OnMouseDown()
    {
        selected = true;
        token.GetComponent<SpriteRenderer>().color = Color.red;

        effectDisplay.SetActive(true);
        effectDisplay.transform.position = new Vector3(5, 5);

        photonView.RequestOwnership();
    }

    public void Deselect()
    {
        selected = false;
        token.GetComponent<SpriteRenderer>().color = Color.white;

        effectDisplay.SetActive(false);
    }

    public bool CheckForMouseOver()
    {
        return mouseOver;
    }

    public void DropPosition(int[] position)
    {
        Vector3[] bankVectors = {
            new Vector3(-3.35f, -1.2f),
            new Vector3(-4.75f, -4.66f),
            new Vector3(-6.2f, -8.2f) ,
            new Vector3(),
            new Vector3(),
        };
        Vector3[] turnVectors = {
            new Vector3(-4.8f, 1.95f),
            new Vector3(-7.6f, -0.8f),
            new Vector3(-10.26f, -3.46f),
            new Vector3(),
            new Vector3(),
        };
        Vector3 selectedVector = position[1] > 2
            ? turnVectors[position[0]]
            : bankVectors[position[0]];

        switch (position[1])
        {
            case 0: //straight
                transform.GetChild(0).localPosition += new Vector3(0, position[0] * -4, 0);
                break;
            case 1: // left bank
                transform.GetChild(0).localPosition += selectedVector;
                transform.GetChild(0).Rotate(0, 0, -45);
                break;
            case 2: // right bank
                transform.GetChild(0).localPosition += new Vector3(-selectedVector.x, selectedVector.y);
                transform.GetChild(0).Rotate(0, 0, 45);
                break;
            case 3: // left turn
                transform.GetChild(0).localPosition += selectedVector;
                transform.GetChild(0).Rotate(0, 0, -90);
                break;
            case 4: // right turn
                transform.GetChild(0).localPosition += new Vector3(-selectedVector.x, selectedVector.y);
                transform.GetChild(0).Rotate(0, 0, 90);
                break;
            default:
                break;
        }

        type = position[2];
        gameObject.GetPhotonView().RPC("ChangeType", RpcTarget.AllBuffered, type);

        if (type == 7) // Cluster Mines
        {
            Vector3 trans = FindObjectOfType<GameController>().TransformVectorByAngle(3.76f, transform.GetChild(0).eulerAngles.z);    
            
            

            int[] arr = { -1, 1 };
            foreach (int i in arr)
            {
                var cluster = PhotonNetwork.Instantiate("Bomb", transform.GetChild(0).position, transform.GetChild(0).rotation, 0);
                cluster.transform.position += trans * i;
                cluster.GetPhotonView().RPC("ChangeType", RpcTarget.AllBuffered, -7);
            }
        }
    }

    [PunRPC]
    public void ChangeType(int newType)
    {
        if (newType == -7)
        {
            type = 7;
            token.GetComponent<SpriteRenderer>().sprite = clusterSide;
            range.GetComponent<SpriteRenderer>().sprite = clusterSideRange;
        }
        else
        {
            type = newType;
            token.GetComponent<SpriteRenderer>().sprite = bombTypes[type];
            range.GetComponent<SpriteRenderer>().sprite = rangeTypes[type];
        }

        effectDisplay.GetComponentInChildren<Text>().text = bombEffects[type].Replace('|', '\n');
        UpdateCollider();
    }

    private void UpdateCollider()
    {
        BoxCollider2D collider = gameObject.GetComponentInChildren<BoxCollider2D>();
        SpriteRenderer renderer = gameObject.GetComponentInChildren<SpriteRenderer>();

        collider.size = renderer.sprite.bounds.size;
        collider.offset = renderer.sprite.bounds.center;
    }

    private void OnMouseOver()
    {
        mouseOver = true;
    }

    private void OnMouseExit()
    {
        mouseOver = false;
    }

    public int GetDeviceType()
    {
        return type;
    }

    public float GetRotation()
    {
        return transform.GetChild(0).eulerAngles.z;
    }

    public Vector3 GetPosition()
    {
        return transform.GetChild(0).position;
    }
}
