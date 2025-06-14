using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
#if UNITY_XR_ENABLED
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
#endif

/// <summary>
/// Controlador de efectos de fade optimizado para aplicaciones VR
/// </summary>
public class VRFadeController : MonoBehaviour
{
    [Header("Material References")]
    [Tooltip("Objeto que contiene los materiales para el efecto de fade (ej. esfera o plano alrededor de la cámara)")]
    public GameObject fadeObject;

    [Tooltip("Renderers adicionales que también deben aplicar el fade")]
    public List<Renderer> additionalRenderers = new List<Renderer>();

    [Tooltip("Nombre de la propiedad del shader para controlar la transparencia (por defecto: _Alpha)")]
    public string alphaPropertyName = "_Alpha";

    [Header("Fade Configuration")]
    [Tooltip("Curva de animación para el efecto de fade")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Duración por defecto del efecto de fade")]
    public float duration = 1f;

    [Tooltip("Realizar fade a transparente al iniciar")]
    public bool fadeOutOnStart = true;

    [Tooltip("Color del fade (negro por defecto)")]
    public Color fadeColor = Color.black;

    [Header("Scene Management")]
    [Tooltip("Escena por defecto a cargar cuando se usa LoadDefaultScene")]
    public string defaultSceneToLoad = "";

    [Tooltip("Si se debe usar fade al cargar escenas por defecto")]
    public bool useFadeForSceneLoading = true;

    [Header("Advanced Options")]
    [Tooltip("Desactivar movimiento del jugador durante el fade")]
    public bool disablePlayerMovementDuringFade = true;

    [Tooltip("Desactivar teletransporte durante el fade")]
    public bool disableTeleportDuringFade = true;

    [Header("Events")]
    [Tooltip("Evento disparado cuando comienza el fade a negro")]
    public UnityEvent onFadeInBegin;
    
    [Tooltip("Evento disparado cuando se completa el fade a negro")]
    public UnityEvent onFadeInComplete;

    [Tooltip("Evento disparado cuando comienza el fade a transparente")]
    public UnityEvent onFadeOutBegin;
    
    [Tooltip("Evento disparado cuando se completa el fade a transparente")]
    public UnityEvent onFadeOutComplete;

    // Lista de todos los materiales a controlar
    private List<Material> targetMaterials = new List<Material>();
    private Coroutine fadeCoroutine;
    private bool isFadingIn = false;
    
    // Variables para controlar el estado del fade
    private bool isFading = false;
    private bool isLoadingScene = false;

    // Referencias a componentes XR para desactivar durante el fade
    #if UNITY_XR_ENABLED
    private DynamicMoveProvider moveProvider;
    private TeleportationProvider teleportProvider;
    #endif

    /// <summary>
    /// Indica si hay un fade actualmente en progreso
    /// </summary>
    public bool IsFading => isFading;
    
    /// <summary>
    /// Indica si se está cargando una escena en este momento
    /// </summary>
    public bool IsLoadingScene => isLoadingScene;

    private void Awake()
    {
        // Encontrar componentes XR para control de interacción
        #if UNITY_XR_ENABLED
        moveProvider = FindFirstObjectByType<DynamicMoveProvider>();
        teleportProvider = FindFirstObjectByType<TeleportationProvider>();
        #else
        Debug.LogWarning("Los paquetes XR no están instalados. VRFadeController requiere XR para funcionar correctamente.", this);
        #endif

        // Inicializar materiales
        InitializeMaterials();
    }

    private void Start()
    {
        // Inicializar estado inicial
        if (fadeOutOnStart)
        {
            SetAlpha(1f);
            FadeOut();
        }
        else
        {
            SetAlpha(0f);
        }
    }

    /// <summary>
    /// Inicializa todos los materiales que se usarán para el efecto de fade
    /// </summary>
    private void InitializeMaterials()
    {
        targetMaterials.Clear();

        // Añadir material del objeto de fade principal
        if (fadeObject != null)
        {
            // Primero comprobar si el fadeObject tiene un renderer
            Renderer mainRenderer = fadeObject.GetComponent<Renderer>();
            if (mainRenderer != null)
            {
                ProcessRenderer(mainRenderer);
            }

            // Luego comprobar renderers de hijos
            Renderer[] childRenderers = fadeObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in childRenderers)
            {
                // Saltar si es el mismo que el renderer principal que ya procesamos
                if (renderer != mainRenderer)
                {
                    ProcessRenderer(renderer);
                }
            }
        }
        else
        {
            Debug.LogWarning("No se ha asignado un objeto de fade. VRFadeController no funcionará correctamente.", this);
        }

        // Añadir materiales adicionales
        foreach (Renderer renderer in additionalRenderers)
        {
            if (renderer != null)
            {
                ProcessRenderer(renderer);
            }
        }

        // Configurar color y estado inicial para todos los materiales
        foreach (Material material in targetMaterials)
        {
            // Si el material usa canal alpha estándar
            if (material.HasProperty("_Color"))
            {
                Color color = material.color;
                color = fadeColor;
                color.a = 0f;
                material.color = color;
            }

            // Si usa una propiedad personalizada para alpha
            if (material.HasProperty(alphaPropertyName))
            {
                material.SetFloat(alphaPropertyName, 0f);
            }
        }

        Debug.Log($"VRFadeController: Inicializados {targetMaterials.Count} materiales para el efecto de fade");
    }

