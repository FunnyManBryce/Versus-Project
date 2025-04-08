using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BryceAudioManager : NetworkBehaviour
{
    public GameObject audioPrefab;
    public VolumeControl volumeControl;
    public Sound[] sounds;
    public List<AudioSource> sourcesPlaying;
    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            //volumeControl.audioSource.Add(s.source);
            s.source.clip = s.clip;
            s.source.spread = s.spread;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.spatialBlend = s.spatialBlend;
            s.source.minDistance = 10;
        }
    }

    public void Update()
    {
        if (sourcesPlaying.Count <= 0) return;
        for (int i = 0; i < sourcesPlaying.Count; i++)
        {
            if (!sourcesPlaying[i].isPlaying)
            {
                Destroy(sourcesPlaying[i].gameObject);
                sourcesPlaying.Remove(sourcesPlaying[i]);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayServerRpc(string name, Vector3 spawnLocation)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        var audio = Instantiate(audioPrefab, spawnLocation, Quaternion.identity);
        AudioSource audioSource = audio.GetComponent<AudioSource>();
        audioSource.clip = s.clip;
        audioSource.spread = s.spread;
        audioSource.volume = s.volume;
        audioSource.pitch = s.pitch;
        audioSource.loop = s.loop;
        audioSource.spatialBlend = s.spatialBlend;
        audioSource.minDistance = 10;
        audioSource.Play();
        sourcesPlaying.Add(audioSource);
    }

    [ClientRpc(RequireOwnership = false)]
    public void PlayClientRpc(string name, Vector3 spawnLocation)
    {
        if (IsServer) return;
        Sound s = Array.Find(sounds, sound => sound.name == name);
        var audio = Instantiate(audioPrefab, spawnLocation, Quaternion.identity);
        AudioSource audioSource = audio.GetComponent<AudioSource>();
        audioSource.clip = s.clip;
        audioSource.spread = s.spread;
        audioSource.volume = s.volume;
        audioSource.pitch = s.pitch;
        audioSource.loop = s.loop;
        audioSource.spatialBlend = s.spatialBlend;
        audioSource.minDistance = 10;
        audioSource.Play();
        sourcesPlaying.Add(audioSource);
    }

    public void Play(string name, Vector3 spawnLocation)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        var audio = Instantiate(audioPrefab, spawnLocation, Quaternion.identity);
        AudioSource audioSource = audio.GetComponent<AudioSource>();
        audioSource.clip = s.clip;
        audioSource.spread = s.spread;
        audioSource.volume = s.volume;
        audioSource.pitch = s.pitch;
        audioSource.loop = s.loop;
        audioSource.spatialBlend = s.spatialBlend;
        audioSource.minDistance = 10;
        audioSource.Play();
        sourcesPlaying.Add(audioSource);
    }

    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Stop();
    }
}
