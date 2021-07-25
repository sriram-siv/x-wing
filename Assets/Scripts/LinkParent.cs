using UnityEngine;

public class LinkParent : MonoBehaviour
{
  virtual public void AttachChildCollider(LinkedDialCollider child)
  {
    Debug.Log("Replace this method in collider root");
  }
}
