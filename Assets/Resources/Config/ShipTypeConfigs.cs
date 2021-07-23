using UnityEngine;

// Type definition for ship size related variables object
// ? Getter values used to transform array into Vector3 in order to simplify json file
[System.Serializable]
public struct ShipTypeConfigs
{
  [System.Serializable]
  public struct Values
  {
    public float width;

    [System.Serializable]
    public struct Movement
    {
      public float[] forward;
      public float[] bank;
      public float[] turn;
    }
    public Movement movement;
    public float[] barrel;
    [System.Serializable]
    public struct Cloak
    {
      public float[] _straight;
      public float[] _left;
      public float[] _right;
      public Vector3 this[int curve]
      {
        get
        {
          switch (curve)
          {
            case 0:
              return new Vector3(this._straight[0], this._straight[1]);
            case 1:
              return new Vector3(this._left[0], this._left[1]);
            case 2:
              return new Vector3(this._right[0], this._right[1]);
            default:
              return new Vector3(this._straight[0], this._straight[1]);
          }
        }
      }
    }
    public Cloak cloak;

    [System.Serializable]
    public struct Objects
    {
      public float[] _arc;
      public Vector3 arc
      {
        get { return new Vector3(_arc[0], _arc[1]); }
      }
      public float[] _template;
      public Vector3 template
      {
        get { return new Vector3(_template[0], _template[1]); }
      }
      public float[] _stats;
      public Vector3 stats
      {
        get { return new Vector3(_stats[0], _stats[1]); }
      }
      public float[] _coords;
      public Vector3 coords
      {
        get { return new Vector3(_coords[0], _coords[1]); }
      }
      public float[] _status;
      public Vector3 status
      {
        get { return new Vector3(_status[0], _status[1]); }
      }
      // Basic green action tokens
      public float[] _basic;
      public Vector3 basic
      {
        get { return new Vector3(_basic[0], _basic[1]); }
      }
      public float[] _stress;
      public Vector3 stress
      {
        get { return new Vector3(_stress[0], _stress[1]); }
      }
      public float[] _reinforceFore;
      public Vector3 reinforceFore
      {
        get { return new Vector3(_reinforceFore[0], _reinforceFore[1]); }
      }
      public float[] _reinforceAft;
      public Vector3 reinforceAft
      {
        get { return new Vector3(_reinforceAft[0], _reinforceAft[1]); }
      }
      public float[] _target;
      public Vector3 target
      {
        get { return new Vector3(_target[0], _target[1]); }
      }
    }
    public Objects objects;
  }
  public Values small;
  public Values medium;
  public Values large;

  public Values this[string size]
  {
    get
    {
      switch (size)
      {
        case "small":
          return small;
        case "medium":
          return medium;
        case "large":
          return large;
        default:
          Debug.Log("Invalid ship size requested: " + size);
          return small;
      }
    }
  }
}
