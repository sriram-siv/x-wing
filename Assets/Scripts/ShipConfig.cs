using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "ShipConfig")]
public class ShipConfig : ScriptableObject
{
  [FormerlySerializedAs("size")]
  [SerializeField] float _sizeNum;
  [SerializeField] Size _size;
  public string size { get { return _size.ToString(); } }
  public float sizeNum { get { return _sizeNum; } }
  [FormerlySerializedAs("hull")]
  [SerializeField] int _hull;
  public int hull { get { return _hull; } }
  [FormerlySerializedAs("shield")]
  [SerializeField] int _shield;
  public int shield { get { return _shield; } }
  [FormerlySerializedAs("arcDirections")]
  [SerializeField] int _arcs;
  public int arcs { get { return _arcs; } }
  [FormerlySerializedAs("dial")]
  [SerializeField] Sprite[] _dial;
  public Sprite[] dial { get { return _dial; } }
  [FormerlySerializedAs("moves")]
  [SerializeField] DialMove[] _moves;
  public DialMove[] moves { get { return _moves; } }
  [SerializeField] Sprite[] _colorSchemes;
  public Sprite[] colorSchemes { get { return _colorSchemes; } }

  [TextArea]
  [FormerlySerializedAs("ability")]
  [SerializeField] string _ability;
  public string ability { get { return _ability; } }

  [Serializable]
  public class DialMove
  {
    public int speed;
    public Maneuver maneuver;
    public int direction;
    public string difficulty;

  }

  public enum Size { small, medium, large };
  public enum Maneuver { NONE, FORWARD, BANK, TURN, KTURN, SEGNOR, TALLON, STOP, REVERSE, REVERSE_BANK };
  public enum Direction { STRAIGHT, LEFT, RIGHT = -1 };
  public enum Difficulty { WHITE, BLUE, RED };
}
