#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Advanced recording tool for Unity with predefined resolution options,
/// optimized for recording what is seen through VR headsets.
/// </summary>
public class AdvancedVideoRecorder : MonoBehaviour
{
    // Singleton implementation
    public static AdvancedVideoRecorder Instance { get; private set; }

    [Header("Recording Status")]
    [Tooltip("Current recording status")]
    [SerializeField] private string recordingStatus = "Ready";
    
    [Tooltip("Current recording duration")]
    [SerializeField] private float recordingDuration = 0f;

    [Header("Recording Settings")]
    [Tooltip("Enable to automatically start recording when entering play mode")]
    [SerializeField] private bool autoStartRecording = false;
    
    [Tooltip("Enable to automatically stop recording when exiting play mode")]
    [SerializeField] private bool autoStopOnExit = true;
    
    [Tooltip("Maximum recording time in seconds (0 = unlimited)")]
    [SerializeField] private float maxRecordingTime = 0f;
    
    [Tooltip("Folder where recordings will be saved (relative to project folder)")]
    [SerializeField] private string outputFolder = "Recordings";
    
    [Tooltip("Base filename for recordings (date/time will be appended)")]
    [SerializeField] private string baseFileName = "Recording";
    
    [Tooltip("Use timestamp in filename")]
    [SerializeField] private bool useTimestamp = true;
    
    [Tooltip("Continue recording when changing scenes")]
    [SerializeField] private bool persistBetweenScenes = true;

    [Header("Video Settings")]
    [Tooltip("Video codec to use for recording")]
    [SerializeField] private VideoCodec videoCodec = VideoCodec.MP4;
    
    [Tooltip("Quality of the video encoding")]
    [SerializeField] private VideoQuality videoQuality = VideoQuality.High;
    
    [Tooltip("Predefined resolution for the recording")]
    [SerializeField] private RecordingResolution recordingResolution = RecordingResolution.FullHD_1080p;
    
    [Tooltip("Custom width (only used if Custom Resolution is selected)")]
    [SerializeField] private int customWidth = 1280;
    
    [Tooltip("Custom height (only used if Custom Resolution is selected)")]
    [SerializeField] private int customHeight = 720;
    
    [Tooltip("Capture alpha channel (transparent background)")]
    [SerializeField] private bool captureAlpha = false;
    
    [Tooltip("Frame rate for recording")]
    [Range(15, 120)]
    [SerializeField] private int frameRate = 60;

    [Header("Audio Settings")]
    [Tooltip("Include audio in the recording")]
    [SerializeField] private bool recordAudio = true;
    
    [Header("Input Source")]
    [Tooltip("Source for the recording")]
    [SerializeField] private RecordingSource recordingSource = RecordingSource.GameView;
    
    [Tooltip("Camera to use for recording (if Camera source is selected)")]
    [SerializeField] private Camera targetCamera;

    // Recording controller
    private RecorderController recorderController;
    private MovieRecorderSettings recorderSettings;
    private bool isRecording = false;
    private float recordingStartTime = 0f;
    private string currentOutputPath = "";

    // Enums for options
    public enum VideoCodec { MP4, WebM }
    public enum VideoQuality { Low, Medium, High, VeryHigh }
    public enum RecordingSource { GameView, Camera, RenderTexture }
    
    /// <summary>
    /// Predefined resolutions for video recording
    /// </summary>
    public enum RecordingResolution
    {
        // Standard resolutions
        HD_720p,           // 1280x720 - Standard HD
        FullHD_1080p,      // 1920x1080 - Standard Full HD
        QHD_1440p,         // 2560x1440 - Standard Quad HD
        UHD_4K,            // 3840x2160 - Standard 4K
        
        // Resolutions optimized for recording FROM VR headsets
        Quest2_View,       // 1832x1920 - Quest 2 view
        Quest3_View,       // 2064x2208 - Quest 3 view
        ViveXR_View,       // 2448x2448 - Vive XR view
        Index_View,        // 1440x1600 - Valve Index view
        
        // Social media formats
        Youtube_Standard,  // 1920x1080 - YouTube standard HD
        Youtube_Premium,   // 3840x2160 - YouTube 4K
        Instagram_Square,  // 1080x1080 - Instagram square
        Facebook_HD,       // 1920x1080 - Facebook HD
        TikTok_Vertical,   // 1080x1920 - TikTok vertical
        
        // Custom resolution
        Custom             // Uses customWidth and customHeight
    }

