using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Item : MonoBehaviour
{
    public GameObject Shop;
    public string itemName;
    public float[] StatBuffs;
    public int goldCost;
    public string specialEffect;
    public GameObject Description;
    public int itemNumber;
    public bool isStarterItem;
    public bool isDiscounted = true;

    public TMP_Text costText;


    public void Update()
    {
        costText.text = "Gold Cost: " + goldCost;
        //Add a feature to show the first item bought is at a discount
    }

    public void Click()
    {
        Shop.GetComponent<Shop>().ItemBought(itemNumber);
    }

    public void MouseEnter()
    {
        Description.SetActive(true);
        Shop.GetComponent<Shop>().HoveringDialogue(itemNumber);
    }
    public void MouseExit()
    {
        Description.SetActive(false);
    }

}
