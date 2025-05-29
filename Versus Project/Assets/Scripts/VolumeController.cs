using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    public Slider volumeSlider;
    public BryceAudioManager BAM;

    private const string VolumeKey = "VolumeLevel";

    public void Start()
    {
        BAM = FindAnyObjectByType<BryceAudioManager>().GetComponent<BryceAudioManager>();
        float newVolume = PlayerPrefs.GetFloat("VolumeLevel");
        volumeSlider.value = newVolume;
        foreach (Sound a in BAM.sounds)
        {
            a.volume = a.iniitalVolume * volumeSlider.value;
        }
        for(int i = 0; i < BAM.sourcesPlaying.Count; i++)
        {
            BAM.sourcesPlaying[i].volume = BAM.sourceVolume[i] * volumeSlider.value;
        }
    }

    public void ChangeVolume(float Volume)
    {
        float newVolume = Volume;
        foreach (Sound a in BAM.sounds)
        {
            a.volume = a.iniitalVolume * volumeSlider.value;
        }
        for (int i = 0; i < BAM.sourcesPlaying.Count; i++)
        {
            BAM.sourcesPlaying[i].volume = BAM.sourceVolume[i] * volumeSlider.value;
        }
        PlayerPrefs.SetFloat(VolumeKey, newVolume);
        PlayerPrefs.Save();
    }
}
