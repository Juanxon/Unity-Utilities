using System.Collections.Generic;
using UnityEngine;

namespace Juanxon.EditorSceneManager
{
    [CreateAssetMenu(fileName = "SceneCollection", menuName = "Scene Manager/Scene Collection")]
    public class SceneCollection : ScriptableObject
    {
        public List<SceneData> scenes = new List<SceneData>();
    }

    [System.Serializable]
    public class SceneData
    {
        public string sceneName;
        public string scenePath;
        public Color savedColor;
        public bool isHeader;
    }
}