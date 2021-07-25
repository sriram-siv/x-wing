using UnityEngine;

public class DialButton : MonoBehaviour
{
  Dial dial;
  void Start()
  {
    dial = GetComponentInParent<Dial>();
  }
  void OnMouseDown()
  {
    string[] vals = name.Split(',');
    dial.SetMove(float.Parse(vals[1]), float.Parse(vals[0]));
    dial.Select();
  }
}
