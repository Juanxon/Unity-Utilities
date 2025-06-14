using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundsSO soundsData;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] [Range(0f, 1f)] private float globalVolume = 1f;

    [Header("Audio Source Pool Settings")]
    [SerializeField] private int initialPoolSize = 5;
    [SerializeField] private int maxPoolSize = 20;

    private AudioSource mainAudioSource;
    private const float MIN_DECIBELS = -80f;

    private ObjectPool<AudioSource> audioSourcePool;
    private Dictionary<AudioSource, Coroutine> activeAudioSources = new Dictionary<AudioSource, Coroutine>();

    private void Awake()
    {
        mainAudioSource = GetComponent<AudioSource>();

        audioSourcePool = new ObjectPool<AudioSource>(
            createFunc: CreatePooledAudioSource,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: true,
            defaultCapacity: initialPoolSize,
            maxSize: maxPoolSize
        );

        // Pre-populate pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            var src = audioSourcePool.Get();
            audioSourcePool.Release(src);
        }
    }

    private void Start()
    {
        if (audioMixer != null)
            SetGlobalVolume(globalVolume);
    }

    private void OnDestroy()
    {
        foreach (var coroutine in activeAudioSources.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeAudioSources.Clear();
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (audioMixer == null) return;

        float volumeDB = LinearToDecibel(globalVolume);
        if (Application.isPlaying)
        {
            audioMixer.SetFloat(masterVolumeParameter, volumeDB);
            Debug.Log($"Setting {masterVolumeParameter} to {volumeDB}dB");
        }
        else
        {
            Debug.Log($"Edit mode: Volume will be set to {volumeDB}dB on play");
        }
    }
    #endif

    /// <summary>
    /// Sets the global volume (0-1) on the AudioMixer.
    /// </summary>
    public void SetGlobalVolume(float volume)
    {
        globalVolume = Mathf.Clamp01(volume);

        if (audioMixer != null)
        {
            float volumeDB = LinearToDecibel(globalVolume);
            audioMixer.SetFloat(masterVolumeParameter, volumeDB);
        }
    }

    private float LinearToDecibel(float linearVolume)
    {
        return (linearVolume <= 0f) ? MIN_DECIBELS : Mathf.Log10(linearVolume) * 20f;
    }

    private float DecibelToLinear(float decibelVolume)
    {
        return Mathf.Pow(10f, decibelVolume / 20f);
    }

    /// <summary>
    /// Plays a sound by name, using SoundsSO for lookup.
    /// </summary>
    public void PlaySound(string soundName)
    {
        if (soundsData == null)
        {
            Debug.LogWarning("SoundManager: No SoundsSO assigned");
            return;
        }

        var sound = soundsData.GetSoundByName(soundName);
        if (sound == null || sound.clips == null || sound.clips.Length == 0)
        {
            Debug.LogWarning($"SoundManager: Sound '{soundName}' not found or has no clips assigned");
            return;
        }

        var clipToPlay = sound.clips[UnityEngine.Random.Range(0, sound.clips.Length)];
        if (clipToPlay == null)
        {
            Debug.LogWarning($"SoundManager: Null AudioClip for sound '{soundName}'");
            return;
        }

        float finalVolume = sound.volume;

        if (sound.usePitchVariation && sound.minPitch > 0f && sound.maxPitch > 0f)
        {
            var pooledSource = audioSourcePool.Get();
            pooledSource.outputAudioMixerGroup = sound.audioMixerGroup;
            pooledSource.pitch = UnityEngine.Random.Range(sound.minPitch, sound.maxPitch);
            pooledSource.clip = clipToPlay;
            pooledSource.volume = finalVolume;
            pooledSource.Play();

            float clipDuration = clipToPlay.length / pooledSource.pitch;
            var coroutine = StartCoroutine(ReleaseAudioSourceWhenDone(pooledSource, clipDuration));
            activeAudioSources[pooledSource] = coroutine;
        }
        else
        {
            mainAudioSource.outputAudioMixerGroup = sound.audioMixerGroup;
            mainAudioSource.pitch = 1f;
            mainAudioSource.PlayOneShot(clipToPlay, finalVolume);
        }
    }

    /// <summary>
    /// Plays a sound by its zero-based index via SoundsSO.
    /// </summary>
    public void PlaySoundByIndex(int index)
    {
        var sound = soundsData?.GetSoundAt(index);
        if (sound == null)
        {
            Debug.LogWarning($"SoundManager: Invalid sound index {index}");
            return;
        }
        PlaySound(sound.name);
    }

    /// <summary>
    /// Plays a sound by its one-based display index.
    /// </summary>
    public void PlaySoundByDisplayIndex(int displayIndex)
    {
        PlaySoundByIndex(displayIndex - 1);
    }

    // --- Object Pool Callbacks ---

    private AudioSource CreatePooledAudioSource()
    {
        var go = new GameObject("Pooled AudioSource");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;

        if (mainAudioSource != null)
        {
            src.priority    = mainAudioSource.priority;
            src.spatialBlend = mainAudioSource.spatialBlend;
            src.rolloffMode = mainAudioSource.rolloffMode;
            src.minDistance = mainAudioSource.minDistance;
            src.maxDistance = mainAudioSource.maxDistance;
        }

        return src;
    }

    private void OnTakeFromPool(AudioSource source)    => source.gameObject.SetActive(true);
    private void OnReturnToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.outputAudioMixerGroup = null;
        source.gameObject.SetActive(false);
        activeAudioSources.Remove(source);
    }
    private void OnDestroyPoolObject(AudioSource source) => Destroy(source.gameObject);

    private IEnumerator ReleaseAudioSourceWhenDone(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (activeAudioSources.ContainsKey(source))
        {
            activeAudioSources.Remove(source);
            audioSourcePool.Release(source);
        }
    }
}
