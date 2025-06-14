using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class DialogueData
{
    [Tooltip("Unique identifier for this dialogue set")]
    public string dialogueID;
    
    [Tooltip("Probability of this dialogue occurring (0-100%)")]
    [Range(0, 100)]
    public float probability = 100f;
    
    [Tooltip("List of audio clips to choose from randomly")]
    public List<AudioClip> audioClips = new List<AudioClip>();
    
    [Tooltip("Priority level of this dialogue (higher numbers have higher priority)")]
    [Range(0, 10)]
    public int priority = 5;
    
    [Tooltip("Cooldown time in seconds before this dialogue can be played again")]
    public float cooldownTime = 3f;
    
    [HideInInspector]
    public float lastPlayedTime = -999f; // Timestamp when this dialogue was last played
    
    [HideInInspector]
    public int lastPlayedClipIndex = -1; // Index of the last clip played
    
    [HideInInspector]
    public List<int> playedClipIndices = new List<int>(); // History of played clip indices
}

public enum AudioRepetitionMode
{
    AllowRepetition,         // Allow any clip to play (original behavior)
    AvoidLastPlayed,         // Avoid playing the last clip that was played
    PlayAllBeforeRepeat,     // Play all clips before repeating any
    PercentageBeforeRepeat   // Play a percentage of all clips before allowing repeats
}

public class CharacterDialogeManager : MonoBehaviour
{
    [Tooltip("Audio source where dialogues will be played")]
    [SerializeField] private AudioSource dialogueAudioSource;
    
    [Tooltip("List of dialogue sets for this character")]
    [SerializeField] private List<DialogueData> dialogueSets = new List<DialogueData>();
    
    [Header("Dialogue Behavior")]
    [Tooltip("If true, will interrupt current dialogue to play new ones")]
    [SerializeField] private bool interruptCurrentDialogue = true;
    
    [Tooltip("If true, will only interrupt if new dialogue has higher priority")]
    [SerializeField] private bool respectPriority = true;
    
    [Tooltip("Global cooldown between any dialogues (in seconds)")]
    [SerializeField] private float globalCooldown = 1.0f;
    
    [Header("Audio Clip Selection")]
    [Tooltip("Controls how audio clips are selected to avoid repetition")]
    [SerializeField] private AudioRepetitionMode repetitionMode = AudioRepetitionMode.AvoidLastPlayed;
    
    [Tooltip("Percentage of clips to play before allowing repeats (0-100%)")]
    [Range(0, 100)]
    [SerializeField] private float percentageBeforeRepeat = 75f;
    
    [Header("Events")]
    [Tooltip("Event triggered when dialogue starts playing")]
    public UnityEvent<string> OnDialogueStarted;
    
    [Tooltip("Event triggered when dialogue finishes playing")]
    public UnityEvent<string> OnDialogueFinished;
    
    private float lastPlayedTime = -999f;
    private string currentDialogueID = "";
    private int currentPriority = -1;
    private Coroutine dialogueFinishCoroutine;
    
    private void Awake()
    {
        // If no audio source is assigned, try to get one from this GameObject
        if (dialogueAudioSource == null)
        {
            dialogueAudioSource = GetComponent<AudioSource>();
            
            // If still null, add one
            if (dialogueAudioSource == null)
            {
                dialogueAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log($"[{gameObject.name}] No AudioSource assigned. Created a new one.");
            }
        }
    }
    
    /// <summary>
    /// Plays a random dialogue clip from the specified dialogue set based on probability
    /// </summary>
    /// <param name="dialogueID">Identifier of the dialogue set to play</param>
    /// <param name="forcePlay">If true, ignores cooldowns and always attempts to play</param>
    /// <returns>True if a dialogue was played, false otherwise</returns>
    public bool PlayDialogue(string dialogueID, bool forcePlay = false)
    {
        // Find the dialogue set with the matching ID
        DialogueData dialogueSet = dialogueSets.Find(d => d.dialogueID == dialogueID);
        
        if (dialogueSet == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Dialogue set with ID '{dialogueID}' not found!");
            return false;
        }
        
        // Check cooldowns if not forcing play
        if (!forcePlay)
        {
            // Check global cooldown
            if (Time.time - lastPlayedTime < globalCooldown)
            {
                Debug.Log($"[{gameObject.name}] Global cooldown active. Waiting {globalCooldown - (Time.time - lastPlayedTime):F1} more seconds.");
                return false;
            }
            
            // Check dialogue-specific cooldown
            if (Time.time - dialogueSet.lastPlayedTime < dialogueSet.cooldownTime)
            {
                Debug.Log($"[{gameObject.name}] Dialogue '{dialogueID}' on cooldown. Waiting {dialogueSet.cooldownTime - (Time.time - dialogueSet.lastPlayedTime):F1} more seconds.");
                return false;
            }
        }
        
        // Check if we should interrupt current dialogue
        if (IsDialoguePlaying())
        {
            if (!interruptCurrentDialogue)
            {
                Debug.Log($"[{gameObject.name}] Dialogue already playing and interruption is disabled.");
                return false;
            }
            
            // Check priority if needed
            if (respectPriority && dialogueSet.priority <= currentPriority)
            {
                Debug.Log($"[{gameObject.name}] New dialogue '{dialogueID}' (priority {dialogueSet.priority}) has lower or equal priority than current '{currentDialogueID}' (priority {currentPriority}).");
                return false;
            }
            
            // Stop the current dialogue and any tracking coroutine
            StopDialogue();
        }
        
        // Check if dialogue should play based on probability
        if (!forcePlay && UnityEngine.Random.Range(0f, 100f) > dialogueSet.probability)
        {
            Debug.Log($"[{gameObject.name}] Dialogue '{dialogueID}' probability check failed. Rolled higher than {dialogueSet.probability}%");
            return false;
        }
        
        // Ensure there are clips to play
        if (dialogueSet.audioClips == null || dialogueSet.audioClips.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] Dialogue set '{dialogueID}' has no audio clips!");
            return false;
        }
        
