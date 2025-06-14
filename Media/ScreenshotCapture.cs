using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Provides functionality for taking high-quality screenshots with various options
/// for resolution, format, and source cameras.
/// </summary>
public class ScreenshotCapture : MonoBehaviour
{
    // Singleton implementation
    public static ScreenshotCapture Instance { get; private set; }

    [Header("Screenshot Settings")]
    [Tooltip("Save screenshots to the system Pictures folder")]
    [SerializeField] private bool saveToSystemPicturesFolder = true;
    
    [Tooltip("Folder where screenshots will be saved (if not using system Pictures folder)")]
    [SerializeField] private string outputFolder = "Screenshots";
    
    [Tooltip("Base filename for screenshots")]
    [SerializeField] private string baseFileName = "Screenshot";
    
    [Tooltip("Add timestamp to filenames")]
    [SerializeField] private bool useTimestamp = true;
    
    [Tooltip("Add sequential numbering to filenames")]
    [SerializeField] private bool useSequentialNumbering = false;
    
    [Tooltip("Starting number for sequential numbering")]
    [SerializeField] private int sequenceStartNumber = 1;
    
    [Header("Image Settings")]
    [Tooltip("Format to save the screenshot in")]
    [SerializeField] private ImageFormat imageFormat = ImageFormat.PNG;
    
    [Tooltip("JPEG Quality (only for JPG format)")]
    [Range(1, 100)]
    [SerializeField] private int jpegQuality = 95;
    
    [Tooltip("Include alpha channel (only for PNG format)")]
    [SerializeField] private bool captureAlpha = false;
    
    [Header("Resolution Settings")]
    [Tooltip("Method to determine the resolution")]
    [SerializeField] private ResolutionMode resolutionMode = ResolutionMode.CurrentResolution;
    
    [Tooltip("Predefined resolution for the screenshot")]
    [SerializeField] private PredefinedResolution predefinedResolution = PredefinedResolution.FullHD_1080p;
    
    [Tooltip("Resolution multiplier (for supersampling)")]
    [Range(1, 8)]
    [SerializeField] private int resolutionMultiplier = 1;
    
    [Tooltip("Custom width (only used with Custom Resolution)")]
    [SerializeField] private int customWidth = 1920;
    
    [Tooltip("Custom height (only used with Custom Resolution)")]
    [SerializeField] private int customHeight = 1080;
    
    [Header("Capture Source")]
    [Tooltip("Source for the screenshot")]
    [SerializeField] private CaptureSource captureSource = CaptureSource.GameView;
    
    [Tooltip("Camera to use for screenshot (if Camera source is selected)")]
    [SerializeField] private Camera targetCamera;
    
    [Tooltip("Include UI elements in the screenshot")]
    [SerializeField] private bool captureUI = true;
    
    [Header("Advanced Options")]
    [Tooltip("Clear flags for camera capture")]
    [SerializeField] private CameraClearFlags clearFlags = CameraClearFlags.Skybox;
    
    [Tooltip("Background color (used with SolidColor clear flags)")]
    [SerializeField] private Color backgroundColor = Color.black;
    
    [Tooltip("Take screenshots even in headless mode")]
    [SerializeField] private bool allowHeadlessCapture = false;
    
    [Header("Burst Mode")]
    [Tooltip("Enable burst mode (multiple screenshots)")]
    [SerializeField] private bool burstMode = false;
    
    [Tooltip("Number of screenshots to take in burst mode")]
    [Range(2, 20)]
    [SerializeField] private int burstCount = 3;
    
    [Tooltip("Delay between burst screenshots (seconds)")]
    [SerializeField] private float burstDelay = 0.5f;
    
    [Header("Input Settings")]
    [Tooltip("Enable input action for taking screenshots")]
    [SerializeField] private bool enableInputAction = true;
    
    [Tooltip("Reference to the Input Action for taking screenshots")]
    [SerializeField] private InputActionProperty screenshotAction;

