using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Inhibitor : Tower
{
    void Start()
    {
        networkTower = tower.GetComponent<NetworkObject>();
        lameManager = FindObjectOfType<LameManager>();
    }

    protected override void Update()
    {
        if(Team == 1)
        {
            if (lameManager.teamOneTowersLeft.Value != orderInLane)
            {
                health.invulnerable = true;
            }
            else
            {
                health.invulnerable = false;
            }
        } else if(Team == 2)
        {
            if (lameManager.teamTwoTowersLeft.Value != orderInLane)
            {
                health.invulnerable = true;
            }
            else
            {
                health.invulnerable = false;
            }
        }
    }
}
