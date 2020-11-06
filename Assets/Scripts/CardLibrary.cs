using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardLibrary : MonoBehaviour
{
    [SerializeField] Sprite[] cardImages;

    void Start()
    {

    }

    void Update()
    {
        
    }

    public Sprite GetCard(string xws)
    {
        for (int i = 0; i < cardImages.Length; i++)
        {
            if (cardImages[i].name == xws)
            {
                return cardImages[i];
            }
        }
        return cardImages[0];
    }
}
