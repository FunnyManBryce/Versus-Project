using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class KnockbackFeedback : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb2d;

    [SerializeField] float strength = 16, delay = 0.15f;
    public UnityEvent OnBegin, OnDone;

    private float knockbackMultiplier = 1.0f;

    public void SetKnockbackMultiplier(float multiplier)
    {
        knockbackMultiplier = multiplier;
    }

    public void PlayFeedback(GameObject sender)
    {
        StopAllCoroutines();
        OnBegin?.Invoke();
        Vector2 direction = (transform.position - sender.transform.position).normalized;
        rb2d.AddForce(direction * strength * knockbackMultiplier, ForceMode2D.Impulse);
        StartCoroutine(Reset());
    }

    private IEnumerator Reset()
    {
        yield return new WaitForSeconds(delay);
        rb2d.velocity = Vector3.zero;
        OnDone?.Invoke();
    }
}
