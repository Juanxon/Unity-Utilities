using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class EyeBlendShape
{
    [Tooltip("Name of the blendshape that controls this eye's closing/opening")]
    public string blendShapeName = "Eye_Close";
    
    [Tooltip("Check if this is the left eye (for identification purposes)")]
    public bool isLeftEye = false;
    
    [Tooltip("Maximum weight to apply to the blendshape when fully closed (0-100)")]
    [Range(0, 100)]
    public float maxCloseAmount = 100f;
}

/// <summary>
/// Controls realistic eye blinking for characters using blendshapes
/// </summary>
public class CharacterBlinking : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SkinnedMeshRenderer that contains the eye blendshapes")]
    public SkinnedMeshRenderer skinnedMeshRenderer;
    
    [Header("Blink Configuration")]
    [Tooltip("Whether the character should blink automatically")]
    public bool enableBlinking = true;
    
    [Tooltip("If true, both eyes will blink at exactly the same time. If false, eyes can blink slightly offset from each other")]
    public bool synchronizedBlinking = true;
    
    [Tooltip("How the model's blendshapes are set up: TRUE if the model uses a single blendshape that controls both eyes at once, FALSE if each eye has its own separate blendshape")]
    public bool useSingleBlendShapeForBothEyes = false;
    
    [Tooltip("Optional prefix added to blendshape names when searching. Some models use prefixes like 'Face.' or 'Viseme.'")]
    public string blendShapePrefix = "";
    
    [Header("Eyes Setup")]
    [Tooltip("List of blendshapes that control the eyes. If using a single blendshape for both eyes, only the first entry is used")]
    public List<EyeBlendShape> eyeBlendShapes = new List<EyeBlendShape>();
    
    [Header("Timing Settings")]
    [Tooltip("Minimum time in seconds between blinks")]
    public float blinkIntervalMin = 2f;
    
    [Tooltip("Maximum time in seconds between blinks")]
    public float blinkIntervalMax = 5f;
    
    [Tooltip("How long in seconds it takes for the eye to close and then open again")]
    public float blinkDuration = 0.15f;
    
    [Tooltip("How long the eyes stay fully closed during a blink")]
    public float closeToOpenInterval = 0.05f;
    
    [Tooltip("When eyes aren't synchronized, this determines the maximum random delay between left and right eye blinking (seconds)")]
    [Range(0f, 0.2f)]
    public float asyncBlinkOffset = 0.05f;
    
    [Header("Special Blink Modes")]
    [Tooltip("If enabled, the character may occasionally blink twice in quick succession")]
    public bool enableDoubleBlinking = true;
    
    [Tooltip("Probability (0-1) of a double-blink occurring")]
    [Range(0f, 1f)]
    public float doubleBlinkChance = 0.1f;
    
    [Tooltip("Makes eyes appear partially closed (like when tired or drowsy). 0 = fully open, 1 = almost closed")]
    [Range(0f, 1f)]
    public float sleepyEyesAmount = 0f;
    
    [Header("Animation Settings")]
    [Tooltip("Enable to ensure blinking and eye state overrides any animation values. This uses LateUpdate to give priority to our blendshape values over animations.")]
    public bool overrideAnimations = true;
    
    [Header("Events")]
    [Tooltip("Event triggered when a blink starts")]
    public UnityEvent onBlink;
    
    // Private variables
    private float blinkTimer = 0f;
    private float nextBlinkTime = 2f;
    private Dictionary<EyeBlendShape, int> blendShapeIndices = new Dictionary<EyeBlendShape, int>();
    private bool isBlinking = false;
    private Dictionary<int, float> currentBlendshapeValues = new Dictionary<int, float>();
    private Animator characterAnimator;
    
    private void Start()
    {
        nextBlinkTime = Random.Range(blinkIntervalMin, blinkIntervalMax);
        characterAnimator = GetComponentInParent<Animator>();
        
        // Cache blendshape indices for performance
        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
        {
            foreach (var eye in eyeBlendShapes)
            {
                string fullName = blendShapePrefix + eye.blendShapeName;
                int index = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(fullName);
                
                if (index < 0)
                {
                    Debug.LogWarning($"BlendShape '{fullName}' not found on mesh.");
                }
                else
                {
                    blendShapeIndices[eye] = index;
                    currentBlendshapeValues[index] = 0f;
                    
                    // Set initial sleepy eyes state
                    if (sleepyEyesAmount > 0)
                    {
                        float value = sleepyEyesAmount * eye.maxCloseAmount;
                        skinnedMeshRenderer.SetBlendShapeWeight(index, value);
                        currentBlendshapeValues[index] = value;
                    }
                }
            }
        }
        
        // Log information about animation overriding
        if (overrideAnimations)
        {
            Debug.Log("Blinking will override animation values using LateUpdate");
        }
    }
    
    private void Update()
    {
        if (enableBlinking && !isBlinking)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= nextBlinkTime)
            {
                if (synchronizedBlinking || useSingleBlendShapeForBothEyes)
                {
                    StartCoroutine(BlinkAllEyes());
                }
                else
                {
                    StartCoroutine(BlinkEyesIndependently());
                }
                
                blinkTimer = 0f;
                nextBlinkTime = Random.Range(blinkIntervalMin, blinkIntervalMax);
            }
        }
        
        // Apply sleepy eyes in Update only if not overriding animations
        // This ensures values are set at least once, but won't override animations
        if (!overrideAnimations && !isBlinking && sleepyEyesAmount > 0)
        {
            ApplySleepyEyes();
        }
    }
    
    private void LateUpdate()
    {
        // Only apply in LateUpdate if we want to override animations
        if (!overrideAnimations) return;
        
        // Apply our blendshape values after animation has been applied
        if (!isBlinking && sleepyEyesAmount > 0)
        {
            ApplySleepyEyes();
        }
        else if (isBlinking)
        {
            // During blinks, our coroutines handle the values
            // but we still need to ensure they're applied after animation
            foreach (var pair in currentBlendshapeValues)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(pair.Key, pair.Value);
            }
        }
    }
    
    private void ApplySleepyEyes()
    {
        foreach (var pair in blendShapeIndices)
        {
            if (pair.Value >= 0)
            {
                float value = sleepyEyesAmount * pair.Key.maxCloseAmount;
                skinnedMeshRenderer.SetBlendShapeWeight(pair.Value, value);
                currentBlendshapeValues[pair.Value] = value;
            }
        }
    }
    
    /// <summary>
    /// Blinks all eyes simultaneously in a synchronized manner
    /// </summary>
    private IEnumerator BlinkAllEyes()
    {
        isBlinking = true;
        onBlink?.Invoke();
        
        // Close eyes
        float time = 0f;
        while (time < blinkDuration)
        {
            time += Time.deltaTime;
            float t = time / blinkDuration;
            
            foreach (var pair in blendShapeIndices)
            {
                if (pair.Value >= 0)
                {
                    float targetWeight = Mathf.Lerp(sleepyEyesAmount * pair.Key.maxCloseAmount, pair.Key.maxCloseAmount, t);
                    skinnedMeshRenderer.SetBlendShapeWeight(pair.Value, targetWeight);
                    currentBlendshapeValues[pair.Value] = targetWeight;
                }
            }
            
            yield return null;
        }
        
        // Keep closed
        yield return new WaitForSeconds(closeToOpenInterval);
        
        // Open eyes
        time = 0f;
        while (time < blinkDuration)
        {
            time += Time.deltaTime;
            float t = time / blinkDuration;
            
            foreach (var pair in blendShapeIndices)
            {
                if (pair.Value >= 0)
                {
                    float targetWeight = Mathf.Lerp(pair.Key.maxCloseAmount, sleepyEyesAmount * pair.Key.maxCloseAmount, t);
                    skinnedMeshRenderer.SetBlendShapeWeight(pair.Value, targetWeight);
                    currentBlendshapeValues[pair.Value] = targetWeight;
                }
            }
            
            yield return null;
        }
        
        // Check for double blink
        if (enableDoubleBlinking && Random.value < doubleBlinkChance)
        {
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
            StartCoroutine(BlinkAllEyes());
            yield break;
        }
        
        isBlinking = false;
    }
    
    /// <summary>
    /// Blinks each eye independently with slight timing variations
    /// </summary>
    private IEnumerator BlinkEyesIndependently()
    {
        isBlinking = true;
        onBlink?.Invoke();
        
        List<Coroutine> blinkCoroutines = new List<Coroutine>();
        
        foreach (var eye in eyeBlendShapes)
        {
            if (blendShapeIndices.TryGetValue(eye, out int index) && index >= 0)
            {
                float delay = Random.Range(0f, asyncBlinkOffset);
                Coroutine routine = StartCoroutine(BlinkSingleEye(eye, delay));
                blinkCoroutines.Add(routine);
            }
        }
        
        // Wait for all blinks to complete
        foreach (var routine in blinkCoroutines)
        {
            yield return routine;
        }
        
        isBlinking = false;
    }
    
    /// <summary>
    /// Handles blinking of a single eye with delay
    /// </summary>
    private IEnumerator BlinkSingleEye(EyeBlendShape eye, float delay)
    {
        if (!blendShapeIndices.TryGetValue(eye, out int index) || index < 0)
        {
            yield break;
        }
        
        yield return new WaitForSeconds(delay);
        
        // Close eye
        float time = 0f;
        while (time < blinkDuration)
        {
            time += Time.deltaTime;
            float t = time / blinkDuration;
            float targetWeight = Mathf.Lerp(sleepyEyesAmount * eye.maxCloseAmount, eye.maxCloseAmount, t);
            skinnedMeshRenderer.SetBlendShapeWeight(index, targetWeight);
            currentBlendshapeValues[index] = targetWeight;
            yield return null;
        }
        
        // Keep closed
        yield return new WaitForSeconds(closeToOpenInterval);
        
        // Open eye
        time = 0f;
        while (time < blinkDuration)
        {
            time += Time.deltaTime;
            float t = time / blinkDuration;
            float targetWeight = Mathf.Lerp(eye.maxCloseAmount, sleepyEyesAmount * eye.maxCloseAmount, t);
            skinnedMeshRenderer.SetBlendShapeWeight(index, targetWeight);
            currentBlendshapeValues[index] = targetWeight;
            yield return null;
        }
    }
    
    /// <summary>
    /// Triggers a blink manually, useful for cinematic moments or reactions
    /// </summary>
    public void TriggerBlink()
    {
        if (!isBlinking)
        {
            if (synchronizedBlinking || useSingleBlendShapeForBothEyes)
            {
                StartCoroutine(BlinkAllEyes());
            }
            else
            {
                StartCoroutine(BlinkEyesIndependently());
            }
        }
    }
    
    /// <summary>
    /// Sets how sleepy/tired the character's eyes appear
    /// </summary>
    /// <param name="amount">Value from 0 to 1 where 0 is fully open eyes and 1 is nearly closed</param>
    public void SetSleepyEyes(float amount)
    {
        sleepyEyesAmount = Mathf.Clamp01(amount);
        
        // Apply immediately
        if (!isBlinking)
        {
            ApplySleepyEyes();
        }
    }
    
    /// <summary>
    /// Forces the current eye state to be applied, overriding any animation
    /// </summary>
    public void ForceApplyEyeState()
    {
        if (isBlinking)
        {
            // During blinks, use the currently tracked values
            foreach (var pair in currentBlendshapeValues)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(pair.Key, pair.Value);
            }
        }
        else
        {
            // Otherwise apply sleepy eyes state
            ApplySleepyEyes();
        }
    }
}