    // Runtime variables
    private int currentSequenceNumber;
    private bool isTakingScreenshot = false;
    private string lastScreenshotPath = "";
    private Texture2D lastScreenshot = null;

    // Enums for options
    public enum ImageFormat { PNG, JPG, EXR }
    public enum ResolutionMode { CurrentResolution, PredefinedResolution, CustomResolution, SuperSize }
    public enum CaptureSource { GameView, Camera, RenderTexture }
    
    /// <summary>
    /// Predefined resolutions for screenshots
    /// </summary>
    public enum PredefinedResolution
    {
        HD_720p,           // 1280x720
        FullHD_1080p,      // 1920x1080
        QHD_1440p,         // 2560x1440
        UHD_4K,            // 3840x2160
        UHD_8K,            // 7680x4320
        
        // Common aspect ratios
        Square_1x1,        // 1080x1080
        Widescreen_21x9,   // 2560x1080
        Ultrawide_32x9,    // 3840x1080
        Portrait_9x16,     // 1080x1920
        
        // Social media optimized
        Twitter_16x9,      // 1200x675
        Instagram_1x1,     // 1080x1080
        Instagram_4x5,     // 1080x1350
        Facebook_16x9,     // 1200x630
        
        // Custom resolution
        Custom             // Uses customWidth and customHeight
    }

    void Awake()
    {
        // Singleton implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize sequence number
        currentSequenceNumber = sequenceStartNumber;
    }

    private void OnEnable()
    {
        // Enable the input action if configured
        if (enableInputAction && screenshotAction != null && screenshotAction.action != null)
        {
            screenshotAction.action.Enable();
            screenshotAction.action.performed += OnScreenshotTriggered;
        }
    }

    private void OnDisable()
    {
        // Disable the input action
        if (screenshotAction != null && screenshotAction.action != null)
        {
            screenshotAction.action.performed -= OnScreenshotTriggered;
            screenshotAction.action.Disable();
        }
    }

    private void OnScreenshotTriggered(InputAction.CallbackContext context)
    {
        if (!enableInputAction || isTakingScreenshot) return;
        
        if (burstMode)
            StartCoroutine(TakeScreenshotBurst());
        else
            TakeScreenshot();
    }

    /// <summary>
    /// Takes a screenshot with the current settings
    /// </summary>
    public void TakeScreenshot()
    {
        // Early exit if already processing
        if (isTakingScreenshot) return;
        
        isTakingScreenshot = true;
        
        // Skip if in headless mode and not allowed
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null && !allowHeadlessCapture)
        {
            Debug.LogWarning("Screenshot capture: Skipped in headless mode");
            isTakingScreenshot = false;
            return;
        }
        
        try
        {
            // Create the filename
            string fileName = GenerateFileName();
            
            // Create the full path
            string fullPath = CreateOutputPath(fileName);
            
            // Get screenshot dimensions
            int width, height;
            GetScreenshotDimensions(out width, out height);
            
            // Take the actual screenshot
            Texture2D screenshot = CaptureScreenshot(width, height);
            
            // Save the screenshot
            SaveScreenshot(screenshot, fullPath);
            
            // Store the last screenshot
            lastScreenshot = screenshot;
            lastScreenshotPath = fullPath;
            
            // Increment sequence number if using sequential numbering
            if (useSequentialNumbering)
            {
                currentSequenceNumber++;
            }
            
            Debug.Log($"Screenshot saved: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error taking screenshot: {e.Message}");
        }
        
