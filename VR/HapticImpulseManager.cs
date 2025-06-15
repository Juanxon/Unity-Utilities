using System.Collections;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Manages haptic feedback for VR controllers with a comprehensive library of haptic patterns.
/// </summary>
public class HapticImpulseManager : MonoBehaviour
{
    [Title("Controller References")]
    [Tooltip("Reference to the left hand haptic controller.")]
    [SerializeField] private HapticImpulsePlayer leftHandHaptics;
    
    [Tooltip("Reference to the right hand haptic controller.")]
    [SerializeField] private HapticImpulsePlayer rightHandHaptics;

    [Title("Pattern Settings")]
    [Tooltip("Duration multiplier for all haptic patterns.")]
    [SerializeField] private float durationMultiplier = 1.0f;
    
    [Tooltip("Intensity multiplier for all haptic patterns.")]
    [SerializeField, Range(0.1f, 2.0f)] private float intensityMultiplier = 1.0f;

    /// <summary>
    /// Triggers haptic feedback on specified hand(s) with the selected pattern type
    /// </summary>
    public void TriggerHapticFeedback(Hand hand, HapticType type)
    {
        switch (hand)
        {
            case Hand.Left:
                ApplyHapticFeedback(leftHandHaptics, type);
                break;
            case Hand.Right:
                ApplyHapticFeedback(rightHandHaptics, type);
                break;
            case Hand.Both:
                ApplyHapticFeedback(leftHandHaptics, type);
                ApplyHapticFeedback(rightHandHaptics, type);
                break;
        }
    }

    /// <summary>
    /// Applies the selected haptic pattern to a specific haptic controller
    /// </summary>
    public void ApplyHapticFeedback(HapticImpulsePlayer haptics, HapticType type)
    {
        if (haptics == null) return;
        
        // Group patterns by category
        switch (type)
        {
            // Basic Intensity Patterns
            case HapticType.Subtle:
                haptics.SendHapticImpulse(0.15f * intensityMultiplier, 0.1f * durationMultiplier);
                break;
            case HapticType.Light:
                haptics.SendHapticImpulse(0.3f * intensityMultiplier, 0.1f * durationMultiplier);
                break;
            case HapticType.Medium:
                haptics.SendHapticImpulse(0.6f * intensityMultiplier, 0.15f * durationMultiplier);
                break;
            case HapticType.Strong:
                haptics.SendHapticImpulse(1.0f * intensityMultiplier, 0.2f * durationMultiplier);
                break;
            case HapticType.Intense:
                haptics.SendHapticImpulse(1.0f * intensityMultiplier, 0.4f * durationMultiplier);
                break;
                
            // Pulse Patterns
            case HapticType.Pulse:
                StartCoroutine(PulsePattern(haptics, 3, 0.15f));
                break;
            case HapticType.DoublePulse:
                StartCoroutine(DoublePulsePattern(haptics));
                break;
            case HapticType.TriplePulse:
                StartCoroutine(TriplePulsePattern(haptics));
                break;
            case HapticType.SlowPulse:
                StartCoroutine(PulsePattern(haptics, 3, 0.3f));
                break;
            case HapticType.MorseCode:
                StartCoroutine(MorsePattern(haptics));
                break;
                
            // Texture Patterns
            case HapticType.Rough:
                StartCoroutine(RoughTexturePattern(haptics));
                break;
            case HapticType.Smooth:
                StartCoroutine(SmoothTexturePattern(haptics));
                break;
            case HapticType.Buzzing:
                StartCoroutine(BuzzingPattern(haptics));
                break;
            case HapticType.Static:
                StartCoroutine(StaticPattern(haptics));
                break;
                
            // Impact Patterns
            case HapticType.Sharp:
                StartCoroutine(SharpImpactPattern(haptics));
                break;
            case HapticType.Heavy:
                StartCoroutine(HeavyImpactPattern(haptics));
                break;
            case HapticType.Bounce:
                StartCoroutine(BounceImpactPattern(haptics));
                break;
            case HapticType.Explosion:
                StartCoroutine(ExplosionPattern(haptics));
                break;
                
            // Ambient Patterns
            case HapticType.Heartbeat:
                StartCoroutine(HeartbeatPattern(haptics));
                break;
            case HapticType.Escalating:
                StartCoroutine(EscalatingPattern(haptics));
                break;
            case HapticType.LongRumble:
                StartCoroutine(LongRumblePattern(haptics));
                break;
            case HapticType.Earthquake:
                StartCoroutine(EarthquakePattern(haptics));
                break;
            case HapticType.Engine:
                StartCoroutine(EnginePattern(haptics));
                break;
        }
    }

