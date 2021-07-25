using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon;

public class Templates : MonoBehaviour
{
  [SerializeField] Sprite[] templates;

  bool isDoubleClick = false;
  [SerializeField] bool displayTemplate;
  [SerializeField] int speed = 0;
  [SerializeField] int maneuver = 0;

  void Update()
  {
    if (displayTemplate) { Rotate(); }
  }

  [PunRPC]
  public void InitTemplate(int templateNumber, bool flip)
  {
    GetComponent<SpriteRenderer>().sprite = templates[templateNumber];
    gameObject.AddComponent<PolygonCollider2D>();
    if (flip)
    {
      transform.localScale = new Vector3(-1, 1, 1);
    }
  }

  private void OnMouseUp()
  {
    if (isDoubleClick)
      gameObject.GetPhotonView().RPC("RemoveTemplate", RpcTarget.AllBuffered);
    else
      StartCoroutine(DoubleClickTimer());
  }

  IEnumerator DoubleClickTimer()
  {
    isDoubleClick = true;
    yield return new WaitForSeconds(0.5f);
    isDoubleClick = false;
  }

  [PunRPC]
  private void RemoveTemplate()
  {
    Destroy(gameObject);
  }

  private void Rotate()
  {
    if (Input.GetKey(KeyCode.LeftArrow))
    {
      transform.Rotate(Vector3.forward);
    }
    else if (Input.GetKey(KeyCode.RightArrow))
    {
      transform.Rotate(Vector3.back);
    }
  }

  public void ChangeSpeed(int newSpeed)
  {
    speed = newSpeed;

    switch (maneuver)
    {
      case 0:
        GetComponent<SpriteRenderer>().sprite = templates[speed];
        break;
      case 1:
        GetComponent<SpriteRenderer>().sprite = templates[Mathf.Clamp(speed, 0, 2) + 5];
        break;
      case 2:
        GetComponent<SpriteRenderer>().sprite = templates[Mathf.Clamp(speed, 0, 2) + 8];
        break;
      default:
        break;
    }
  }

  public void ChangeManeuver(int newManeuver)
  {
    maneuver = newManeuver;
    Debug.Log(newManeuver);

    switch (maneuver)
    {
      case 0:
        GetComponent<SpriteRenderer>().sprite = templates[speed];
        break;
      case 1:
        GetComponent<SpriteRenderer>().sprite = templates[Mathf.Clamp(speed, 0, 2) + 5];
        break;
      case 2:
        GetComponent<SpriteRenderer>().sprite = templates[Mathf.Clamp(speed, 0, 2) + 8];
        break;
      default:
        break;
    }
  }
}