    /// <summary>
    /// Procesa un renderer para extraer y clonar sus materiales
    /// </summary>
    private void ProcessRenderer(Renderer renderer)
    {
        // Clonar materiales para evitar modificar assets originales del proyecto
        foreach (Material material in renderer.materials)
        {
            Material materialInstance = new Material(material);
            materialInstance.color = fadeColor;
            targetMaterials.Add(materialInstance);
        }

        // Aplicar materiales clonados de vuelta al renderer
        Material[] newMaterials = new Material[renderer.materials.Length];
        for (int i = 0; i < renderer.materials.Length; i++)
        {
            newMaterials[i] = targetMaterials[targetMaterials.Count - renderer.materials.Length + i];
        }
        renderer.materials = newMaterials;
    }

    /// <summary>
    /// Establece manualmente el valor alpha en todos los materiales sin animación
    /// </summary>
    public void SetAlpha(float alpha)
    {
        foreach (Material material in targetMaterials)
        {
            // Si el material usa canal alpha estándar
            if (material.HasProperty("_Color"))
            {
                Color color = material.color;
                color.a = alpha;
                material.color = color;
            }

            // Si usa una propiedad personalizada para alpha
            if (material.HasProperty(alphaPropertyName))
            {
                material.SetFloat(alphaPropertyName, alpha);
            }
        }
    }

    // ================ VERSIONES COMPATIBLES CON EVENTOS DE UNITY (void) ================

    /// <summary>
    /// Iniciar fade a negro/opaco - Para usar con eventos de Unity
    /// </summary>
    public void FadeIn()
    {
        FadeIn(duration);
    }

    /// <summary>
    /// Iniciar fade a negro/opaco con duración personalizada - Para usar con eventos de Unity
    /// </summary>
    public void FadeIn(float customDuration)
    {
        if (isFading)
        {
            Debug.Log("Ya hay un fade en progreso. Ignorando nueva solicitud de fade.");
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeWithCustomDuration(0, 1, customDuration));
    }

    public void FadeInWithDefaultSceneLoad(float customDuration = -1f)
    {
        if (isFading || isLoadingScene)
        {
            Debug.Log("Ya hay un fade o carga de escena en progreso. Ignorando nueva solicitud.");
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        isLoadingScene = true;
        float fadeDuration = customDuration < 0 ? duration : customDuration;
        fadeCoroutine = StartCoroutine(FadeInAndLoadScene(defaultSceneToLoad, fadeDuration));
        return;
    }

    /// <summary>
    /// Iniciar fade a transparente - Para usar con eventos de Unity
    /// </summary>
    public void FadeOut()
    {
        FadeOut(duration);
    }

    /// <summary>
    /// Iniciar fade a transparente con duración personalizada - Para usar con eventos de Unity
    /// </summary>
    public void FadeOut(float customDuration)
    {
        if (isFading)
        {
            Debug.Log("Ya hay un fade en progreso. Ignorando nueva solicitud de fade.");
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeWithCustomDuration(1, 0, customDuration));
    }

    // ================ VERSIONES PROGRAMÁTICAS (bool) ================

    /// <summary>
    /// Iniciar fade a negro/opaco con duración personalizada opcional (versión programática)
    /// </summary>
    /// <param name="customDuration">Duración personalizada opcional (si se omite, usa la duración por defecto)</param>
    /// <returns>True si se inició el fade, false si otro fade está en progreso</returns>
    public bool TryFadeIn(float customDuration = -1f)
    {
        if (isFading)
        {
            Debug.Log("Ya hay un fade en progreso. Ignorando nueva solicitud de fade.");
            return false;
        }

        float useDuration = customDuration < 0 ? duration : customDuration;
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeWithCustomDuration(0, 1, useDuration));
        return true;
    }

