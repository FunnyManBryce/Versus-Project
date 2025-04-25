using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

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

    public string[] purchasingDialogue;
    public string[] poorDialogue;
    public string[] needResevoirDialogue;
    public string[] idleDialogue;
    public string[] hoverDialogue;
    public string[] enteringDialogue;
    public string[] exitingDialogue;
    public bool isYapping;
    public GameObject Text;
    float yapTimer;
    public float timeForYap;

    public float idleTimer = 15;

    void Update()
    {
        if(isYapping && yapTimer >= 0)
        {
            yapTimer -= Time.deltaTime;
        } else if(isYapping && yapTimer <= 0)
        {
            isYapping = false;
            yapTimer = timeForYap;
            animator.SetBool("isYapping", isYapping);
            Text.GetComponent<TextMeshProUGUI>().text = "";
        }
        if(isYapping && !animator.GetBool("isYapping") && !animator.GetBool("Closing"))
        {
            isYapping = false;
            yapTimer = timeForYap;
            animator.SetBool("isYapping", isYapping);
            Text.GetComponent<TextMeshProUGUI>().text = "";
        }
        if(isOpen && !isYapping && idleTimer >= 0)
        {
            idleTimer -= Time.deltaTime;
        } else if(isOpen && !isYapping && idleTimer <= 0)
        {
            idleTimer = 15;
            isYapping = true;
            animator.SetBool("isYapping", isYapping);
            Text.GetComponent<TextMeshProUGUI>().text = idleDialogue[Random.Range(0, idleDialogue.Length)];
            yapTimer = timeForYap;
        }
        if(!isOpen)
        {
            idleTimer = 15;
        }
        if(isOpen && Vector3.Distance(transform.position, new Vector3(460,120,0)) <= 100f) 
        {
            gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(460, 120, 0);

        }
        else if(isOpen)
        {
            var speed = 1000 * Time.deltaTime;
            gameObject.GetComponent<RectTransform>().anchoredPosition = Vector3.MoveTowards(gameObject.GetComponent<RectTransform>().anchoredPosition, new Vector3(460, 120, 0), speed);
        }
        if (!isOpen && Vector3.Distance(transform.position, new Vector3(1227, 120, 0)) <= 100f)
        {
            gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(1227, 120, 0);

        }
        else if (!isOpen)
        {
            var speed = 1000 * Time.deltaTime;
            gameObject.GetComponent<RectTransform>().anchoredPosition = Vector3.MoveTowards(gameObject.GetComponent<RectTransform>().anchoredPosition, new Vector3(1227, 120, 0), speed);
        }
    }

    public void OpenButton()
    {
        if (animator.GetBool("Opening") || animator.GetBool("Closing")) return;
        if(!isOpen)
        {
            animator.SetBool("Opening", true);
        } else
        {
            animator.SetBool("Closing", true);
            isYapping = true;
            Text.GetComponent<TextMeshProUGUI>().text = exitingDialogue[Random.Range(0, exitingDialogue.Length)];
            yapTimer = timeForYap + 1;
        }
    }
    public void OpenShop()
    {
        animator.SetBool("Opening", false);
        animator.SetBool("Closing", false);
        isOpen = !isOpen;
        if (isOpen)
        {
            isYapping = true;
            animator.SetBool("isYapping", isYapping);
            Text.GetComponent<TextMeshProUGUI>().text = enteringDialogue[Random.Range(0, enteringDialogue.Length)];
            yapTimer = timeForYap;
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
        if (animator.GetBool("Opening") || animator.GetBool("Closing")) return;
        GameObject item = CurrentItems[itemNumber];
        Item itemScript = item.GetComponent<Item>();
        if (itemScript.goldCost <= playerController.Gold.Value)
        {
            if (!playerController.resevoirRegen.Value)
            {
                isYapping = true;
                animator.SetBool("isYapping", isYapping);
                Text.GetComponent<TextMeshProUGUI>().text = needResevoirDialogue[Random.Range(0, needResevoirDialogue.Length)];
                yapTimer = timeForYap;
            }
            else
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
                if (itemScript.specialEffect == "Player Kill Money")
                {
                    playerController.ItemEffectServerRpc("Player Kill Money", 0);
                }
                if (itemScript.specialEffect == "25% Lifesteal")
                {
                    playerController.ItemEffectServerRpc("Lifesteal", 0.25f);
                }
                if (itemScript.specialEffect == "10% Lifesteal")
                {
                    playerController.ItemEffectServerRpc("Lifesteal", 0.1f);
                }
                Destroy(item);
                isYapping = true;
                animator.SetBool("isYapping", isYapping);
                Text.GetComponent<TextMeshProUGUI>().text = purchasingDialogue[Random.Range(0, purchasingDialogue.Length)];
                yapTimer = timeForYap;
            }
        }
        else
        {
            isYapping = true;
            animator.SetBool("isYapping", isYapping);
            Text.GetComponent<TextMeshProUGUI>().text = poorDialogue[Random.Range(0, poorDialogue.Length)];
            yapTimer = timeForYap;
        }
        
    }

    public void HoveringDialogue(int itemNumber)
    {
        if (animator.GetBool("Opening") || animator.GetBool("Closing")) return;
        isYapping = true;
        animator.SetBool("isYapping", isYapping);
        Text.GetComponent<TextMeshProUGUI>().text = hoverDialogue[itemNumber];
        yapTimer = timeForYap;
    }
}
