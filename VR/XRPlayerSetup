using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple script to position an XR player at a target location and adjust height
/// </summary>
public class XRPlayerSetup : MonoBehaviour
{
    [Header("Initialization")]
    [Tooltip("Time to wait before executing actions")]
    public float timeToWait = 1.5f;

    [Tooltip("Events to trigger on start")]
    public UnityEvent onStart;

    [Header("Player Height")]
    [Tooltip("Should the player's height be adjusted for this level")]
    public bool adjustPlayerHeight = false;

    [Tooltip("Height to set for the player in this level")]
    public float playerHeight = 1.3f;

    [Tooltip("XR Origin reference (found automatically if null)")]
    public Transform xrRig;

    private IEnumerator Start()
    {
        // Find XR Rig if not assigned
        if (adjustPlayerHeight && xrRig == null)
        {
            xrRig = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>()?.transform;
            if (xrRig == null)
                xrRig = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        // Wait the specified time
        yield return new WaitForSeconds(timeToWait);

        // Apply height adjustment if enabled
        if (adjustPlayerHeight && xrRig != null)
        {
            ApplyPlayerHeight(playerHeight);
            Debug.Log($"Adjusted player height to {playerHeight}m");
        }

        // Trigger the onStart event
        onStart?.Invoke();
    }

    /// <summary>
    /// Applies the specified absolute height by calculating the required offset based on camera local position
    /// </summary>
    /// <param name="targetAbsoluteHeight">The desired absolute height from floor in meters</param>
    public void ApplyPlayerHeight(float targetAbsoluteHeight)
    {
        if (xrRig == null) return;

        var xrOrigin = xrRig.GetComponent<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            // Get camera reference
            var xrCamera = xrOrigin.Camera;
            if (xrCamera == null)
            {
                Debug.LogWarning("XR Camera not found. Cannot adjust height.");
                return;
            }

            // Get camera's local Y position (this is the HMD's position relative to the offset)
            float cameraLocalY = xrCamera.transform.localPosition.y;

            // Calculate required offset to achieve the target absolute height
            // Example: If target height is 1.4m and camera local Y is 0.4m, offset should be 1.0m
            float requiredOffset = targetAbsoluteHeight - cameraLocalY;

            Debug.Log($"Camera local Y: {cameraLocalY}m, Target absolute height: {targetAbsoluteHeight}m, Required offset: {requiredOffset}m");

            // Set the camera height offset for XR Origin
            xrOrigin.CameraYOffset = requiredOffset;

            // Force update by adjusting the camera offset transform directly
            var cameraOffsetTransform = xrCamera.transform.parent;
            if (cameraOffsetTransform != null)
            {
                Vector3 position = cameraOffsetTransform.localPosition;
                position.y = requiredOffset;
                cameraOffsetTransform.localPosition = position;

                // Calculate final expected world height for verification
                float expectedWorldHeight = xrRig.position.y + position.y + cameraLocalY;
                Debug.Log($"Camera offset set to: {requiredOffset}m, Expected absolute height: {expectedWorldHeight}m");
            }
        }
        else
        {
            Debug.LogWarning("XR Origin component not found on target. Cannot adjust height.");
        }
    }
    
}
