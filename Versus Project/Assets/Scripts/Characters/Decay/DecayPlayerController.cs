using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecayPlayerController : BasePlayerController
{
    public LameManager lameManager;
    // Start is called before the first frame update
    void Start()
    {
        lameManager = FindObjectOfType<LameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
