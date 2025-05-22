using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatDisplay : MonoBehaviour
{
    public bool isOpen = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isOpen = !isOpen;
        }
        if (isOpen && Vector3.Distance(transform.position, new Vector3(-660, -200, 0)) <= 100f)
        {
            gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(-660, -200, 0);

        }
        else if (isOpen)
        {
            var speed = 1000 * Time.deltaTime;
            gameObject.GetComponent<RectTransform>().anchoredPosition = Vector3.MoveTowards(gameObject.GetComponent<RectTransform>().anchoredPosition, new Vector3(-660, -200, 0), speed);
        }
        if (!isOpen && Vector3.Distance(transform.position, new Vector3(-1260, -200, 0)) <= 100f)
        {
            gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(-1260, -200, 0);

        }
        else if (!isOpen)
        {
            var speed = 1000 * Time.deltaTime;
            gameObject.GetComponent<RectTransform>().anchoredPosition = Vector3.MoveTowards(gameObject.GetComponent<RectTransform>().anchoredPosition, new Vector3(-1260, -200, 0), speed);
        }
    }

    public void Open()
    {
        isOpen = !isOpen;
    }


}