    void Awake()
    {
        transform.parent = null;
        // Singleton implementation to persist between scenes
        if (Instance != null && Instance != this)
        {
            // If an instance already exists and it's not us, destroy ourselves
            Destroy(gameObject);
            return;
        }
        
        // If we're the first instance, become the Singleton
        Instance = this;
        
        // Ensure the object persists between scenes if configured to do so
        if (persistBetweenScenes)
        {
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to the scene change event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    void OnDestroy()
    {
        // Clean up subscription to the event when destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // If we're the Singleton instance, clear the reference
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Make sure to stop recording if active
        if (isRecording)
        {
            StopRecording();
        }
    }

    void Start()
    {
        // Start automatically if configured
        if (autoStartRecording)
        {
            StartRecording();
        }
    }

    void Update()
    {
        // Update recording timer if active
        if (isRecording)
        {
            recordingDuration = Time.time - recordingStartTime;
            recordingStatus = $"Recording ({FormatTime(recordingDuration)})";
            
            // Check if time limit has been reached
            if (maxRecordingTime > 0 && recordingDuration >= maxRecordingTime)
            {
                StopRecording();
            }
        }
    }

    void OnDisable()
    {
        if (isRecording && autoStopOnExit)
        {
            StopRecording();
        }
    }
    
    /// <summary>
    /// Handles scene changes when recorder persists
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isRecording) return;
        
        // If we're recording and a camera was assigned, we need to re-assign it
        if (recordingSource == RecordingSource.Camera && targetCamera != null)
        {
            // Previous camera no longer exists or has changed, try to find a new one
            targetCamera = Camera.main;
            
            Debug.Log($"Scene changed during recording. Using new camera: {(targetCamera != null ? targetCamera.name : "none found")}");
        }
    }

