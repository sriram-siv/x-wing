using UnityEngine;

namespace Photon.Pun.Demo.PunBasics
{
  public class GameManager : MonoBehaviourPunCallbacks
  {
    [SerializeField] GameObject player1SpawnPosition;
    [SerializeField] GameObject player2SpawnPosition;
    GameObject spawnPosition;
    [SerializeField] GameObject asteroidSpawn;
    const string PLAY_AREA = "Play Area";
    const string HAZARDS = "Hazards";

    void Start()
    {
      Loader loader = FindObjectOfType<Loader>();

      spawnPosition = player1SpawnPosition;

      if (!PhotonNetwork.IsConnected)
      {
        FindObjectOfType<Menu>().QuitToMainMenu();
        return;
      }

      if (PlayerManager.LocalPlayerInstance == null)
      {
        if (PhotonNetwork.IsMasterClient)
        {
          for (int i = 0; i < 6; i++)
          {
            GameObject asteroid = PhotonNetwork.InstantiateSceneObject("photonAsteroid",
                asteroidSpawn.transform.position, transform.rotation, 0);
            asteroid.GetPhotonView().RPC("SetAsteroidImage", RpcTarget.AllBuffered, i);

            asteroidSpawn.transform.position += new Vector3(10, 0);
            asteroid.transform.SetParent(GameObject.Find(HAZARDS).transform);
          }
        }

        else
        {
          spawnPosition = player2SpawnPosition;
        }
      }

      Loader.XWSquad squad = loader.GetSquad();

      foreach (Loader.Pilot pilot in squad.pilots)
      {
        GameObject newShip;
        if (pilot.config.sizeNum == 0)
        {
          newShip = PhotonNetwork.Instantiate("photonShip",
              spawnPosition.transform.position, spawnPosition.transform.rotation, 0);
        }
        else
        {
          newShip = PhotonNetwork.Instantiate("photonShipLarge",
              spawnPosition.transform.position, spawnPosition.transform.rotation, 0);
        }

        newShip.GetPhotonView().RPC("ConfigureShip", RpcTarget.AllBuffered, JsonUtility.ToJson(pilot));

        spawnPosition.transform.position += new Vector3(0, 8);

        FindObjectOfType<Menu>().LoadDials(pilot, 1); // 1 is the playerNumber, will need to be dynamically set if this script is used again
      }
    }

    public void DisconnectFromNetwork()
    {
      PhotonNetwork.Disconnect();
    }

  }
}
