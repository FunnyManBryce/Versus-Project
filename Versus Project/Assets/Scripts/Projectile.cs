using Unity.Netcode;
using UnityEngine;
using System.Collections;
/*
public class Projectile : MonoBehaviour
{
    public Rigidbody2D rb2d;
    public SpriteRenderer spriteRenderer;

    public float damage;
    public float maxRange;
    public float lifetime = 5f;
    public float projSpeed;
    public Vector2 startPosition;
    private NetworkVariable<int> ownerTeam = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        startPosition = transform.position;

        if (IsServer)
        {
            StartCoroutine(DestroyAfterLifetime());
        }
    }

    private IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);

        if (IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    private void Update()
    {

    }

    [Rpc(SendTo.Server)]
    public void SetProjectileDetailsServerRpc(int team, float damageAmount, float speed)
    {
        ownerTeam.Value = team;
        damage = damageAmount;
        projSpeed = speed;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        NetworkObject targetObject = collision.GetComponent<NetworkObject>();

        //Spawn through networkobject.
        if (targetObject != null && targetObject.NetworkObjectId != NetworkObject.NetworkObjectId)
        {
            NetworkObjectReference targetReference = new NetworkObjectReference(targetObject);
            NetworkObjectReference senderReference = new NetworkObjectReference(NetworkObject);

            if (targetObject.CompareTag("Player"))
            {
                // Player damage logic (placeholder)
                Debug.Log("Player hit by projectile");
            }
            else if (targetObject.CompareTag("Minion"))
            {
                MeleeMinion minionScript = targetObject.GetComponent<MeleeMinion>();
                if (minionScript != null && minionScript.Team != ownerTeam.Value)
                {
                    minionScript.TakeDamageServerRPC(damage, senderReference);
                }
            }
            else if (targetObject.CompareTag("Tower"))
            {
                Tower towerScript = targetObject.GetComponent<Tower>();
                if (towerScript != null && towerScript.Team != ownerTeam.Value)
                {
                    towerScript.TakeDamageServerRPC(damage, senderReference);
                }
            }
            NetworkObject.Despawn();
        }
    }
}
*/