    /// <summary>
    /// Iniciar fade a transparente con duración personalizada opcional (versión programática)
    /// </summary>
    /// <param name="customDuration">Duración personalizada opcional (si se omite, usa la duración por defecto)</param>
    /// <returns>True si se inició el fade, false si otro fade está en progreso</returns>
    public bool TryFadeOut(float customDuration = -1f)
    {
        if (isFading)
        {
            Debug.Log("Ya hay un fade en progreso. Ignorando nueva solicitud de fade.");
            return false;
        }
        
        float useDuration = customDuration < 0 ? duration : customDuration;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeWithCustomDuration(1, 0, useDuration));
        return true;
    }

    /// <summary>
    /// Realizar un fade completo con duración personalizada (versión programática)
    /// </summary>
    /// <returns>True si se inició el fade, false si otro fade está en progreso</returns>
    public bool TryFadeWithDuration(float fromAlpha, float toAlpha, float customDuration)
    {
        if (isFading)
        {
            Debug.Log("Ya hay un fade en progreso. Ignorando nueva solicitud de fade.");
            return false;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeWithCustomDuration(fromAlpha, toAlpha, customDuration));
        return true;
    }

    /// <summary>
    /// Iniciar fade a negro y luego cargar una escena (versión programática)
    /// </summary>
    /// <param name="sceneName">Nombre de la escena a cargar</param>
    /// <param name="customDuration">Duración personalizada opcional (si es -1, usa la duración por defecto)</param>
    /// <returns>True si se inició el fade, false si otro fade está en progreso</returns>
    public bool TryFadeInWithSceneLoad(string sceneName, float customDuration = -1f)
    {
        if (isFading || isLoadingScene)
        {
            Debug.Log("Ya hay un fade o carga de escena en progreso. Ignorando nueva solicitud.");
            return false;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        isLoadingScene = true;
        float fadeDuration = customDuration < 0 ? duration : customDuration;
        fadeCoroutine = StartCoroutine(FadeInAndLoadScene(sceneName, fadeDuration));
        return true;
    }

    /// <summary>
    /// Realizar un fade en ambas direcciones con una acción en medio (versión programática)
    /// </summary>
    /// <param name="actionInBetween">Acción a ejecutar entre fades</param>
    /// <param name="fadeInDuration">Duración del fade a negro (-1 para por defecto)</param>
    /// <param name="fadeOutDuration">Duración del fade a transparente (-1 para por defecto)</param>
    /// <returns>True si se inició el fade, false si otro fade está en progreso</returns>
    public bool TryFadeInOutWithAction(System.Action actionInBetween, float fadeInDuration = -1f, float fadeOutDuration = -1f)
    {
        if (isFading)
        {
            Debug.Log("Ya hay un fade en progreso. Ignorando nueva solicitud de fade.");
            return false;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        float fadeInTime = fadeInDuration < 0 ? duration : fadeInDuration;
        float fadeOutTime = fadeOutDuration < 0 ? duration : fadeOutDuration;
        fadeCoroutine = StartCoroutine(FadeInOutSequence(actionInBetween, fadeInTime, fadeOutTime));
        return true;
    }

    // Implementación principal del fade con duración personalizada
    private IEnumerator FadeWithCustomDuration(float startAlpha, float endAlpha, float customDuration)
    {
        // Marcar que hay un fade en progreso
        isFading = true;
        
        // Determinar si es fadeIn o fadeOut
        isFadingIn = endAlpha > startAlpha;

        // Disparar eventos de inicio
        if (isFadingIn)
        {
            onFadeInBegin?.Invoke();
            
            // Desactivar controles VR durante el fade si está configurado
            SetPlayerMovementEnabled(!disablePlayerMovementDuringFade);
            SetTeleportEnabled(!disableTeleportDuringFade);
        }
        else
        {
            onFadeOutBegin?.Invoke();
        }

        float timeElapsed = 0f;

        while (timeElapsed < customDuration)
        {
            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / customDuration;
            float curveValue = fadeCurve.Evaluate(normalizedTime);
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);

            // Aplicar alpha a todos los materiales
            SetAlpha(currentAlpha);

            yield return null;
        }

        // Asegurar valor final exacto
        SetAlpha(endAlpha);

        // Restaurar controles VR si fue fadeOut
        if (!isFadingIn)
        {
            SetPlayerMovementEnabled(true);
            SetTeleportEnabled(true);
        }

        // Disparar eventos de finalización
        if (isFadingIn)
        {
            onFadeInComplete?.Invoke();
        }
        else
        {
            onFadeOutComplete?.Invoke();
        }
        
        // Marcar que el fade ha terminado
        isFading = false;
    }

    // Fade original (ahora llama a la versión de duración personalizada)
    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        yield return FadeWithCustomDuration(startAlpha, endAlpha, duration);
    }

    private IEnumerator FadeInAndLoadScene(string sceneName, float customDuration)
    {
        isFading = true;
        yield return StartCoroutine(FadeWithCustomDuration(0, 1, customDuration));
        
        // La escena será cargada y reiniciará este componente
        SceneManager.LoadScene(sceneName);
        
        // Estos flags se resetearán cuando se cargue la nueva escena
    }

    private IEnumerator FadeInOutSequence(System.Action actionInBetween, float fadeInDuration, float fadeOutDuration)
    {
        isFading = true;
        
        // Primero fade a negro
        yield return StartCoroutine(FadeWithCustomDuration(0, 1, fadeInDuration));

        // Ejecutar acción entre fades
        actionInBetween?.Invoke();

        // Pequeña pausa para estabilidad
        yield return new WaitForSeconds(0.1f);

        // Luego fade a transparente
        yield return StartCoroutine(FadeWithCustomDuration(1, 0, fadeOutDuration));
        
        isFading = false;
    }

    /// <summary>
    /// Activar/desactivar movimiento del jugador durante el fade
    /// </summary>
    private void SetPlayerMovementEnabled(bool enabled)
    {
        #if UNITY_XR_ENABLED
        if (moveProvider != null)
        {
            moveProvider.enabled = enabled;
        }
        #endif
    }

    /// <summary>
    /// Activar/desactivar teletransporte durante el fade
    /// </summary>
    private void SetTeleportEnabled(bool enabled)
    {
        #if UNITY_XR_ENABLED
        if (teleportProvider != null)
        {
            teleportProvider.enabled = enabled;
        }
        #endif
    }

    /// <summary>
    /// Salir de la aplicación con efecto de fade previo
    /// </summary>
    /// <param name="fadeDuration">Duración del fade (-1 para por defecto)</param>
    public void QuitApplicationWithFade(float fadeDuration = -1f)
    {
        if (isFading)
        {
            Debug.Log("Ya hay un fade en progreso. Ignorando solicitud de salida.");
            return;
        }
        
        float fadeTime = fadeDuration < 0 ? duration : fadeDuration;
        TryFadeInOutWithAction(() =>
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }, fadeTime, fadeTime);
    }

    /// <summary>
    /// Recargar la escena actual con efecto de fade (versión programática)
    /// </summary>
    /// <param name="customDuration">Duración personalizada opcional (si es -1, usa la duración por defecto)</param>
    /// <returns>True si se inició el fade, false si otro fade está en progreso</returns>
    public bool TryReloadCurrentSceneWithFade(float customDuration = -1f)
    {
        if (isFading || isLoadingScene)
        {
            Debug.Log("Ya hay un fade o carga de escena en progreso. Ignorando solicitud de recarga.");
            return false;
        }
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        float fadeDuration = customDuration < 0 ? duration : customDuration;
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        isLoadingScene = true;
        fadeCoroutine = StartCoroutine(FadeInAndLoadScene(currentSceneName, fadeDuration));
        return true;
    }
    
    /// <summary>
    /// Cargar la escena por defecto especificada en el Inspector con efecto de fade (versión programática)
    /// </summary>
    /// <param name="customDuration">Duración personalizada opcional (si es -1, usa la duración por defecto)</param>
    /// <returns>True si se inició el fade, false si otro fade está en progreso</returns>
    public bool TryLoadDefaultSceneWithFade(float customDuration = -1f)
    {
        if (string.IsNullOrEmpty(defaultSceneToLoad))
        {
            Debug.LogWarning("No se ha especificado una escena por defecto. Por favor, establece el campo defaultSceneToLoad.", this);
            return false;
        }
        
        float fadeDuration = customDuration < 0 ? duration : customDuration;
        return TryFadeInWithSceneLoad(defaultSceneToLoad, fadeDuration);
    }

    /// <summary>
    /// Cargar la siguiente escena en los ajustes de build con efecto de fade (versión programática)
    /// </summary>
    /// <param name="customDuration">Duración personalizada opcional (si es -1, usa la duración por defecto)</param>
    /// <param name="loopToFirst">Si es true, volverá a la primera escena cuando esté en la última</param>
    /// <returns>True si se inició el fade, false si otro fade está en progreso</returns>
    public bool TryLoadNextSceneWithFade(float customDuration = -1f, bool loopToFirst = true)
    {
        if (isFading || isLoadingScene)
        {
            Debug.Log("Ya hay un fade o carga de escena en progreso. Ignorando nueva solicitud de carga de escena.");
            return false;
        }
        
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        
        // Volver a la primera escena si es necesario
        if (nextSceneIndex >= sceneCount)
        {
            if (loopToFirst)
            {
                nextSceneIndex = 0;
            }
            else
            {
                Debug.LogWarning("Ya estás en la última escena de los ajustes de build.", this);
                return false;
            }
        }
        
        float fadeDuration = customDuration < 0 ? duration : customDuration;
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        isLoadingScene = true;
        fadeCoroutine = StartCoroutine(FadeInAndLoadSceneByIndex(nextSceneIndex, fadeDuration));
        return true;
    }
    
    /// <summary>
    /// Cargar la escena anterior en los ajustes de build con efecto de fade (versión programática)
    /// </summary>
    /// <param name="customDuration">Duración personalizada opcional (si es -1, usa la duración por defecto)</param>
    /// <param name="loopToLast">Si es true, volverá a la última escena cuando esté en la primera</param>
    /// <returns>True si se inició el fade, false si otro fade está en progreso</returns>
    public bool TryLoadPreviousSceneWithFade(float customDuration = -1f, bool loopToLast = true)
    {
        if (isFading || isLoadingScene)
        {
            Debug.Log("Ya hay un fade o carga de escena en progreso. Ignorando nueva solicitud de carga de escena.");
            return false;
        }
        
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int previousSceneIndex = currentSceneIndex - 1;
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        
        // Volver a la última escena si es necesario
        if (previousSceneIndex < 0)
        {
            if (loopToLast)
            {
                previousSceneIndex = sceneCount - 1;
            }
            else
            {
                Debug.LogWarning("Ya estás en la primera escena de los ajustes de build.", this);
                return false;
            }
        }
        
        float fadeDuration = customDuration < 0 ? duration : customDuration;
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        isLoadingScene = true;
        fadeCoroutine = StartCoroutine(FadeInAndLoadSceneByIndex(previousSceneIndex, fadeDuration));
        return true;
    }
    
    /// <summary>
    /// Cargar una escena por índice con efecto de fade
    /// </summary>
    private IEnumerator FadeInAndLoadSceneByIndex(int sceneIndex, float customDuration)
    {
        isFading = true;
        yield return StartCoroutine(FadeWithCustomDuration(0, 1, customDuration));
        SceneManager.LoadScene(sceneIndex);
    }
    
    /// <summary>
    /// Cargar una escena directamente sin fade (método de utilidad)
    /// </summary>
    public void LoadSceneWithoutFade(string sceneName)
    {
        if (isLoadingScene)
        {
            Debug.Log("Ya hay una carga de escena en progreso. Ignorando nueva solicitud de carga de escena.");
            return;
        }
        
        SceneManager.LoadScene(sceneName);
    }

    private void OnDestroy()
    {
        // Limpiar materiales instanciados para evitar fugas de memoria
        foreach (Material material in targetMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
