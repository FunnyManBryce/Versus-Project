using UnityEngine;
using UnityEngine.Events;

public class AnimationHelper : MonoBehaviour
{
    public UnityEvent OnAnimationEventTriggered, OnAttackPerformed, EnemyAttackPerformed;

    public void TriggerEvent()
    {
        OnAnimationEventTriggered?.Invoke();
    }

    public void TriggerAttack()
    {
        OnAttackPerformed?.Invoke();
    }

    public void TriggerEnemyAttack()
    {
        EnemyAttackPerformed?.Invoke();
    }
}
