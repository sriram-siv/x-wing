using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "ShipConfig")]
public class ShipConfig : ScriptableObject
{
    [SerializeField] float size;
    [SerializeField] int hull;
    [SerializeField] int shield;
    [SerializeField] int arcDirections;
    [SerializeField] Sprite[] dial;
    [SerializeField] DialMove[] moves;
    [SerializeField] Sprite[] _colorSchemes;
    public Sprite[] colorSchemes { get { return _colorSchemes;  } }

    [TextArea]
    [SerializeField] string ability;
 
    public float Size() { return size; }

    public int Hull() { return hull; }

    public int Shield() { return shield; }

    public int Arcs() { return arcDirections; }

    public Sprite[] Dial() { return dial; }

    public DialMove[] Moves() { return moves; }

    public string Ability() { return ability; }

        
    [Serializable]
    public class DialMove
    {
        public int speed;
        public Maneuver maneuver;
        public int direction;
        public string difficulty;

    }

    public enum Maneuver { NONE, FORWARD, BANK, TURN, KTURN, SEGNOR, TALLON, STOP, REVERSE, REVERSE_BANK };
    public enum Direction { STRAIGHT, LEFT, RIGHT = -1 };
    public enum Difficulty { WHITE, BLUE, RED };
}
