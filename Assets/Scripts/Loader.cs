using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Text.RegularExpressions;

public class Loader : MonoBehaviour
{
  [SerializeField] GameObject feedbackText;
  [SerializeField] TextAsset[] upgradeFiles;
  [SerializeField] XWSquad xws; // This is only serialized for debug purposes
  Wrapper upgrades;

  [SerializeField] InputField inputField;
  [SerializeField] GameObject squadListing;
  [SerializeField] GameObject shipListing;
  GameController controller;

  [SerializeField] XWSquad loadedSquad;

  int playmat = 0;
  string playerName;

  bool _isWebGL = false;
  public bool isWebGL { get { return _isWebGL; } }

  void Start()
  {
    DontDestroyOnLoad(gameObject);
    controller = FindObjectOfType<GameController>();
  }

  public void LoadXWS()
  {
    Dictionary<string, int> factionNames = new Dictionary<string, int>(){
            { "rebelalliance",      0 },
            { "galacticempire",     1 },
            { "scumandvillainy",    2 },
            { "resistance",         0 },
            { "firstorder",         1 },
            { "galacticrepublic",   3 },
            { "separatistalliance", 2 },
        };

    string loadString = inputField.text.Replace("-", "");

    // Prevent method from executing twice on buttonclick (which triggers the endedit function of the inputfield)
    if (loadString == "") { return; }

    inputField.text = "";

    try
    {
      xws = JsonUtility.FromJson<XWSquad>(loadString);
      feedbackText.GetComponent<Text>().text = "";
    }
    catch
    {
      feedbackText.GetComponent<Text>().text = "invalid squad file";
      feedbackText.GetComponent<Text>().color = Color.red;
      return;
    }

    List<string> uniqueNames = new List<string>();

    foreach (Pilot pilot in xws.pilots)
    {
      // Get ShipConfig file for ship
      pilot.config = GetConfigFile(pilot.ship, pilot.name);
      pilot.faction = factionNames[xws.faction];

      if (pilot.config == null)
      {
        feedbackText.GetComponent<Text>().text = "unsupported ship found in file: " + pilot.ship;
        feedbackText.GetComponent<Text>().color = Color.red;
        xws = null;
        return;
      }

      // Get pilot data from Pilot XWS and change name for duplicate pilots
      TextAsset file = Resources.Load<TextAsset>("json/" + xws.faction + "/" + pilot.ship);
      ShipInfo info = JsonUtility.FromJson<ShipInfo>(file.ToString());
      for (int i = 0; i < info.pilots.Length; i++)
      {
        PilotInfo pilotInfo = info.pilots[i];
        if (pilotInfo.xws.Replace("-", "") == pilot.name)
        {
          pilot.name = pilotInfo.name;
          bool uniqueNameAssigned = false;
          int squadNum = 1;
          while (!uniqueNameAssigned)
          {
            if (uniqueNames.Contains(pilot.name))
            {
              Regex pilotNum = new Regex(@"\s#\d");
              pilot.name = pilotNum.Replace(pilot.name, "");
              squadNum++;
              pilot.name += " #" + squadNum.ToString();
            }
            else
            {
              uniqueNameAssigned = true;
              uniqueNames.Add(pilot.name);
            }
          }

          pilot.initiative = pilotInfo.initiative;
          pilot.ability = pilotInfo.ability;
          pilot.force = pilotInfo.force.value;
          pilot.charges = pilotInfo.charges.value;
        }
      }

      // Initialise UsedUpgrades variable
      string[][] upgrades = pilot.upgrades.all;
      foreach (string[] type in upgrades)
      {
        foreach (string upgrade in type)
        {
          int[] upgradeInfo = GetUpgradeInfo(upgrade);

          // Create new UsedUpgrade
          UsedUpgrade newUsedUpgrade = new UsedUpgrade()
          {
            name = upgrade,
            remainingCharges = upgradeInfo[1],
            remainingForce = upgradeInfo[2],
            currentSide = 0,
          };

          // Add to XWS file
          pilot.upgrades.usedUpgrades.Add(newUsedUpgrade);
        }
      }

      pilot.uniqueID = GenerateID();
    }

    XWSquad newSquad = new XWSquad() { pilots = xws.pilots, faction = xws.faction };


    GameObject list = squadListing.transform.GetChild(0).gameObject;
    int count = list.transform.childCount;
    for (int i = 0; i < count; i++)
    {
      Destroy(list.transform.GetChild(i).gameObject);
    }

    loadedSquad = newSquad;

    int listingCounter = 0;
    foreach (Loader.Pilot pilot in newSquad.pilots)
    {
      var newListing = Instantiate(shipListing, transform.position, Quaternion.identity) as GameObject;
      newListing.transform.SetParent(list.transform);
      Vector3 adjust = listingCounter * new Vector3(0, -2.5f, 0);
      newListing.transform.position = list.transform.position + adjust;
      newListing.transform.localScale = new Vector3(1, 1, 1);
      newListing.GetComponentInChildren<TMP_Text>().text = pilot.name;
      listingCounter++;
    }
  }

