using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class SoundManagerWindow : EditorWindow
{
    public VisualTreeAsset mainTree;
    public VisualTreeAsset entryTemplate;

    // Serialized field to bind via UXML binding-path
    [SerializeField]
    private SoundsSO soundsSO;

    private ScrollView soundListContainer;
    private AudioSource previewAudioSource;
    private GameObject previewGameObject;

    [MenuItem("Tools/Sound Manager")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<SoundManagerWindow>();
        wnd.titleContent = new GUIContent("Sound Manager");
    }

    public void CreateGUI()
    {
        // Build UI
        rootVisualElement.Clear();
        mainTree.CloneTree(rootVisualElement);

        // Bind serialized fields (requires binding-path="soundsSO" on the ObjectField in UXML)
        var so = new SerializedObject(this);
        rootVisualElement.Bind(so);

        // Get the container - now as ScrollView instead of ListView
        soundListContainer = rootVisualElement.Q<ScrollView>("soundListContainer");
        if (soundListContainer == null)
        {
            // Fallback: try to find it as a generic VisualElement and use it
            var container = rootVisualElement.Q("soundListContainer");
            if (container != null)
            {
                // Create a ScrollView as replacement
                soundListContainer = new ScrollView();
                soundListContainer.name = "soundListContainer";
                soundListContainer.style.flexGrow = 1;
                
                // Replace the container with our ScrollView
                container.parent.Add(soundListContainer);
                container.parent.Remove(container);
            }
            else
            {
                Debug.LogError("Could not find soundListContainer in the UI");
            }
        }

        var addButton = rootVisualElement.Q<Button>("addSoundButton");
        addButton.clicked += () =>
        {
            if (!EnsureSO()) return;
            Undo.RecordObject(soundsSO, "Add Sound");
            soundsSO.AddSound();
            EditorUtility.SetDirty(soundsSO);
            RefreshSoundList();
        };

        // Initial population
        if (soundsSO != null)
            RefreshSoundList();
            
        // Register for Undo/Redo events with delayed execution
        Undo.undoRedoPerformed += OnUndoRedo;
    }
    
    private void OnUndoRedo()
    {
        // Delay the UI update to avoid issues during Undo/Redo operations
        EditorApplication.delayCall += () =>
        {
            if (this != null && soundsSO != null)
            {
                RefreshSoundList();
            }
        };
    }
    
    private void OnEnable()
    {
        // Create a hidden GameObject with AudioSource for previewing sounds in editor
        CreatePreviewAudioSource();
    }
    
    private void OnDisable()
    {
        // Clean up
        Undo.undoRedoPerformed -= OnUndoRedo;
        DestroyPreviewAudioSource();
    }
    
    private void CreatePreviewAudioSource()
    {
        // Create or find preview audio source
        if (previewGameObject == null)
        {
            previewGameObject = GameObject.Find("__SoundPreview");
            if (previewGameObject == null)
            {
                previewGameObject = new GameObject("__SoundPreview");
                previewGameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            
            previewAudioSource = previewGameObject.GetComponent<AudioSource>();
            if (previewAudioSource == null)
            {
                previewAudioSource = previewGameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    private void DestroyPreviewAudioSource()
    {
        if (previewGameObject != null)
        {
            DestroyImmediate(previewGameObject);
            previewGameObject = null;
            previewAudioSource = null;
        }
    }

    private bool EnsureSO()
    {
        if (soundsSO == null)
        {
            EditorUtility.DisplayDialog("Sound Manager", "Please assign a SoundsSO asset first.", "OK");
            return false;
        }
        return true;
    }
    
    private void PreviewSound(SoundData data)
    {
        if (data.clips == null || data.clips.Length == 0 || previewAudioSource == null)
        {
            Debug.LogWarning("Cannot preview sound: No audio clips or preview source available");
            return;
        }
        
        // Find a non-null clip to play
        AudioClip clipToPlay = null;
        List<AudioClip> validClips = new List<AudioClip>();
        
        foreach (var clip in data.clips)
        {
            if (clip != null)
                validClips.Add(clip);
        }
        
        if (validClips.Count == 0)
        {
            Debug.LogWarning("Cannot preview sound: All clips are null");
            return;
        }
        
        // Select a random clip from valid clips
        clipToPlay = validClips[Random.Range(0, validClips.Count)];
        
        // Configure audio source
        previewAudioSource.clip = clipToPlay;
        previewAudioSource.volume = data.volume;
        previewAudioSource.outputAudioMixerGroup = data.audioMixerGroup;
        
        // Apply pitch variation if enabled
        if (data.usePitchVariation && data.minPitch > 0 && data.maxPitch > 0)
        {
            previewAudioSource.pitch = Random.Range(data.minPitch, data.maxPitch);
        }
        else
        {
            previewAudioSource.pitch = 1.0f;
        }
        
        // Play the sound
        previewAudioSource.Play();
    }

    private void RefreshSoundList()
    {
        if (soundListContainer == null) return;
        
        // Clear the container completely
        soundListContainer.Clear();
        
        // Ensure all children are removed (defensive)
        while (soundListContainer.childCount > 0)
        {
            soundListContainer.RemoveAt(0);
        }
        
        if (!EnsureSO()) return;

        for (int i = 0; i < soundsSO.sounds.Count; i++)
        {
            var data = soundsSO.sounds[i];
            int index = i;
            var entry = entryTemplate.CloneTree();

            // Set up toggle functionality for content visibility
            var hideContentButton = entry.Q<Button>("hideContentButton");
            var contentVE = entry.Q<VisualElement>("contentVE");
            
            // Hide content by default
            contentVE.style.display = DisplayStyle.None;
            
            // Set button text/icon
            hideContentButton.text = "    ▲"; // Down arrow to indicate "expand"
            
            // Set up toggle functionality
            hideContentButton.clicked += () => {
                // Toggle display
                bool isVisible = contentVE.style.display == DisplayStyle.Flex;
                contentVE.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
                
                // Update button text/icon
                hideContentButton.text = isVisible ? "    ▲" : "    ▼"; // Down arrow for collapsed, up arrow for expanded
            };

            // Set title label
            var titleLabel = entry.Q<Label>("soundTitleLabel");
            titleLabel.text = string.IsNullOrEmpty(data.name) ? $"Sound {i + 1}" : data.name;

            // Name field
            var nameField = entry.Q<TextField>("soundNameTextField");
            nameField.value = data.name;
            nameField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(soundsSO, "Change Sound Name");
                data.name = evt.newValue;
                titleLabel.text = string.IsNullOrEmpty(evt.newValue) ? $"Sound {index + 1}" : evt.newValue;
                EditorUtility.SetDirty(soundsSO);
            });

            // Volume slider
            var volumeSlider = entry.Q<Slider>("volumeSlider");
            volumeSlider.value = data.volume;
            volumeSlider.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(soundsSO, "Change Volume");
                data.volume = evt.newValue;
                EditorUtility.SetDirty(soundsSO);
            });

            // Pitch variation toggle
            var pitchContainer = entry.Q("pitchSlidersContainer");
            var usePitchToggle = entry.Q<Toggle>("usePitchToggle");
            usePitchToggle.value = data.usePitchVariation;
            
            // Set initial visibility based on toggle state
            pitchContainer.style.display = data.usePitchVariation ? DisplayStyle.Flex : DisplayStyle.None;
            
            // Register toggle callback
            usePitchToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(soundsSO, "Toggle Pitch Variation");
                data.usePitchVariation = evt.newValue;
                pitchContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                EditorUtility.SetDirty(soundsSO);
            });

            // Pitch sliders
            var minPitchSlider = entry.Q<Slider>("minPitchSlider");
            minPitchSlider.value = data.minPitch;
            minPitchSlider.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(soundsSO, "Change Min Pitch");
                data.minPitch = evt.newValue;
                EditorUtility.SetDirty(soundsSO);
            });

            var maxPitchSlider = entry.Q<Slider>("maxPitchSlider");
            maxPitchSlider.value = data.maxPitch;
            maxPitchSlider.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(soundsSO, "Change Max Pitch");
                data.maxPitch = evt.newValue;
                EditorUtility.SetDirty(soundsSO);
            });

            // AudioMixerGroup field
            var mixerField = entry.Q<ObjectField>("mixerGroupObjectField");
            mixerField.objectType = typeof(AudioMixerGroup);
            mixerField.allowSceneObjects = false;
            mixerField.value = data.audioMixerGroup;
            mixerField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(soundsSO, "Change Audio Mixer Group");
                data.audioMixerGroup = evt.newValue as AudioMixerGroup;
                EditorUtility.SetDirty(soundsSO);
            });

            // Configure the pre-existing audiosList PropertyField
            var audiosList = entry.Q<PropertyField>("audiosList");
            if (audiosList != null)
            {
                var serializedObject = new SerializedObject(soundsSO);
                var soundsProperty = serializedObject.FindProperty("sounds");
                var soundProperty = soundsProperty.GetArrayElementAtIndex(index);
                var clipsProperty = soundProperty.FindPropertyRelative("clips");
                
                audiosList.bindingPath = null; // Clear any existing binding
                audiosList.label = "Audio Clips";
                audiosList.Bind(serializedObject); // Bind to the serialized object
                audiosList.BindProperty(clipsProperty); // Bind specifically to the clips property
                
                // Update the serialized object when changes occur
                serializedObject.Update();
                audiosList.RegisterCallback<SerializedPropertyChangeEvent>(evt => {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(soundsSO);
                });
            }

            // Test sound button with editor support
            var testBtn = entry.Q<Button>("testSoundButton");
            testBtn.clicked += () =>
            {
                // If in Play Mode, use the SoundManager
                if (Application.isPlaying)
                {
                    var manager = FindFirstObjectByType<SoundManager>();
                    if (manager != null)
                        manager.PlaySound(data.name);
                    else
                        Debug.LogWarning("SoundManager not found in scene.");
                }
                // If in Editor Mode, play directly
                else
                {
                    PreviewSound(data);
                }
            };

            // Remove sound button
            var removeBtn = entry.Q<Button>("removeButton");
            removeBtn.clicked += () =>
            {
                if (EditorUtility.DisplayDialog("Remove Sound", 
                    $"Are you sure you want to remove the sound '{data.name}'?", 
                    "Yes", "No"))
                {
                    Undo.RecordObject(soundsSO, "Remove Sound");
                    soundsSO.RemoveSoundAt(index);
                    EditorUtility.SetDirty(soundsSO);
                    RefreshSoundList();
                }
            };

            // Use Add() consistently instead of hierarchy.Add()
            soundListContainer.Add(entry);
        }
    }
}