        isTakingScreenshot = false;
    }

    /// <summary>
    /// Takes a series of screenshots in quick succession
    /// </summary>
    public IEnumerator TakeScreenshotBurst()
    {
        if (isTakingScreenshot) yield break;
        
        Debug.Log($"Starting burst mode: {burstCount} screenshots with {burstDelay}s delay");
        
        for (int i = 0; i < burstCount; i++)
        {
            TakeScreenshot();
            yield return new WaitForSeconds(burstDelay);
        }
        
        Debug.Log("Burst mode completed");
    }

    /// <summary>
    /// Generates a filename based on current settings
    /// </summary>
    private string GenerateFileName()
    {
        string fileName = baseFileName;
        
        // Add timestamp if enabled
        if (useTimestamp)
        {
            fileName += "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }
        
        // Add sequence number if enabled
        if (useSequentialNumbering)
        {
            fileName += "_" + currentSequenceNumber.ToString("D4");
        }
        
        // Add resolution info
        int width, height;
        GetScreenshotDimensions(out width, out height);
        fileName += $"_{width}x{height}";
        
        // Add extension
        switch (imageFormat)
        {
            case ImageFormat.PNG:
                fileName += ".png";
                break;
            case ImageFormat.JPG:
                fileName += ".jpg";
                break;
            case ImageFormat.EXR:
                fileName += ".exr";
                break;
        }
        
        return fileName;
    }

    /// <summary>
    /// Creates the output directory and returns the full path
    /// </summary>
    private string CreateOutputPath(string fileName)
    {
        string basePath;
        string gameFolder = Application.productName;
        
        try
        {
            if (saveToSystemPicturesFolder)
            {
                // Get the user's Pictures folder
                basePath = GetPicturesFolder();
                
                // Create game subfolder
                string gameFolderPath = Path.Combine(basePath, gameFolder);
                
                if (!Directory.Exists(gameFolderPath))
                {
                    Directory.CreateDirectory(gameFolderPath);
                }
                
                return Path.Combine(gameFolderPath, fileName);
            }
            else
            {
                // Use the default path
                #if UNITY_EDITOR
                // In editor, use the project directory
                basePath = Path.Combine(Application.dataPath, "..");
                #else
                // In builds, use persistent data path
                basePath = Application.persistentDataPath;
                #endif
                
                string fullOutputPath = Path.Combine(basePath, outputFolder);
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(fullOutputPath))
                {
                    Directory.CreateDirectory(fullOutputPath);
                }
                
                return Path.Combine(fullOutputPath, fileName);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating output path: {e.Message}. Falling back to persistent data path.");
            
            // Fallback to persistent data path
            string fallbackPath = Path.Combine(Application.persistentDataPath, outputFolder);
            
            if (!Directory.Exists(fallbackPath))
            {
                Directory.CreateDirectory(fallbackPath);
            }
            
            return Path.Combine(fallbackPath, fileName);
        }
    }
    
    /// <summary>
    /// Gets the user's Pictures folder path
    /// </summary>
    private string GetPicturesFolder()
    {
        string picturesPath;
        
        try
        {
            // Get the system's Pictures folder
            picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            
            // If the path is empty, try alternative methods
            if (string.IsNullOrEmpty(picturesPath))
            {
                string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                
                if (Application.platform == RuntimePlatform.WindowsPlayer || 
                    Application.platform == RuntimePlatform.WindowsEditor)
                {
                    // For Windows
                    picturesPath = Path.Combine(userProfilePath, "Pictures");
                }
                else if (Application.platform == RuntimePlatform.OSXPlayer || 
                         Application.platform == RuntimePlatform.OSXEditor)
                {
                    // For macOS
                    picturesPath = Path.Combine(userProfilePath, "Pictures");
                }
                else
                {
                    // For Linux or other platforms
                    picturesPath = userProfilePath;
                }
            }
            
            // Create directory if it doesn't exist (unlikely but just in case)
            if (!Directory.Exists(picturesPath))
            {
                Directory.CreateDirectory(picturesPath);
            }
            
            return picturesPath;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting Pictures folder: {e.Message}");
            return Application.persistentDataPath;
        }
    }

    /// <summary>
    /// Gets the dimensions for the screenshot based on settings
    /// </summary>
    private void GetScreenshotDimensions(out int width, out int height)
    {
        switch (resolutionMode)
        {
            case ResolutionMode.CurrentResolution:
                width = Screen.width;
                height = Screen.height;
                break;
                
            case ResolutionMode.SuperSize:
                width = Screen.width * resolutionMultiplier;
                height = Screen.height * resolutionMultiplier;
                break;
                
            case ResolutionMode.PredefinedResolution:
                GetPredefinedResolutionDimensions(out width, out height);
                break;
                
            case ResolutionMode.CustomResolution:
                width = customWidth;
                height = customHeight;
                break;
                
            default:
                width = Screen.width;
                height = Screen.height;
                break;
        }
    }

    /// <summary>
    /// Gets dimensions for predefined resolutions
    /// </summary>
    private void GetPredefinedResolutionDimensions(out int width, out int height)
    {
        switch (predefinedResolution)
        {
            case PredefinedResolution.HD_720p:
                width = 1280; height = 720; break;
            case PredefinedResolution.FullHD_1080p:
                width = 1920; height = 1080; break;
            case PredefinedResolution.QHD_1440p:
                width = 2560; height = 1440; break;
            case PredefinedResolution.UHD_4K:
                width = 3840; height = 2160; break;
            case PredefinedResolution.UHD_8K:
                width = 7680; height = 4320; break;
                
            case PredefinedResolution.Square_1x1:
                width = 1080; height = 1080; break;
            case PredefinedResolution.Widescreen_21x9:
                width = 2560; height = 1080; break;
            case PredefinedResolution.Ultrawide_32x9:
                width = 3840; height = 1080; break;
            case PredefinedResolution.Portrait_9x16:
                width = 1080; height = 1920; break;
                
            case PredefinedResolution.Twitter_16x9:
                width = 1200; height = 675; break;
            case PredefinedResolution.Instagram_1x1:
                width = 1080; height = 1080; break;
            case PredefinedResolution.Instagram_4x5:
                width = 1080; height = 1350; break;
            case PredefinedResolution.Facebook_16x9:
                width = 1200; height = 630; break;
                
            case PredefinedResolution.Custom:
                width = customWidth; height = customHeight; break;
                
            default:
                width = 1920; height = 1080; break;
        }
    }

    /// <summary>
    /// Captures the screenshot with specified dimensions
    /// </summary>
    private Texture2D CaptureScreenshot(int width, int height)
    {
        Texture2D screenshot = null;
        
        switch (captureSource)
        {
            case CaptureSource.GameView:
                screenshot = CaptureGameView(width, height);
                break;
                
            case CaptureSource.Camera:
                screenshot = CaptureFromCamera(width, height);
                break;
                
            case CaptureSource.RenderTexture:
                Debug.LogWarning("RenderTexture capture source requires custom setup. Falling back to GameView.");
                screenshot = CaptureGameView(width, height);
                break;
        }
        
        return screenshot;
    }

    /// <summary>
    /// Captures screenshot from the game view
    /// </summary>
    private Texture2D CaptureGameView(int width, int height)
    {
        // Create render texture
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture prevRT = RenderTexture.active;
        
        // Capture the screen
        if (captureUI)
        {
            // Use ScreenCapture for UI elements
            ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
        }
        else
        {
            // For no UI, try to use main camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                RenderTexture prevCamRT = mainCam.targetTexture;
                mainCam.targetTexture = rt;
                mainCam.Render();
                mainCam.targetTexture = prevCamRT;
            }
            else
            {
                Debug.LogWarning("No main camera found. UI elements may still be included.");
                ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
            }
        }
        
        // Create texture and read pixels
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(width, height, 
            captureAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();
        
        // Restore render texture
        RenderTexture.active = prevRT;
        rt.Release();
        Destroy(rt);
        
        return screenshot;
    }

    /// <summary>
    /// Captures screenshot from a specific camera
    /// </summary>
    private Texture2D CaptureFromCamera(int width, int height)
    {
        // Find camera if not assigned
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogWarning("No camera assigned or found for screenshot. Using GameView instead.");
                return CaptureGameView(width, height);
            }
        }
        
        // Create render texture
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture prevRT = targetCamera.targetTexture;
        
        // Configure camera temporarily
        CameraClearFlags originalClearFlags = targetCamera.clearFlags;
        Color originalBgColor = targetCamera.backgroundColor;
        
        targetCamera.clearFlags = clearFlags;
        targetCamera.backgroundColor = backgroundColor;
        
        // Render to texture
        targetCamera.targetTexture = rt;
        targetCamera.Render();
        
        // Create texture and read pixels
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(width, height, 
            captureAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();
        
        // Restore camera settings
        targetCamera.targetTexture = prevRT;
        targetCamera.clearFlags = originalClearFlags;
        targetCamera.backgroundColor = originalBgColor;
        
        // Restore render texture
        RenderTexture.active = null;
        rt.Release();
        Destroy(rt);
        
        return screenshot;
    }

    /// <summary>
    /// Saves the screenshot to disk
    /// </summary>
    private void SaveScreenshot(Texture2D screenshot, string path)
    {
        byte[] imageBytes;
        
        switch (imageFormat)
        {
            case ImageFormat.PNG:
                imageBytes = screenshot.EncodeToPNG();
                break;
                
            case ImageFormat.JPG:
                imageBytes = screenshot.EncodeToJPG(jpegQuality);
                break;
                
            case ImageFormat.EXR:
                imageBytes = screenshot.EncodeToEXR();
                break;
                
            default:
                imageBytes = screenshot.EncodeToPNG();
                break;
        }
        
        // Write to disk
        File.WriteAllBytes(path, imageBytes);
        
        // Refresh asset database if in editor
        #if UNITY_EDITOR
        AssetDatabase.Refresh();
        #endif
    }

    /// <summary>
    /// Opens the folder containing screenshots
    /// </summary>
    public void OpenOutputFolder()
    {
        string folderPath;
        
        if (saveToSystemPicturesFolder)
        {
            string picturesFolder = GetPicturesFolder();
            string gameFolderPath = Path.Combine(picturesFolder, Application.productName);
            
            // Create game folder if it doesn't exist
            if (!Directory.Exists(gameFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(gameFolderPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create directory: {e.Message}");
                    return;
                }
            }
            
            folderPath = gameFolderPath;
        }
        else
        {
            // Use default path
            #if UNITY_EDITOR
            string basePath = Path.Combine(Application.dataPath, "..");
            #else
            string basePath = Application.persistentDataPath;
            #endif
            
            folderPath = Path.Combine(basePath, outputFolder);
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }
        
        // Open folder
        #if UNITY_EDITOR
        EditorUtility.RevealInFinder(folderPath);
        #else
        Application.OpenURL("file://" + folderPath);
        #endif
    }

    /// <summary>
    /// Gets the path to the last screenshot taken
    /// </summary>
    public string GetLastScreenshotPath()
    {
        return lastScreenshotPath;
    }
    
    /// <summary>
    /// Gets the current folder where screenshots are being saved
    /// </summary>
    public string GetCurrentSaveFolder()
    {
        if (saveToSystemPicturesFolder)
        {
            string picturesFolder = GetPicturesFolder();
            return Path.Combine(picturesFolder, Application.productName);
        }
        else
        {
            #if UNITY_EDITOR
            string basePath = Path.Combine(Application.dataPath, "..");
            #else
            string basePath = Application.persistentDataPath;
            #endif
            
            return Path.Combine(basePath, outputFolder);
        }
    }
    
    /// <summary>
    /// Gets the texture of the last screenshot taken
    /// </summary>
    public Texture2D GetLastScreenshot()
    {
        return lastScreenshot;
    }
    
    /// <summary>
    /// Sets a custom target camera
    /// </summary>
    public void SetTargetCamera(Camera camera)
    {
        targetCamera = camera;
        captureSource = CaptureSource.Camera;
    }
    
    /// <summary>
    /// Resets the sequence numbering to the start value
    /// </summary>
    public void ResetSequenceNumbering()
    {
        currentSequenceNumber = sequenceStartNumber;
    }
}
