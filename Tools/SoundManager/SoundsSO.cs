using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sound Manager/Sounds", fileName = "Sounds SO")]
public class SoundsSO : ScriptableObject
{
    [Tooltip("List of sound configurations")]
    public List<SoundData> sounds = new List<SoundData>();

    /// <summary>
    /// Creates and adds a new SoundData at the end of the list.
    /// </summary>
    public void AddSound(string defaultName = "New Sound")
    {
        var sd = new SoundData
        {
            name = defaultName,
            volume = 1f,
            audioMixerGroup = null,
            clips = Array.Empty<AudioClip>(),
            usePitchVariation = false,
            minPitch = 1f,
            maxPitch = 1f
        };
        sounds.Add(sd);
    }

    /// <summary>
    /// Inserts an existing SoundData at the specified index.
    /// Clamps the index between 0 and sounds.Count.
    /// </summary>
    public void InsertSoundAt(int index, SoundData data)
    {
        int clamped = Mathf.Clamp(index, 0, sounds.Count);
        sounds.Insert(clamped, data);
    }

    /// <summary>
    /// Removes the SoundData at the given index.
    /// Returns true if removal succeeded.
    /// </summary>
    public bool RemoveSoundAt(int index)
    {
        if (index >= 0 && index < sounds.Count)
        {
            sounds.RemoveAt(index);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Finds and removes the first SoundData with a matching name (case-insensitive).
    /// Returns true if found and removed.
    /// </summary>
    public bool RemoveSoundByName(string soundName)
    {
        int idx = GetSoundIndex(soundName);
        if (idx >= 0)
        {
            sounds.RemoveAt(idx);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all SoundData entries.
    /// </summary>
    public void ClearAll()
    {
        sounds.Clear();
    }

    /// <summary>
    /// Returns the SoundData at the given index, or null if out of range.
    /// </summary>
    public SoundData GetSoundAt(int index)
    {
        if (index >= 0 && index < sounds.Count)
            return sounds[index];
        return null;
    }

    /// <summary>
    /// Returns the first SoundData whose name matches (case-insensitive), or null if none found.
    /// </summary>
    public SoundData GetSoundByName(string soundName)
    {
        return sounds.Find(s =>
            string.Equals(s.name, soundName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns the index of the first SoundData with the given name (case-insensitive), or –1 if not found.
    /// </summary>
    public int GetSoundIndex(string soundName)
    {
        return sounds.FindIndex(s =>
            string.Equals(s.name, soundName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Moves an entry from oldIndex to newIndex.
    /// Returns true if move succeeded.
    /// </summary>
    public bool MoveSound(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= sounds.Count ||
            newIndex < 0 || newIndex >= sounds.Count ||
            oldIndex == newIndex)
            return false;

        var item = sounds[oldIndex];
        sounds.RemoveAt(oldIndex);

        // Adjust newIndex if removal occurred before it
        if (newIndex > oldIndex) newIndex--;
        sounds.Insert(newIndex, item);
        return true;
    }
}

[Serializable]
public class SoundData
{
    [Tooltip("Unique name to identify this sound")]
    public string name;

    [Range(0f, 1f), Tooltip("Base volume of the clip")]
    public float volume = 1f;

    [Tooltip("AudioMixerGroup to route this sound through")]
    public UnityEngine.Audio.AudioMixerGroup audioMixerGroup;

    [Tooltip("Array of AudioClips for this sound")]
    public AudioClip[] clips = Array.Empty<AudioClip>();

    [Tooltip("Whether to apply random pitch variation")]
    public bool usePitchVariation;

    [Tooltip("Minimum pitch (when variation enabled)")]
    public float minPitch = 1f;

    [Tooltip("Maximum pitch (when variation enabled)")]
    public float maxPitch = 1f;
}
