using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public GameObject Shop;
    public string itemName;
    public float[] StatBuffs;
    public int goldCost;
    public string specialEffect;
    public GameObject Description;
    public int itemNumber;


    public void Click()
    {
        Shop.GetComponent<Shop>().ItemBought(itemNumber);
    }

    public void MouseEnter()
    {
        Debug.Log("??");
        Description.SetActive(true);
        Shop.GetComponent<Shop>().HoveringDialogue(itemNumber);
    }
    public void MouseExit()
    {
        Description.SetActive(false);
    }
}