  public Loader.XWSquad GetSquad()
  {
    return loadedSquad;
  }

  // public int GetCharges(string name)
  // {
  //     foreach (TextAsset file in upgradeFiles)
  //     {
  //         string wrapFile = "{ \"array\" :" + file.ToString() + "}";
  //         upgrades = JsonUtility.FromJson<Wrapper>(wrapFile);
  //         foreach (UpgradeInfo upgrade in upgrades.array)
  //         {
  //             if (upgrade.xws == name)
  //             {
  //                 return upgrade.sides[0].charges.value;
  //             }
  //         }
  //     }
  //     return 0;
  // }

  // public int GetSides(string name)
  // {
  //     foreach (TextAsset file in upgradeFiles)
  //     {
  //         string wrapFile = "{ \"array\" :" + file.ToString() + "}";
  //         upgrades = JsonUtility.FromJson<Wrapper>(wrapFile);
  //         foreach (UpgradeInfo upgrade in upgrades.array)
  //         {
  //             if (upgrade.xws == name)
  //             {
  //                 return upgrade.sides.Length;
  //             }
  //         }
  //     }
  //     return 1;
  // }

  public int[] GetUpgradeInfo(string name)
  {
    foreach (TextAsset file in upgradeFiles)
    {
      string wrapFile = "{ \"array\" :" + file.ToString() + "}";
      upgrades = JsonUtility.FromJson<Wrapper>(wrapFile);
      foreach (UpgradeInfo upgrade in upgrades.array)
      {
        if (upgrade.xws.Replace("-", "") == name)
        {
          int[] info = {
                        upgrade.sides.Length,
                        upgrade.sides[0].charges.value,
                        upgrade.sides[0].force.value,
                    };
          return info;
        }
      }
    }
    return new int[3];
  }
  //TODO implement this method ^^ instead of the two seperate ones and then use the force value too
  // Add force to usedupgrades generation

  public ShipConfig GetConfigFile(string ship, string pilot)
  {
    // Check for pilots that have specific configs
    if (pilot == "autopilotdrone") { ship = "escapecraft[autopilot]"; }
    if (pilot == "l337escapecraft") { ship = "escapecraft[l337]"; }
    if (pilot == "r1j5") { ship = "fireball[r1j5]"; }
    if (pilot == "chopper") { ship = "vcx100lightfreighter[chopper]"; }
    if (pilot == "bb8") { ship = "resistancetransportpod[bb8]"; }

    return Resources.Load<ShipConfig>("ShipConfig/" + ship);
  }

  private string GenerateID()
  {
    string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    char[] stringChars = new char[8];

    for (int i = 0; i < stringChars.Length; i++)
    {
      int random = Random.Range(0, 58);
      stringChars[i] = chars[random];
    }

    string finalString = new string(stringChars);

    return finalString;
  }