    #region Basic Intensity Patterns
    // Implemented directly in ApplyHapticFeedback
    #endregion

    #region Pulse Patterns

    private IEnumerator PulsePattern(HapticImpulsePlayer haptics, int pulseCount, float interval)
    {
        for (int i = 0; i < pulseCount; i++)
        {
            haptics.SendHapticImpulse(0.8f * intensityMultiplier, 0.1f * durationMultiplier);
            yield return new WaitForSeconds(interval * durationMultiplier);
        }
    }

    private IEnumerator DoublePulsePattern(HapticImpulsePlayer haptics)
    {
        haptics.SendHapticImpulse(0.7f * intensityMultiplier, 0.08f * durationMultiplier);
        yield return new WaitForSeconds(0.1f * durationMultiplier);
        haptics.SendHapticImpulse(0.7f * intensityMultiplier, 0.08f * durationMultiplier);
    }
    
    private IEnumerator TriplePulsePattern(HapticImpulsePlayer haptics)
    {
        float pulseIntensity = 0.7f * intensityMultiplier;
        float pulseDuration = 0.06f * durationMultiplier;
        float pauseDuration = 0.08f * durationMultiplier;
        
        haptics.SendHapticImpulse(pulseIntensity, pulseDuration);
        yield return new WaitForSeconds(pauseDuration);
        haptics.SendHapticImpulse(pulseIntensity, pulseDuration);
        yield return new WaitForSeconds(pauseDuration);
        haptics.SendHapticImpulse(pulseIntensity, pulseDuration);
    }
    
    private IEnumerator MorsePattern(HapticImpulsePlayer haptics)
    {
        // SOS pattern: ... --- ...
        float dotDuration = 0.08f * durationMultiplier;
        float dashDuration = 0.2f * durationMultiplier;
        float symbolPause = 0.08f * durationMultiplier;
        float letterPause = 0.2f * durationMultiplier;
        
        // S (...)
        for (int i = 0; i < 3; i++)
        {
            haptics.SendHapticImpulse(0.8f * intensityMultiplier, dotDuration);
            yield return new WaitForSeconds(symbolPause);
        }
        yield return new WaitForSeconds(letterPause);
        
        // O (---)
        for (int i = 0; i < 3; i++)
        {
            haptics.SendHapticImpulse(0.8f * intensityMultiplier, dashDuration);
            yield return new WaitForSeconds(symbolPause);
        }
        yield return new WaitForSeconds(letterPause);
        
        // S (...)
        for (int i = 0; i < 3; i++)
        {
            haptics.SendHapticImpulse(0.8f * intensityMultiplier, dotDuration);
            yield return new WaitForSeconds(symbolPause);
        }
    }

    #endregion

    #region Texture Patterns
    
    private IEnumerator RoughTexturePattern(HapticImpulsePlayer haptics)
    {
        float totalDuration = 1.0f * durationMultiplier;
        float elapsedTime = 0f;
        
        while (elapsedTime < totalDuration)
        {
            // Random intensity for rough feel
            float intensity = Random.Range(0.3f, 0.7f) * intensityMultiplier;
            float pulseDuration = Random.Range(0.02f, 0.08f) * durationMultiplier;
            float pauseDuration = Random.Range(0.01f, 0.05f) * durationMultiplier;
            
            haptics.SendHapticImpulse(intensity, pulseDuration);
            yield return new WaitForSeconds(pauseDuration);
            elapsedTime += (pulseDuration + pauseDuration);
        }
    }
    
    private IEnumerator SmoothTexturePattern(HapticImpulsePlayer haptics)
    {
        float totalDuration = 1.0f * durationMultiplier;
        float frequency = 10f; // Cycles per second
        float elapsedTime = 0f;
        float timeIncrement = 0.05f;
        
        while (elapsedTime < totalDuration)
        {
            // Sine wave pattern for smooth transitions
            float phase = elapsedTime * frequency;
            float intensity = (Mathf.Sin(phase * Mathf.PI) * 0.4f + 0.4f) * intensityMultiplier;
            
            haptics.SendHapticImpulse(intensity, timeIncrement);
            yield return new WaitForSeconds(timeIncrement);
            elapsedTime += timeIncrement;
        }
    }
    
