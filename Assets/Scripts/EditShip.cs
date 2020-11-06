using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class EditShip : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;
    Dial dial;
    Ship ship;
    int savedColor = 0;

    void Start()
    {
        dial = GetComponentInParent<Dial>();
        ship  = dial.GetAttachedShip();

        Sprite[] colorSchemes = ship.GetColorShemes();
        List<string> colorOptions = new List<string>() {
            { "default" },
        };

        for (int i = 1; i < colorSchemes.Length; i++)
        {
            string colorName = colorSchemes[i].name.Split('-')[1];
            colorOptions.Add(colorName);
        }

        Dropdown colorPicker = settingsPanel.GetComponentInChildren<Dropdown>();
        colorPicker.ClearOptions();
        colorPicker.AddOptions(colorOptions);
    }

    void Update()
    {
        
    }

    void OnMouseDown()
    {
        ToggleSettings();
    }

    public void ToggleSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);

        settingsPanel.GetComponentInChildren<InputField>().text = ship.name;
        settingsPanel.GetComponentInChildren<Dropdown>().value = savedColor;
        ship.ChangeColorScheme(savedColor);
    }

    public void SaveSettings()
    {
        RenameShip();

        savedColor = settingsPanel.GetComponentInChildren<Dropdown>().value;

        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void RenameShip()
    {
        string newName = settingsPanel.GetComponentInChildren<InputField>().text.ToLower();
        string oldName = ship.name.ToLower();

        if (newName == oldName)
        {
            return;
        }

        if (newName == "")
        {
            StartCoroutine(FindObjectOfType<GameController>().SetAlertMessage("pilot name cannot be blank", 5, false));
            return;
        }

        bool nameInUse = FindObjectOfType<RelayDevice>().CheckNameInUse(newName);
        if (nameInUse)
        {
            StartCoroutine(FindObjectOfType<GameController>().SetAlertMessage("name already in use", 5, false));
            return;
        }

        // Rename in all relevant places
        dial.RenameDial(newName);
        
        ship.gameObject.GetPhotonView().RPC(
            "RenameShip",
            RpcTarget.AllBuffered,
            newName
        );

        // Update relay device
        FindObjectOfType<RelayDevice>().gameObject.GetPhotonView().RPC(
            "RenameShip",
            RpcTarget.AllBuffered,
            FindObjectOfType<GameController>().GetPlayerNumber(),
            oldName,
            newName
        );
    }

    public void ChangeColorScheme(int option)
    {
        ship.ChangeColorScheme(option);
    }
}
