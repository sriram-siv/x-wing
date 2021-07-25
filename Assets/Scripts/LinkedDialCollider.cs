using UnityEngine;
using UnityEngine.Events;

public class LinkedDialCollider : MonoBehaviour
{
  UnityEvent<bool> _trigger = new UnityEvent<bool>();
  public UnityEvent<bool> trigger { get { return _trigger; } }
  void Start()
  {
    GetComponentInParent<LinkParent>().AttachChildCollider(this);
  }
  void OnMouseEnter()
  {
    trigger.Invoke(true);
  }
  void OnMouseExit()
  {
    trigger.Invoke(false);
  }
}