        // Select a clip based on repetition mode
        AudioClip selectedClip = SelectAudioClip(dialogueSet);
        
        if (selectedClip == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Selected null audio clip from dialogue set '{dialogueID}'!");
            return false;
        }
        
        // Play the selected clip
        dialogueAudioSource.clip = selectedClip;
        dialogueAudioSource.Play();
        
        // Update tracking variables
        currentDialogueID = dialogueID;
        currentPriority = dialogueSet.priority;
        lastPlayedTime = Time.time;
        dialogueSet.lastPlayedTime = Time.time;
        
        // Start coroutine to track when dialogue finishes
        if (dialogueFinishCoroutine != null)
        {
            StopCoroutine(dialogueFinishCoroutine);
        }
        dialogueFinishCoroutine = StartCoroutine(TrackDialogueFinish(dialogueID, selectedClip.length));
        
        // Trigger start event
        OnDialogueStarted?.Invoke(dialogueID);
        
        return true;
    }
    
    /// <summary>
    /// Selects an audio clip based on the current repetition mode
    /// </summary>
    private AudioClip SelectAudioClip(DialogueData dialogueSet)
    {
        int clipIndex = -1;
        int totalClips = dialogueSet.audioClips.Count;
        
        // If only one clip, just use it
        if (totalClips == 1)
        {
            clipIndex = 0;
            dialogueSet.lastPlayedClipIndex = clipIndex;
            dialogueSet.playedClipIndices.Add(clipIndex);
            return dialogueSet.audioClips[clipIndex];
        }
        
        switch (repetitionMode)
        {
            case AudioRepetitionMode.AllowRepetition:
                // Original behavior - completely random selection
                clipIndex = UnityEngine.Random.Range(0, totalClips);
                break;
                
            case AudioRepetitionMode.AvoidLastPlayed:
                // Avoid playing the last clip that was played
                if (dialogueSet.lastPlayedClipIndex >= 0)
                {
                    // Get all indices except the last played one
                    List<int> availableIndices = Enumerable.Range(0, totalClips)
                        .Where(i => i != dialogueSet.lastPlayedClipIndex)
                        .ToList();
                        
                    clipIndex = availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
                }
                else
                {
                    // No last played clip, select randomly
                    clipIndex = UnityEngine.Random.Range(0, totalClips);
                }
                break;
                
            case AudioRepetitionMode.PlayAllBeforeRepeat:
                // Play all clips before repeating any
                if (dialogueSet.playedClipIndices.Count >= totalClips)
                {
                    // All clips have been played, reset history
                    dialogueSet.playedClipIndices.Clear();
                }
                
                // Get indices of clips that haven't been played yet
                List<int> unplayedIndices = Enumerable.Range(0, totalClips)
                    .Where(i => !dialogueSet.playedClipIndices.Contains(i))
                    .ToList();
                    
                // Select a random unplayed clip
                clipIndex = unplayedIndices[UnityEngine.Random.Range(0, unplayedIndices.Count)];
                break;
                
            case AudioRepetitionMode.PercentageBeforeRepeat:
                // Calculate how many clips need to be played before allowing repeats
                int clipsToPlayBeforeRepeat = Mathf.CeilToInt(totalClips * (percentageBeforeRepeat / 100f));
                
                // If we've played enough clips, reset history
                if (dialogueSet.playedClipIndices.Count >= clipsToPlayBeforeRepeat)
                {
                    dialogueSet.playedClipIndices.Clear();
                }
                
                // Get indices of clips that haven't been played yet
                List<int> remainingIndices = Enumerable.Range(0, totalClips)
                    .Where(i => !dialogueSet.playedClipIndices.Contains(i))
                    .ToList();
                    
                // If all available clips have been played (but not enough for reset),
                // just pick a random one that's not the last played
                if (remainingIndices.Count == 0)
                {
                    remainingIndices = Enumerable.Range(0, totalClips)
                        .Where(i => i != dialogueSet.lastPlayedClipIndex)
                        .ToList();
                }
                
                clipIndex = remainingIndices[UnityEngine.Random.Range(0, remainingIndices.Count)];
                break;
        }
        
        // Update tracking
        dialogueSet.lastPlayedClipIndex = clipIndex;
        
        // Add to history if not already there
        if (!dialogueSet.playedClipIndices.Contains(clipIndex))
        {
            dialogueSet.playedClipIndices.Add(clipIndex);
        }
        
        return dialogueSet.audioClips[clipIndex];
    }
    
    /// <summary>
    /// Unity Event-friendly version that plays a dialogue without returning a value.
    /// Can be used directly in the Unity Inspector's Events.
    /// </summary>
    /// <param name="dialogueID">Identifier of the dialogue set to play</param>
    public void PlayDialogueEvent(string dialogueID)
    {
        PlayDialogue(dialogueID, false);
        // No return value, making it compatible with Unity Events
    }

    /// <summary>
    /// Unity Event-friendly version that forces a dialogue to play without returning a value.
    /// Can be used directly in the Unity Inspector's Events.
    /// </summary>
    /// <param name="dialogueID">Identifier of the dialogue set to play</param>
    public void ForcePlayDialogueEvent(string dialogueID)
    {
        PlayDialogue(dialogueID, true);
        // No return value, making it compatible with Unity Events
    }

    /// <summary>
    /// Plays a dialogue at random from the available dialogue sets.
    /// Useful for generic responses that don't need specific dialogue IDs.
    /// </summary>
    public void PlayRandomDialogue()
    {
        if (dialogueSets.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No dialogue sets available to play random dialogue!");
            return;
        }

        // Get a random dialogue set
        int randomIndex = UnityEngine.Random.Range(0, dialogueSets.Count);
        PlayDialogueEvent(dialogueSets[randomIndex].dialogueID);
    }
    
    /// <summary>
    /// Coroutine to track when a dialogue finishes playing
    /// </summary>
    private IEnumerator TrackDialogueFinish(string dialogueID, float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        
        // Trigger finish event if this is still the current dialogue
        if (currentDialogueID == dialogueID)
        {
            OnDialogueFinished?.Invoke(dialogueID);
            currentDialogueID = "";
            currentPriority = -1;
            dialogueFinishCoroutine = null;
        }
    }
    
    /// <summary>
    /// Stops the currently playing dialogue
    /// </summary>
    public void StopDialogue()
    {
        if (dialogueAudioSource.isPlaying)
        {
            dialogueAudioSource.Stop();
            
            // Trigger finish event for the stopped dialogue
            if (!string.IsNullOrEmpty(currentDialogueID))
            {
                OnDialogueFinished?.Invoke(currentDialogueID);
            }
            
            // Reset tracking variables
            currentDialogueID = "";
            currentPriority = -1;
            
            // Stop the tracking coroutine
            if (dialogueFinishCoroutine != null)
            {
                StopCoroutine(dialogueFinishCoroutine);
                dialogueFinishCoroutine = null;
            }
        }
    }
    
    /// <summary>
    /// Checks if a dialogue is currently playing
    /// </summary>
    public bool IsDialoguePlaying()
    {
        return dialogueAudioSource.isPlaying;
    }
    
    /// <summary>
    /// Gets the ID of the currently playing dialogue
    /// </summary>
    public string GetCurrentDialogueID()
    {
        return currentDialogueID;
    }
    
    /// <summary>
    /// Force play a specific dialogue regardless of conditions
    /// </summary>
    public bool ForcePlayDialogue(string dialogueID)
    {
        return PlayDialogue(dialogueID, true);
    }
    
    /// <summary>
    /// Set the volume for the dialogue audio source
    /// </summary>
    public void SetVolume(float volume)
    {
        dialogueAudioSource.volume = Mathf.Clamp01(volume);
    }
    
    /// <summary>
    /// Reset all cooldowns to allow dialogues to play again immediately
    /// </summary>
    public void ResetAllCooldowns()
    {
        lastPlayedTime = -999f;
        foreach (var dialogue in dialogueSets)
        {
            dialogue.lastPlayedTime = -999f;
        }
    }
    
    /// <summary>
    /// Reset audio clip history for all dialogue sets
    /// </summary>
    public void ResetPlaybackHistory()
    {
        foreach (var dialogue in dialogueSets)
        {
            dialogue.playedClipIndices.Clear();
            dialogue.lastPlayedClipIndex = -1;
        }
    }
    
    /// <summary>
    /// Set the audio repetition mode
    /// </summary>
    public void SetRepetitionMode(AudioRepetitionMode mode)
    {
        repetitionMode = mode;
    }
    
    /// <summary>
    /// Set the percentage threshold for clip repetition
    /// </summary>
    public void SetPercentageBeforeRepeat(float percentage)
    {
        percentageBeforeRepeat = Mathf.Clamp(percentage, 0f, 100f);
    }
}
