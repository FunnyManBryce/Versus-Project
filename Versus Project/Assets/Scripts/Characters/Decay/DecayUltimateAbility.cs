/*using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class DecayUltimateAbility : Ability<DecayPlayerController>
{
    BasePlayerController enemyPlayer;
    [Rpc(SendTo.Server)]
    public override void OnUse()
    {
        base.OnUse();
        if (playerController.health.Team.Value == 1)
        {
            enemyPlayer = playerController.lameManager.playerTwoChar.GetComponent<BasePlayerController>();
        }
        else if (playerController.health.Team.Value == 2)
        {
            enemyPlayer = playerController.lameManager.playerOneChar.GetComponent<BasePlayerController>();
        }
        enemyPlayer.TriggerBuffServerRpc("Attack Damage", -playerController.totalStatDecay.Value, 10f);
        enemyPlayer.TriggerBuffServerRpc("Armor", -playerController.totalStatDecay.Value, 10f);
        enemyPlayer.TriggerBuffServerRpc("Auto Attack Speed", -(0.1f * playerController.totalStatDecay.Value), 10f);
        enemyPlayer.TriggerBuffServerRpc("Armor Pen", -playerController.totalStatDecay.Value, 10f);
        enemyPlayer.TriggerBuffServerRpc("Regen", -(0.05f * playerController.totalStatDecay.Value), 10f);
        enemyPlayer.TriggerBuffServerRpc("Mana Regen", -(0.05f * playerController.totalStatDecay.Value), 10f);
        playerController.TriggerBuffServerRpc("Attack Damage", playerController.totalStatDecay.Value, 10f);
        playerController.TriggerBuffServerRpc("Armor", playerController.totalStatDecay.Value, 10f);
        playerController.TriggerBuffServerRpc("Auto Attack Speed", (0.1f * playerController.totalStatDecay.Value), 10f);
        playerController.TriggerBuffServerRpc("Armor Pen", playerController.totalStatDecay.Value, 10f);
        playerController.TriggerBuffServerRpc("Regen", (0.05f * playerController.totalStatDecay.Value), 10f);
        playerController.TriggerBuffServerRpc("Mana Regen", (0.05f * playerController.totalStatDecay.Value), 10f);
        playerController.TriggerBuffServerRpc("Speed", 3f, 10f);
    }
}
*/