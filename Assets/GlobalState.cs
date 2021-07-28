using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalState : MonoBehaviour
{
  const string SHIPS = "Ships";
  const string HAZARDS = "Hazards";
  const string DEVICES = "Devices";

  Transform shipsRoot;
  Transform hazardsRoot;
  Transform devicesRoot;

  List<MoveLog> _moveLog = new List<MoveLog>();

  private int logReadIndex;

  void Start()
  {
    shipsRoot = GameObject.Find(SHIPS).transform;
    hazardsRoot = GameObject.Find(HAZARDS).transform;
    // devicesRoot = GameObject.Find(DEVICES).transform;
  }

  public struct MoveLog
  {
    // string id;
    // string type;
    GameObject piece;
    Vector3 position;
    float rotation;
    int stressChange;
  }

  // Could return a uid for the log entry?
  // Probably needs to receive as JSON
  public void LogMove(MoveLog move)
  {
    // If log head is not at end, remove tail
    if (logReadIndex < _moveLog.Count - 1)
    {
      _moveLog.RemoveRange(logReadIndex + 1, _moveLog.Count - logReadIndex);
    }
    _moveLog.Add(move);
    logReadIndex = _moveLog.Count - 1;
  }

  // public void PrintMove(MonoBehaviour piece)
  // {
  //   string type = piece.GetType().ToString();
  //   Vector2 position = piece.transform.position;
  //   float rotation = piece.transform.eulerAngles.z;

  //   Debug.Log("type: " + type);
  //   Debug.Log("position: " + position);
  //   Debug.Log("rotation: " + rotation);
  // }

  private Transform GetRoot(MonoBehaviour piece)
  {
    string type = piece.GetType().ToString();
    switch (type)
    {
      case "Ship": return shipsRoot;
      case "Hazards": return hazardsRoot;
      case "Bomb": return devicesRoot;
      default: return null;
    }
  }
}
