using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    public GameObject[] ItemPrefabs;
    public Vector3[] ItemPositions;
    public GameObject[] CurrentItems;
    public GameObject player;
    [SerializeField] private Button openShopButton;


    public bool isOpen;

    [SerializeField] private BasePlayerController playerController;
    public bool initialized;
    [SerializeField] private bool isPlayer1UI;    // Start is called before the first frame update

    public void Awake()
    {
        openShopButton.onClick.AddListener(() =>
        {
            isOpen = !isOpen;
            if(isOpen)
            {
                //Animation to open shop
            } else
            {
                //Animation to close shop
            }
        });
    }
    void Start()
    {
        isPlayer1UI = transform.root.name.Contains("1");
        FindAndSetPlayerController();
        
        for (int i = 0; i < ItemPrefabs.Length; i++)
        {
            GameObject item = Instantiate(ItemPrefabs[i],gameObject.transform.position + ItemPositions[i], Quaternion.identity, gameObject.transform);
            CurrentItems[i] = item;
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
        GameObject item = CurrentItems[itemNumber];
        Item itemScript = item.GetComponent<Item>();
        if(itemScript.goldCost <= playerController.Gold.Value)
        {
            playerController.Gold.Value -= itemScript.goldCost;
            playerController.BaseSpeed.Value += itemScript.StatBuffs[0];
            playerController.BaseDamage.Value += itemScript.StatBuffs[1];
            playerController.BaseArmor.Value += itemScript.StatBuffs[2];
            playerController.BaseArmorPen.Value += itemScript.StatBuffs[3];
            playerController.BaseAttackSpeed.Value += itemScript.StatBuffs[4];
            playerController.BaseRegen.Value += itemScript.StatBuffs[5];
            playerController.BaseManaRegen.Value += itemScript.StatBuffs[6];
            playerController.TriggerBuffServerRpc("Max Mana", itemScript.StatBuffs[7], 0, false);
            playerController.BaseCDR.Value += itemScript.StatBuffs[8];
            playerController.TriggerBuffServerRpc("Health", itemScript.StatBuffs[9], 0, false);
            if(itemScript.specialEffect == "something")
            {
                //Add special effect to player character for purchasing
            }
            Destroy(item);
            //Dialogue for item purchase
        } else
        {
            //Dialogue for being too poor
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