    private IEnumerator BuzzingPattern(HapticImpulsePlayer haptics)
    {
        float totalDuration = 0.8f * durationMultiplier;
        float pulseTime = 0.02f;
        float elapsedTime = 0f;
        
        while (elapsedTime < totalDuration)
        {
            // Rapid high-frequency pulses
            haptics.SendHapticImpulse(0.6f * intensityMultiplier, pulseTime);
            yield return new WaitForSeconds(pulseTime);
            elapsedTime += pulseTime;
        }
    }
    
    private IEnumerator StaticPattern(HapticImpulsePlayer haptics)
    {
        float totalDuration = 1.0f * durationMultiplier;
        float elapsedTime = 0f;
        float timeStep = 0.016f; // ~60Hz
        
        while (elapsedTime < totalDuration)
        {
            // Random binary intensity - either on or off
            float intensity = (Random.value > 0.5f ? 0.7f : 0.0f) * intensityMultiplier;
            haptics.SendHapticImpulse(intensity, timeStep);
            yield return new WaitForSeconds(timeStep);
            elapsedTime += timeStep;
        }
    }

    #endregion

    #region Impact Patterns
    
    private IEnumerator SharpImpactPattern(HapticImpulsePlayer haptics)
    {
        // Very brief but intense pulse
        haptics.SendHapticImpulse(1.0f * intensityMultiplier, 0.05f * durationMultiplier);
        yield return null;
    }
    
    private IEnumerator HeavyImpactPattern(HapticImpulsePlayer haptics)
    {
        // Strong initial hit
        haptics.SendHapticImpulse(1.0f * intensityMultiplier, 0.15f * durationMultiplier);
        yield return new WaitForSeconds(0.07f * durationMultiplier);
        
        // Followed by residual vibration
        haptics.SendHapticImpulse(0.6f * intensityMultiplier, 0.2f * durationMultiplier);
    }
    
    private IEnumerator BounceImpactPattern(HapticImpulsePlayer haptics)
    {
        // Initial impact
        haptics.SendHapticImpulse(1.0f * intensityMultiplier, 0.1f * durationMultiplier);
        yield return new WaitForSeconds(0.15f * durationMultiplier);
        
        // First bounce
        haptics.SendHapticImpulse(0.7f * intensityMultiplier, 0.08f * durationMultiplier);
        yield return new WaitForSeconds(0.2f * durationMultiplier);
        
        // Second bounce
        haptics.SendHapticImpulse(0.4f * intensityMultiplier, 0.06f * durationMultiplier);
        yield return new WaitForSeconds(0.25f * durationMultiplier);
        
        // Third bounce
        haptics.SendHapticImpulse(0.2f * intensityMultiplier, 0.04f * durationMultiplier);
    }
    
    private IEnumerator ExplosionPattern(HapticImpulsePlayer haptics)
    {
        // Sharp initial blast
        haptics.SendHapticImpulse(1.0f * intensityMultiplier, 0.2f * durationMultiplier);
        yield return new WaitForSeconds(0.05f * durationMultiplier);
        
        // Secondary shockwave
        haptics.SendHapticImpulse(0.9f * intensityMultiplier, 0.3f * durationMultiplier);
        
        // Debris and aftereffects
        float aftershockDuration = 0.7f * durationMultiplier;
        float elapsedTime = 0f;
        float timeStep = 0.05f;
        
        while (elapsedTime < aftershockDuration)
        {
            float intensity = Mathf.Lerp(0.7f, 0.1f, elapsedTime / aftershockDuration) * intensityMultiplier;
            float randomVariation = Random.Range(-0.2f, 0.2f);
            haptics.SendHapticImpulse(Mathf.Clamp01(intensity + randomVariation), timeStep);
            
            yield return new WaitForSeconds(timeStep);
            elapsedTime += timeStep;
        }
    }

    #endregion

    #region Ambient Patterns