  public void SetPlayerName(string name)
  {
    playerName = name;
  }

  public string GetPlayerName()
  {
    return playerName;
  }

  public void SetPlaymat(int selectedPlaymat)
  {
    playmat = selectedPlaymat;
  }

  public int GetPlaymat()
  {
    return playmat;
  }

  public void StartGame()
  {
    int currentScene = SceneManager.GetActiveScene().buildIndex;
    SceneManager.LoadScene(currentScene + 1);
  }

  public void Quit()
  {
    Application.Quit();
  }

  [System.Serializable]
  public class XWSquad
  {
    public string name;
    public string faction;
    public int points;
    public Pilot[] pilots;
    public bool[] damage = new bool[33];
  }

  [System.Serializable]
  public class Pilot
  {
    public string name;
    public string uniqueID;
    public string ship;
    public ShipConfig config;
    public int initiative;
    public Upgrades upgrades;
    public string ability;
    public int force;
    public int charges;
    public int faction;
    public List<DamageSaveData> damage = new List<DamageSaveData>();
  }

  [System.Serializable]
  public class Upgrades
  {
    public string[] astromech;
    public string[] cannon;
    public string[] configuration;
    public string[] crew;
    public string[] device;
    public string[] forcepower;
    public string[] gunner;
    public string[] illicit;
    public string[] missile;
    public string[] modification;
    public string[] sensor;
    public string[] talent;
    public string[] tech;
    public string[] title;
    public string[] torpedo;
    public string[] turret;

    public List<UsedUpgrade> usedUpgrades = new List<UsedUpgrade>();

    public string[][] all
    {
      get
      {
        string[][] all = { astromech, cannon, configuration, crew, device, forcepower,
                gunner, illicit, missile, modification, sensor , talent, tech, title , torpedo, turret};
        return all.Where(type => type != null).ToArray();
      }
    }
  }

  [System.Serializable]
  private class ShipInfo
  {
    public string xws;
    public string name;
    public PilotInfo[] pilots;
  }

  [System.Serializable]
  private class PilotInfo
  {
    public string xws;
    public string name;
    public int initiative;
    public string ability;
    public int duplicates;
    public Force force;
    public Charges charges;
  }

  // Used for accessing top level json properties that have no name
  [System.Serializable]
  private class Wrapper
  {
    public UpgradeInfo[] array;
  }

  [System.Serializable]
  private class UpgradeInfo
  {
    public string xws;
    public string name;
    public Side[] sides;
  }

  [System.Serializable]
  private class Side
  {
    public Charges charges;
    public Force force;
  }

  [System.Serializable]
  public class Charges
  {
    public int value;
  }

  [System.Serializable]
  private class Force
  {
    public int value;
  }

  [System.Serializable]
  public class UsedUpgrade
  {
    public string name;
    public int currentSide;
    public int remainingCharges;
    public int remainingForce;
  }

  [System.Serializable]
  public class SaveFile
  {
    public XWSquad squad_1;
    public XWSquad squad_2;
    public List<ShipSaveData> shipData;

    public List<HazardSaveData> hazards;
    public List<DeviceSaveData> devices;
  }

  [System.Serializable]
  public class ShipSaveData
  {
    public string name;
    public string id;
    public Vector3 position;
    public float angle;
    public int hull;
    public int shield;
    public int force;
    public int arcDirection;
    public int[] tokenCount;
    public string targetLock;
    public bool cloakState;
  }

  [System.Serializable]
  public class HazardSaveData
  {
    public string type;
    public int id;
    public Vector3 position;
    public float angle;
  }

  [System.Serializable]
  public class DeviceSaveData
  {
    public int type;
    public Vector3 position;
    public float angle;
  }

  [System.Serializable]
  public class DamageSaveData
  {
    public int cardIndex;
    public bool flipped;
  }

  public void pasteFromBrowser(string text)
  {
    inputField.text = text;
  }

  public void clientIsWebGL()
  {
    _isWebGL = true;
  }
}
