using UnityEngine.Audio;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;

    public AudioClip clip;

    public bool loop;

    [Range(0.0f, 1.0f)]
    public float spatialBlend;
    [Range(0f, 3f)]
    public float initialVolume;
    [Range(0f, 3f)]
    public float volume;
    [Range(.1f, 3f)]
    public float pitch;
    [Range(0f, 360f)]
    public float spread;

    [HideInInspector]
    public AudioSource source;
}