    private IEnumerator EscalatingPattern(HapticImpulsePlayer haptics)
    {
        float[] intensities = { 0.3f, 0.5f, 0.7f, 1.0f };
        
        for (int i = 0; i < intensities.Length; i++)
        {
            haptics.SendHapticImpulse(intensities[i] * intensityMultiplier, 0.1f * durationMultiplier);
            yield return new WaitForSeconds(0.12f * durationMultiplier);
        }
    }

    private IEnumerator HeartbeatPattern(HapticImpulsePlayer haptics)
    {
        // Mimics a heartbeat with two pulses of different intensity
        haptics.SendHapticImpulse(0.7f * intensityMultiplier, 0.1f * durationMultiplier);
        yield return new WaitForSeconds(0.15f * durationMultiplier);
        haptics.SendHapticImpulse(0.9f * intensityMultiplier, 0.15f * durationMultiplier);
        yield return new WaitForSeconds(0.5f * durationMultiplier);
    }

    private IEnumerator LongRumblePattern(HapticImpulsePlayer haptics)
    {
        float totalDuration = 1.0f * durationMultiplier;
        float intervalTime = 0.05f;
        float elapsedTime = 0f;
        
        while (elapsedTime < totalDuration)
        {
            float randomIntensity = Random.Range(0.6f, 0.8f) * intensityMultiplier;
            haptics.SendHapticImpulse(randomIntensity, intervalTime);
            yield return new WaitForSeconds(intervalTime);
            elapsedTime += intervalTime;
        }
    }
    
    private IEnumerator EarthquakePattern(HapticImpulsePlayer haptics)
    {
        float totalDuration = 2.0f * durationMultiplier;
        float elapsedTime = 0f;
        float intervalTime = 0.05f;
        
        // Start with foreshock
        for (int i = 0; i < 3; i++)
        {
            haptics.SendHapticImpulse(0.3f * intensityMultiplier, 0.1f * durationMultiplier);
            yield return new WaitForSeconds(0.2f * durationMultiplier);
            elapsedTime += 0.3f * durationMultiplier;
        }
        
        // Main earthquake
        while (elapsedTime < totalDuration)
        {
            // Varying intensity with occasional strong jolts
            float baseIntensity = 0.6f;
            if (Random.value > 0.8f)
            {
                // Strong jolt
                baseIntensity = 1.0f;
            }
            
            haptics.SendHapticImpulse(baseIntensity * intensityMultiplier, intervalTime);
            yield return new WaitForSeconds(intervalTime);
            elapsedTime += intervalTime;
        }
        
        // Aftershocks
        for (int i = 0; i < 5; i++)
        {
            float intensity = Mathf.Lerp(0.7f, 0.2f, i/5f) * intensityMultiplier;
            haptics.SendHapticImpulse(intensity, 0.15f * durationMultiplier);
            yield return new WaitForSeconds(0.3f * durationMultiplier);
        }
    }
    
    private IEnumerator EnginePattern(HapticImpulsePlayer haptics)
    {
        float totalDuration = 3.0f * durationMultiplier;
        float elapsedTime = 0f;
        float cycleTime = 0.05f;
        
        // Start engine - increasing rumble
        for (int i = 0; i < 10; i++)
        {
            float startIntensity = i / 10f * 0.7f * intensityMultiplier;
            haptics.SendHapticImpulse(startIntensity, cycleTime);
            yield return new WaitForSeconds(cycleTime);
            elapsedTime += cycleTime;
        }
        
        // Idle engine - consistent rumble with slight variations
        while (elapsedTime < totalDuration)
        {
            // Base idle with fluctuations
            float idleIntensity = 0.5f + 0.1f * Mathf.Sin(elapsedTime * 8f);
            // Occasional misfires for realism
            if (Random.value > 0.95f)
            {
                idleIntensity += 0.3f;
            }
            
            haptics.SendHapticImpulse(idleIntensity * intensityMultiplier, cycleTime);
            yield return new WaitForSeconds(cycleTime);
            elapsedTime += cycleTime;
        }
        
        // Engine shutdown - decreasing rumble
        for (int i = 10; i >= 0; i--)
        {
            float endIntensity = i / 10f * 0.7f * intensityMultiplier;
            haptics.SendHapticImpulse(endIntensity, cycleTime);
            yield return new WaitForSeconds(cycleTime);
        }
    }

    #endregion

    #region Test Buttons

