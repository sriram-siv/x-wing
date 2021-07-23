using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RelayDevice : MonoBehaviour
{
  const string PLAYMAT = "Playmat";

  [SerializeField] Loader.XWSquad player1Squad;
  [SerializeField] Loader.XWSquad player2Squad;

  [PunRPC]
  private void CacheSquad(string squad, bool loadFromFile)
  {
    player1Squad = JsonUtility.FromJson<Loader.XWSquad>(squad);
    foreach (Loader.Pilot pilot in player1Squad.pilots)
    {
      pilot.config = FindObjectOfType<Loader>().GetConfigFile(pilot.ship, pilot.name);
    }

    if (!PhotonNetwork.IsMasterClient && player2Squad != null && !loadFromFile)
    {
      gameObject.GetPhotonView().RPC(
          "CachePlayer2", RpcTarget.AllBuffered, JsonUtility.ToJson(FindObjectOfType<Loader>().GetSquad()));
    }
  }

  [PunRPC]
  private void CachePlayer2(string squad)
  {
    player2Squad = JsonUtility.FromJson<Loader.XWSquad>(squad);
    foreach (Loader.Pilot pilot in player2Squad.pilots)
    {
      pilot.config = FindObjectOfType<Loader>().GetConfigFile(pilot.ship, pilot.name);
    }
  }

  [PunRPC]
  private void SendRangeInfo(Vector3 start, Vector3 end, bool px)
  {
    FindObjectOfType<Menu>().DrawOpponentRange(start, end, px);
  }

  [PunRPC]
  private void SendDiceInfo(string type, string[] results)
  {
    FindObjectOfType<Menu>().ReceiveDiceInfo(type, results);
  }

  [PunRPC]
  private void SetPlaymat(int selectedPlaymat)
  {
    GameObject.Find(PLAYMAT).GetComponent<SpriteRenderer>().sprite =
        Resources.Load<Sprite>("Playmat/" + selectedPlaymat);
  }

  [PunRPC]
  private void SetupMode()
  {
    FindObjectOfType<Menu>().SetupMode();
  }

  [PunRPC]
  private void SendAlertMessage(string message, int duration)
  {
    GameController controller = FindObjectOfType<GameController>();
    StartCoroutine(controller.SetAlertMessage(message, duration, true));
  }

  [PunRPC]
  private void SetPlayerReady(string player, bool state)
  {
    FindObjectOfType<GameController>().SetOpponentReady(player, state);
  }

  public Loader.XWSquad GetSquad(int playerNumber)
  {
    Loader.XWSquad squad = playerNumber == 1
        ? player1Squad
        : player2Squad;

    return squad;
  }

  [PunRPC]
  private void UpdateUpgrade(int playerNumber, string pilotName, string usedUpgrade)
  {
    Loader.UsedUpgrade updatedUpgrade = JsonUtility.FromJson<Loader.UsedUpgrade>(usedUpgrade);

    Loader.XWSquad squad = playerNumber == 1
        ? player1Squad
        : player2Squad;

    foreach (Loader.Pilot pilot in squad.pilots)
    {
      if (pilot.name == pilotName)
      {
        for (int i = 0; i < pilot.upgrades.usedUpgrades.Count; i++)  //(Loader.UsedUpgrade upgrade in pilot.upgrades.usedUpgrades)
        {
          if (pilot.upgrades.usedUpgrades[i].name == updatedUpgrade.name)
          {
            pilot.upgrades.usedUpgrades[i] = updatedUpgrade;
          }
        }
      }
    }

    FindObjectOfType<Menu>().UpdateUpgrades();
  }

  [PunRPC]
  private void UpdateDamage(int playerNumber, string pilotName, int card, bool state)
  {
    Loader.XWSquad squad = playerNumber == 1
        ? player1Squad
        : player2Squad;

    foreach (Loader.Pilot pilot in squad.pilots)
    {
      if (pilot.name == pilotName)
      {
        bool exists = false;
        foreach (Loader.DamageSaveData damage in pilot.damage)
        {
          if (damage.cardIndex == card)
          {
            damage.flipped = state;
            exists = true;
            break;
          }
        }

        if (!exists)
        {
          pilot.damage.Add(new Loader.DamageSaveData()
          {
            cardIndex = card,
            flipped = state,
          });
        }
      }
    }

    squad.damage[card] = true;

    FindObjectOfType<Menu>().UpdateUpgrades();
  }

  [PunRPC]
  private void DeleteDamage(int playerNumber, string pilotName, int card)
  {
    Loader.XWSquad squad = playerNumber == 1
        ? player1Squad
        : player2Squad;

    foreach (Loader.Pilot pilot in squad.pilots)
    {
      if (pilot.name == pilotName)
      {
        foreach (Loader.DamageSaveData damage in pilot.damage)
        {
          if (damage.cardIndex == card)
          {
            pilot.damage.Remove(damage);
            break;
          }
        }
      }
    }

    squad.damage[card] = false;

    FindObjectOfType<Menu>().UpdateUpgrades();
  }

  [PunRPC]
  private void RenameShip(int playerNumber, string oldName, string newname)
  {
    Loader.XWSquad squad = playerNumber == 1
        ? player1Squad
        : player2Squad;

    foreach (Loader.Pilot pilot in squad.pilots)
    {
      if (pilot.name == oldName)
      {
        pilot.name = newname;
      }
    }

    FindObjectOfType<Menu>().UpdateUpgrades();
  }

  public bool CheckNameInUse(string name)
  {
    bool nameInUse = false;

    foreach (Loader.Pilot pilot in player1Squad.pilots)
    {
      if (pilot.name.ToLower() == name)
      {
        nameInUse = true;
      }
    }
    foreach (Loader.Pilot pilot in player2Squad.pilots)
    {
      if (pilot.name.ToLower() == name)
      {
        nameInUse = true;
      }
    }

    return nameInUse;
  }

  [PunRPC]
  private void DeleteAllObjects()
  {
    foreach (Ship ship in FindObjectsOfType<Ship>()) { Destroy(ship.gameObject); }
    foreach (Hazards hazard in FindObjectsOfType<Hazards>()) { Destroy(hazard.gameObject); }
    foreach (Bomb device in FindObjectsOfType<Bomb>()) { Destroy(device.gameObject); }

    Menu menu = FindObjectOfType<Menu>();
    menu.OpenHand();
    foreach (Dial dial in FindObjectsOfType<Dial>()) { Destroy(dial.gameObject); }
    menu.SwitchHand();
    foreach (Dial dial in FindObjectsOfType<Dial>()) { Destroy(dial.gameObject); }
    menu.OpenHand();

    menu.ClearOppoonentDials(false);
  }
}
