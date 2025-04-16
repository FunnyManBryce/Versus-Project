using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public GameObject Shop;
    public string itemName;
    public float[] StatBuffs;
    public int goldCost;
    public string specialEffect;
    public string Description;
    public int itemNumber;

    public void Click()
    {
        Shop.GetComponent<Shop>().ItemBought(itemNumber);
    }
}
