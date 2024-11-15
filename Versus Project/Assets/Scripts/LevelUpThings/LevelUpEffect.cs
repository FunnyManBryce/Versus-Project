using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpEffect : MonoBehaviour
{
    public string buttonText;
    public Sprite buttonIcon;
    
    public virtual void ApplyEffect()
    {
        Debug.Log("Applied effect: " + name);
    }
}