    /// <summary>
    /// Starts the recording process with current settings
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("A recording is already in progress!");
            return;
        }

        // Create full output path
        string fileName = baseFileName;
        if (useTimestamp)
        {
            fileName += "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }

        // Create output directory if it doesn't exist
        string projectPath = Path.Combine(Application.dataPath, "..");
        string fullOutputPath = Path.Combine(projectPath, outputFolder);
        if (!Directory.Exists(fullOutputPath))
        {
            Directory.CreateDirectory(fullOutputPath);
        }

        currentOutputPath = Path.Combine(fullOutputPath, fileName);
        
        // Initialize recording controller
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderController = new RecorderController(controllerSettings);

        // Create and configure recording settings
        recorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        recorderSettings.name = "Video Recorder";
        recorderSettings.Enabled = true;

        // Configure codec
        recorderSettings.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = ConvertVideoQuality(videoQuality),
            Codec = videoCodec == VideoCodec.MP4 
                ? CoreEncoderSettings.OutputCodec.MP4 
                : CoreEncoderSettings.OutputCodec.WEBM
        };
        
        recorderSettings.CaptureAlpha = captureAlpha;

        // Get width and height for the selected resolution
        int width, height;
        GetResolutionDimensions(out width, out height);

        // Configure input source
        switch (recordingSource)
        {
            case RecordingSource.GameView:
                recorderSettings.ImageInputSettings = new GameViewInputSettings
                {
                    OutputWidth = width,
                    OutputHeight = height
                };
                break;
                
            case RecordingSource.Camera:
                if (targetCamera == null)
                {
                    targetCamera = Camera.main;
                    if (targetCamera == null)
                    {
                        Debug.LogError("No camera assigned or found for recording!");
                        return;
                    }
                }
                
                var cameraInputSettings = new CameraInputSettings
                {
                    OutputWidth = width,
                    OutputHeight = height,
                };
                
                // Configure camera correctly
                if (targetCamera == Camera.main)
                {
                    // Use main camera
                    cameraInputSettings.CameraTag = "MainCamera";
                }
                else
                {
                    // Try to use camera tag
                    if (!string.IsNullOrEmpty(targetCamera.gameObject.tag) && targetCamera.gameObject.tag != "Untagged")
                    {
                        cameraInputSettings.CameraTag = targetCamera.gameObject.tag;
                    }
                    else
                    {
                        // If no tag, use GameObject name
                        cameraInputSettings.CameraTag = "Untagged";
                        Debug.LogWarning("Camera has no specific tag. This may cause problems when selecting it for recording.");
                    }
                }
                
                recorderSettings.ImageInputSettings = cameraInputSettings;
                break;
                
            case RecordingSource.RenderTexture:
                // Using GameView as fallback
                recorderSettings.ImageInputSettings = new GameViewInputSettings
                {
                    OutputWidth = width,
                    OutputHeight = height
                };
                Debug.LogWarning("RenderTexture requires additional setup. Using GameView as alternative.");
                break;
        }

        // Configure audio
        recorderSettings.CaptureAudio = recordAudio;

        // Set output file path
        recorderSettings.OutputFile = currentOutputPath;

        // Setup recording
        controllerSettings.AddRecorderSettings(recorderSettings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = frameRate;

        RecorderOptions.VerboseMode = false;
        recorderController.PrepareRecording();
        recorderController.StartRecording();

        isRecording = true;
        recordingStartTime = Time.time;
        recordingStatus = "Recording...";
        
        Debug.Log($"Started recording to {currentOutputPath}");
    }

    /// <summary>
    /// Stops the current recording
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording || recorderController == null)
        {
            Debug.LogWarning("No active recording to stop!");
            return;
        }

        recorderController.StopRecording();
        
        recordingDuration = Time.time - recordingStartTime;
        isRecording = false;
        recordingStatus = $"Completed - {FormatTime(recordingDuration)}";
        
        Debug.Log($"Recording stopped. Duration: {recordingDuration:F2} seconds. Output: {currentOutputPath}");
    }

    /// <summary>
    /// Opens the folder containing recordings
    /// </summary>
    public void OpenOutputFolder()
    {
        string projectPath = Path.Combine(Application.dataPath, "..");
        string fullOutputPath = Path.Combine(projectPath, outputFolder);
        
        if (!Directory.Exists(fullOutputPath))
        {
            Directory.CreateDirectory(fullOutputPath);
        }
        
        EditorUtility.RevealInFinder(fullOutputPath);
    }

    /// <summary>
    /// Gets width and height dimensions for the selected resolution
    /// </summary>
    private void GetResolutionDimensions(out int width, out int height)
    {
        switch (recordingResolution)
        {
            // Standard resolutions
            case RecordingResolution.HD_720p:
                width = 1280; height = 720; break;
            case RecordingResolution.FullHD_1080p:
                width = 1920; height = 1080; break;
            case RecordingResolution.QHD_1440p:
                width = 2560; height = 1440; break;
            case RecordingResolution.UHD_4K:
                width = 3840; height = 2160; break;
                
            // VR headset resolutions
            case RecordingResolution.Quest2_View:
                width = 1832; height = 1920; break;
            case RecordingResolution.Quest3_View:
                width = 2064; height = 2208; break;
            case RecordingResolution.ViveXR_View:
                width = 2448; height = 2448; break;
            case RecordingResolution.Index_View:
                width = 1440; height = 1600; break;
                
            // Social media formats
            case RecordingResolution.Youtube_Standard:
                width = 1920; height = 1080; break;
            case RecordingResolution.Youtube_Premium:
                width = 3840; height = 2160; break;
            case RecordingResolution.Instagram_Square:
                width = 1080; height = 1080; break;
            case RecordingResolution.Facebook_HD:
                width = 1920; height = 1080; break;
            case RecordingResolution.TikTok_Vertical:
                width = 1080; height = 1920; break;
                
            // Custom resolution
            case RecordingResolution.Custom:
                width = customWidth; height = customHeight; break;
            default:
                width = 1920; height = 1080; break; // Full HD as default
        }
    }

    private CoreEncoderSettings.VideoEncodingQuality ConvertVideoQuality(VideoQuality quality)
    {
        switch (quality)
        {
            case VideoQuality.Low:
                return CoreEncoderSettings.VideoEncodingQuality.Low;
            case VideoQuality.Medium:
                return CoreEncoderSettings.VideoEncodingQuality.Medium;
            case VideoQuality.High:
                return CoreEncoderSettings.VideoEncodingQuality.High;
            case VideoQuality.VeryHigh:
                return CoreEncoderSettings.VideoEncodingQuality.High; // Using High as VeryHigh equivalent
            default:
                return CoreEncoderSettings.VideoEncodingQuality.Medium;
        }
    }

    private string FormatTime(float seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", 
            timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
    }
    
    /// <summary>
    /// Exposes the recording state for other classes
    /// </summary>
    public bool IsRecording => isRecording;
    
    /// <summary>
    /// Exposes the current recording duration
    /// </summary>
    public float RecordingDuration => recordingDuration;
}

#endif
