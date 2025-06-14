using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Juanxon.EditorSceneManager
{
    public class SceneManagerWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset visualTree;
        private SceneCollection sceneCollection;
        private VisualElement sceneListContainer;
        private VisualElement headerCreation;
        private bool showOptions = true;
        private string headerNameInputField;

        [MenuItem("Tools/Scene Manager %e")] // %e adds a keyboard shortcut (Ctrl+E on Windows, Cmd+E on macOS)
        public static void ShowWindow()
        {
            SceneManagerWindow wnd = GetWindow<SceneManagerWindow>();
            wnd.titleContent = new GUIContent("Editor Scene Manager");
        }
    
        public void CreateGUI()
        {
            if (visualTree == null)
            {
                Debug.LogError("The VisualTreeAsset is not assigned. Please assign one in the inspector.");
                return;
            }
            visualTree.CloneTree(rootVisualElement);

            var sceneCollectionField = rootVisualElement.Q<ObjectField>("sceneCollectionField");
            if (sceneCollectionField == null)
            {
                Debug.LogError("Could not find 'sceneCollectionField'. Check the name in the UXML.");
                return;
            }
            sceneCollectionField.objectType = typeof(SceneCollection);
            sceneCollectionField.RegisterValueChangedCallback(evt =>
            {
                sceneCollection = evt.newValue as SceneCollection;
                RefreshSceneList();
            });

            sceneListContainer = rootVisualElement.Q<ScrollView>("sceneListContainer");
            if (sceneListContainer == null)
            {
                Debug.LogError("Could not find 'sceneListContainer'. Check the name in the UXML.");
                return;
            }

            var addCurrentSceneButton = rootVisualElement.Q<Button>("addCurrentSceneButton");
            if (addCurrentSceneButton == null)
            {
                Debug.LogError("Could not find 'addCurrentSceneButton'. Check the name in the UXML.");
                return;
            }
            addCurrentSceneButton.clicked += AddCurrentScene;

            headerCreation = rootVisualElement.Q<VisualElement>("headerCreation");
            if (headerCreation == null)
            {
                Debug.LogError("Could not find 'createHeaderButton'. Check the name in the UXML.");
                return;
            }
        
            var showCreateheaderButton = rootVisualElement.Q<Button>("showCreateheaderButton");
            if (showCreateheaderButton == null)
            {
                Debug.LogError("Could not find 'createHeaderButton'. Check the name in the UXML.");
                return;
            }
            showCreateheaderButton.clicked += ToggleHeaderCreationVisibility;

            var optionsButton = rootVisualElement.Q<Button>("optionsButton");
            if (optionsButton == null)
            {
                Debug.LogError("Could not find 'optionsButton'. Check the name in the UXML.");
                return;
            }
            optionsButton.clicked += ToggleOptionsVisibility;

            var headerNameTextField = rootVisualElement.Q<TextField>("headerNameTextField");
            if (headerNameTextField == null)
            {
                Debug.LogError("Could not find 'optionsButton'. Check the name in the UXML.");
                return;
            }
            headerNameTextField.RegisterValueChangedCallback(evt =>
            {
                headerNameInputField = evt.newValue;
            });
        
            var createHeaderButton = rootVisualElement.Q<Button>("createHeaderButton");
            if (createHeaderButton == null)
            {
                Debug.LogError("Could not find 'optionsButton'. Check the name in the UXML.");
                return;
            }
            createHeaderButton.clicked += CreateHeader;
        
            sceneListContainer.RegisterCallback<DragEnterEvent>(OnDragEnter);
            sceneListContainer.RegisterCallback<DragLeaveEvent>(OnDragLeave);
            sceneListContainer.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            sceneListContainer.RegisterCallback<DragPerformEvent>(OnDragPerform);

            HideHeaderCreation();
            RefreshSceneList();
        }

        private void AddCurrentScene()
        {
            if (sceneCollection == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Scene Collection first.", "OK");
                return;
            }

            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                EditorUtility.DisplayDialog("Error", "The current scene must be saved first.", "OK");
                return;
            }

            SceneData newScene = new SceneData { sceneName = activeScene.name, scenePath = activeScene.path, isHeader = false};

            if (!sceneCollection.scenes.Exists(s => s.scenePath == activeScene.path))
            {
                sceneCollection.scenes.Add(newScene);
                EditorUtility.SetDirty(sceneCollection);
                RefreshSceneList();
            }
            else
            {
                EditorUtility.DisplayDialog("Notice", "Scene is already in the collection.", "OK");
            }
        }
    
        private void CreateHeader()
        {
            if (sceneCollection == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Scene Collection first.", "OK");
                return;
            }
        
            SceneData newScene = new SceneData { sceneName = headerNameInputField, scenePath = String.Empty, isHeader = true };
        
            sceneCollection.scenes.Add(newScene);
            EditorUtility.SetDirty(sceneCollection);
            RefreshSceneList();
        }
    
        private void AddSceneToCollection(SceneAsset sceneAsset)
        {
            if (sceneCollection == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Scene Collection first.", "OK");
                return;
            }

            // Asegúrate de que la escena no esté ya en la colección
            var newScene = new SceneData { sceneName = sceneAsset.name, scenePath = AssetDatabase.GetAssetPath(sceneAsset), isHeader = false };
    
            if (!sceneCollection.scenes.Exists(s => s.scenePath == newScene.scenePath))
            {
                sceneCollection.scenes.Add(newScene);
                EditorUtility.SetDirty(sceneCollection);
                RefreshSceneList();
            }
            else
            {
                EditorUtility.DisplayDialog("Notice", "Scene is already in the collection.", "OK");
            }
        }

        private void RefreshSceneList()
        {
            sceneListContainer.Clear();

            if (sceneCollection == null) return;
        
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        
            for (int i = 0; i < sceneCollection.scenes.Count; i++)
            {
                var sceneData = sceneCollection.scenes[i];
                var sceneEntry = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            
                bool isActiveScene = sceneData.scenePath == activeScene.path;
            
            
                // Label for scene name
                var sceneLabel = new Label(sceneData.sceneName) { style = { flexGrow = 1 } };

                if (sceneData.isHeader)
                {
                    sceneEntry.AddToClassList("section");
                    sceneLabel.AddToClassList("headerText");
                    sceneEntry.style.flexDirection = FlexDirection.Row;
                    sceneEntry.style.alignItems = Align.Center;

                    sceneLabel.style.flexGrow = 1; 
                    sceneLabel.style.unityTextAlign = TextAnchor.MiddleCenter; 

                    sceneLabel.style.color = sceneData.savedColor != default ? sceneData.savedColor : Color.white;

                    var colorField = CreateColorField(sceneData, sceneLabel);
                    var removeButton = CreateRemoveButton(sceneData);
                    var moveUpButton = CreateMoveUpButton(i);

                    sceneEntry.Add(sceneLabel);
                    sceneEntry.Add(colorField);
                    sceneEntry.Add(removeButton);
                    sceneEntry.Add(moveUpButton);

                    colorField.style.display = showOptions ? DisplayStyle.Flex : DisplayStyle.None;
                    removeButton.style.display = showOptions ? DisplayStyle.Flex : DisplayStyle.None;
                    moveUpButton.style.display = showOptions ? DisplayStyle.Flex : DisplayStyle.None;
                }
                else
                {

                    var sceneButton = new Button(() => { LoadScene(sceneData.scenePath); })
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row, 
                            flexGrow = 1, 
                            justifyContent = Justify.Center
                        }
                    };

                    sceneButton.AddToClassList("sceneButton");

                    if (isActiveScene)
                    {
                        sceneEntry.AddToClassList("selectedSection");
                        sceneLabel.AddToClassList("textSelected");
                    }
                    else
                    {
                        sceneEntry.AddToClassList("section");
                        sceneLabel.AddToClassList("text");
                    }
                    
                    // Assign the saved color or default to white
                    sceneLabel.style.color = sceneData.savedColor != default ? sceneData.savedColor : Color.white;

                    var colorField = CreateColorField(sceneData, sceneLabel);
                    sceneEntry.Add(colorField);
            
                    var removeButton = CreateRemoveButton(sceneData);
            
                    var moveUpButton = CreateMoveUpButton(i);

                    // Add elements to scene entry
                    sceneEntry.Add(sceneButton);
                    sceneButton.Add(sceneLabel);
                    sceneButton.Add(colorField);
                    sceneButton.Add(removeButton);
                    sceneButton.Add(moveUpButton);

                    // Set initial visibility of options
                    colorField.style.display = showOptions ? DisplayStyle.Flex : DisplayStyle.None;
                    removeButton.style.display = showOptions ? DisplayStyle.Flex : DisplayStyle.None;
                    moveUpButton.style.display = showOptions ? DisplayStyle.Flex : DisplayStyle.None;
                }

                sceneListContainer.Add(sceneEntry);
            }
        }
    
        private ColorField CreateColorField(SceneData sceneData, Label sceneLabel)
        {
            var colorField = new ColorField
            {
                value = sceneLabel.style.color.value, // Use the current color of the label
                label = "",
                viewDataKey = sceneData.sceneName + "Color"
            };
            colorField.AddToClassList("colorField");

            // Save color whenever it changes
            colorField.RegisterValueChangedCallback(evt =>
            {
                sceneLabel.style.color = evt.newValue;
                sceneData.savedColor = evt.newValue; // Save the color in SceneData
            });

            return colorField;
        }

        private Button CreateLoadButton(SceneData sceneData)
        {
            var loadButton = new Button(() => LoadScene(sceneData.scenePath)) { text = "Load" };
            loadButton.AddToClassList("loadButton");
            return loadButton;
        }

        private Button CreateRemoveButton(SceneData sceneData)
        {
            var removeButton = new Button(() => RemoveScene(sceneData)) { text = "X" };
            removeButton.AddToClassList("removeButton");
            return removeButton;
        }

        private Button CreateMoveUpButton(int index)
        {
            var moveUpButton = new Button(() => MoveSceneUp(index)) { text = "↑" };
            moveUpButton.AddToClassList("moveUpButton");
            return moveUpButton;
        }

        private void ToggleOptionsVisibility()
        {
            showOptions = !showOptions;

            // Ensure sceneCollection and sceneListContainer are defined
            if (sceneCollection == null || sceneListContainer == null) return;

            for (int i = 0; i < sceneListContainer.childCount; i++)
            {
                var element = sceneListContainer[i];
                var sceneData = sceneCollection.scenes[i]; // Get corresponding sceneData

                var colorField = element.Q<ColorField>();
                var removeButton = element.Q<Button>(className: "removeButton");
                var moveUpButton = element.Q<Button>(className: "moveUpButton");

                colorField.style.display = showOptions ? DisplayStyle.Flex : DisplayStyle.None;
                removeButton.style.display = showOptions ? DisplayStyle.Flex : DisplayStyle.None;
                moveUpButton.style.display = showOptions ? DisplayStyle.Flex : DisplayStyle.None;

                // Restore label color based on saved color in sceneData
                var sceneLabel = element.Q<Label>();
                if (sceneLabel != null)
                {
                    sceneLabel.style.color = sceneData.savedColor != default ? sceneData.savedColor : Color.white;
                }
            }
        }
    
        private void ToggleHeaderCreationVisibility()
        {
            if (headerCreation.resolvedStyle.display == DisplayStyle.None)
            {
                headerCreation.style.display = DisplayStyle.Flex;
            }
            else
            {
                headerCreation.style.display = DisplayStyle.None;
            }
        }

        private void HideHeaderCreation()
        {
            headerCreation.style.display = DisplayStyle.None;
        }

        private void LoadScene(string scenePath)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

                void OnPlayModeStateChanged(PlayModeStateChange state)
                {
                    if (state == PlayModeStateChange.EnteredEditMode)
                    {
                        if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                            RefreshSceneList();
                        }
                        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                    }
                }
            }
            else
            {
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }
            }
            RefreshSceneList();
        }

        private void RemoveScene(SceneData sceneData)
        {
            if (sceneCollection.scenes.Contains(sceneData))
            {
                sceneCollection.scenes.Remove(sceneData);
                RefreshSceneList();
            }
        }

        private void MoveSceneUp(int index)
        {
            if (index <= 0 || index >= sceneCollection.scenes.Count) return;

            // Swap the current scene with the previous one in the list
            var temp = sceneCollection.scenes[index];
            sceneCollection.scenes[index] = sceneCollection.scenes[index - 1];
            sceneCollection.scenes[index - 1] = temp;

            // Mark collection as dirty to allow saving changes
            EditorUtility.SetDirty(sceneCollection);
            RefreshSceneList();
        }
    
        private void OnDragEnter(DragEnterEvent evt)
        {
            evt.StopImmediatePropagation();
        }

        private void OnDragLeave(DragLeaveEvent evt)
        {
            evt.StopImmediatePropagation();
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            // Verifica si se está arrastrando un objeto de escena
            if (DragAndDrop.objectReferences.Length > 0)
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is SceneAsset)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy; // Indica que se puede soltar
                        break;
                    }
                }
            }
            evt.StopImmediatePropagation();
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            // Procesa los objetos arrastrados y añade la escena a la colección
            DragAndDrop.AcceptDrag();

            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is SceneAsset sceneAsset)
                {
                    AddSceneToCollection(sceneAsset);
                }
            }
            evt.StopImmediatePropagation();
        }
    
    }
}