    [Title("Test - Basic Intensity")]
    [Button] public void TestSubtle() => TriggerHapticFeedback(Hand.Both, HapticType.Subtle);
    [Button] public void TestLight() => TriggerHapticFeedback(Hand.Both, HapticType.Light);
    [Button] public void TestMedium() => TriggerHapticFeedback(Hand.Both, HapticType.Medium);
    [Button] public void TestStrong() => TriggerHapticFeedback(Hand.Both, HapticType.Strong);
    [Button] public void TestIntense() => TriggerHapticFeedback(Hand.Both, HapticType.Intense);
    
    [Title("Test - Pulse Patterns")]
    [Button] public void TestPulse() => TriggerHapticFeedback(Hand.Both, HapticType.Pulse);
    [Button] public void TestDoublePulse() => TriggerHapticFeedback(Hand.Both, HapticType.DoublePulse);
    [Button] public void TestTriplePulse() => TriggerHapticFeedback(Hand.Both, HapticType.TriplePulse);
    [Button] public void TestSlowPulse() => TriggerHapticFeedback(Hand.Both, HapticType.SlowPulse);
    [Button] public void TestMorseCode() => TriggerHapticFeedback(Hand.Both, HapticType.MorseCode);
    
    [Title("Test - Texture Patterns")]
    [Button] public void TestRough() => TriggerHapticFeedback(Hand.Both, HapticType.Rough);
    [Button] public void TestSmooth() => TriggerHapticFeedback(Hand.Both, HapticType.Smooth);
    [Button] public void TestBuzzing() => TriggerHapticFeedback(Hand.Both, HapticType.Buzzing);
    [Button] public void TestStatic() => TriggerHapticFeedback(Hand.Both, HapticType.Static);
    
    [Title("Test - Impact Patterns")]
    [Button] public void TestSharp() => TriggerHapticFeedback(Hand.Both, HapticType.Sharp);
    [Button] public void TestHeavy() => TriggerHapticFeedback(Hand.Both, HapticType.Heavy);
    [Button] public void TestBounce() => TriggerHapticFeedback(Hand.Both, HapticType.Bounce);
    [Button] public void TestExplosion() => TriggerHapticFeedback(Hand.Both, HapticType.Explosion);
    
    [Title("Test - Ambient Patterns")]
    [Button] public void TestHeartbeat() => TriggerHapticFeedback(Hand.Both, HapticType.Heartbeat);
    [Button] public void TestEscalating() => TriggerHapticFeedback(Hand.Both, HapticType.Escalating);
    [Button] public void TestLongRumble() => TriggerHapticFeedback(Hand.Both, HapticType.LongRumble);
    [Button] public void TestEarthquake() => TriggerHapticFeedback(Hand.Both, HapticType.Earthquake);
    [Button] public void TestEngine() => TriggerHapticFeedback(Hand.Both, HapticType.Engine);

    #endregion
}

/// <summary>
/// Types of haptic feedback patterns organized by category
/// </summary>
public enum HapticType
{
    // Basic Intensity Patterns
    Subtle,      // Very light feedback
    Light,       // Subtle feedback
    Medium,      // Moderate feedback
    Strong,      // Intense feedback
    Intense,     // Maximum intensity feedback
    
    // Pulse Patterns
    Pulse,       // Rhythmic pulses
    DoublePulse, // Two quick pulses
    TriplePulse, // Three quick pulses
    SlowPulse,   // Slower rhythmic pulses
    MorseCode,   // SOS pattern
    
    // Texture Patterns
    Rough,       // Irregular bumpy texture
    Smooth,      // Gentle undulating texture
    Buzzing,     // High-frequency vibration
    Static,      // Random noise pattern
    
    // Impact Patterns
    Sharp,       // Quick, precise impact
    Heavy,       // Weighty impact with follow-through
    Bounce,      // Impact with decreasing bounces
    Explosion,   // Powerful blast with aftershocks
    
    // Ambient Patterns
    Heartbeat,   // Mimics heartbeat rhythm
    Escalating,  // Increasing intensity
    LongRumble,  // Extended variable vibration
    Earthquake,  // Intense, chaotic shaking
    Engine       // Motor running simulation
}

/// <summary>
/// Target hand for haptic feedback
/// </summary>
public enum Hand
{
    Left,
    Right,
    Both
}
