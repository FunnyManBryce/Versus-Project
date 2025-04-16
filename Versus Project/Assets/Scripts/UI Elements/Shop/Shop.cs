using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    public GameObject[] CurrentItems;
    public GameObject player;
    [SerializeField] private Button openShopButton;

    public Animator animator;
    public bool isOpen;

    [SerializeField] private BasePlayerController playerController;
    public bool initialized;
    [SerializeField] private bool isPlayer1UI;    // Start is called before the first frame update

    public void Awake()
    {
    }

    public void OpenButton()
    {
        if (animator.GetBool("Opening")) return;
        animator.SetBool("Opening", true);
    }
    public void OpenShop()
    {
        animator.SetBool("Opening", false);
        isOpen = !isOpen;
        if (isOpen)
        {
            gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(460, 120, 0);
        }
        else
        {
            gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(1227, 120, 0);
        }
    }

    void Start()
    {
        isPlayer1UI = transform.root.name.Contains("1");
        FindAndSetPlayerController();
        
        for (int i = 0; i < CurrentItems.Length; i++)
        {
            GameObject item = CurrentItems[i];
            Item itemScript = item.GetComponent<Item>();
            itemScript.Shop = gameObject;
            itemScript.itemNumber = i;
        }
    }

    private void FindAndSetPlayerController()
    {
        var players = Object.FindObjectsOfType<BasePlayerController>();

        foreach (var player in players)
        {
            if (player.NetworkObject.IsSpawned)
            {
                bool isPlayer1 = player.NetworkObject.OwnerClientId == 0;

                if (isPlayer1 == isPlayer1UI)
                {
                    playerController = player;
                    if (playerController != null)
                    {
                        initialized = true;
                        break;
                    }
                }
            }
        }
    }

    public void ItemBought(int itemNumber)
    {
        if (!playerController.resevoirRegen)
        {
            //Dialogue for not being near resevoir
        }
        else
        {
            GameObject item = CurrentItems[itemNumber];
            Item itemScript = item.GetComponent<Item>();
            if (itemScript.goldCost <= playerController.Gold.Value)
            {
                playerController.ItemEffectServerRpc("Gold", itemScript.goldCost);
                playerController.ItemEffectServerRpc("Speed", itemScript.StatBuffs[0]);
                playerController.ItemEffectServerRpc("Attack Damage", itemScript.StatBuffs[1]);
                playerController.ItemEffectServerRpc("Armor", itemScript.StatBuffs[2]);
                playerController.ItemEffectServerRpc("Armor Pen", itemScript.StatBuffs[3]);
                playerController.ItemEffectServerRpc("Auto Attack Speed", itemScript.StatBuffs[4]);
                playerController.ItemEffectServerRpc("Regen", itemScript.StatBuffs[5]);
                playerController.ItemEffectServerRpc("Mana Regen", itemScript.StatBuffs[6]);
                playerController.ItemEffectServerRpc("Max Mana", itemScript.StatBuffs[7]);
                playerController.ItemEffectServerRpc("CDR", itemScript.StatBuffs[8]);
                playerController.ItemEffectServerRpc("Health", itemScript.StatBuffs[9]);
                if (itemScript.specialEffect == "something")
                {
                    //Add special effect to player character for purchasing
                }
                Destroy(item);
                //Dialogue for item purchase
            }
            else
            {
                //Dialogue for being too poor
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
