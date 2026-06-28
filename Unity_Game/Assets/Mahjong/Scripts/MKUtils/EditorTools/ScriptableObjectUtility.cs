using System.IO;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/*
  27.10.2020
  23.02.2021 - add SingletonScriptableObject<T> , LoadResourceAssets
 */
namespace Mkey
{
    public static class ScriptableObjectUtility //http://wiki.unity3d.com/index.php?title=CreateScriptableObjectAsset
    {
#if UNITY_EDITOR
        /// <summary>
        //	This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        public static T CreateAsset<T>(string subFolder, string namePrefix, string nameSuffics) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/Resources/" + subFolder + "/" + namePrefix + typeof(T).ToString().Replace('.', '_') + nameSuffics + ".asset");
            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            T aT = (T)AssetDatabase.LoadAssetAtPath(assetPathAndName, typeof(T));
            Selection.activeObject = aT;
            return aT;
        }

        /// <summary>
        //	This makes it easy to create, name and place unique new ScriptableObject asset files in Resource/Subfolder .
        //  string path like "Assets/ForestMatch/Resources/";
        /// </summary>
        public static T CreateResourceAsset<T>(string path, string subFolder, string namePrefix, string nameSuffics) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + subFolder + "/" + namePrefix + typeof(T).ToString().Replace('.', '_') + nameSuffics + ".asset");
            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            T aT = (T)AssetDatabase.LoadAssetAtPath(assetPathAndName, typeof(T));
            Selection.activeObject = aT;
            return aT;
        }

        /// <summary>
        //	This makes it easy to create, name and place unique new ScriptableObject asset files in Resource/Subfolder .
        //  string path like "Assets/ForestMatch/Resources/";
        /// </summary>
        public static T CreateResourceAsset<T>(string path, string subFolder, string name, string namePrefix, string nameSuffics) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + subFolder + "/" + namePrefix + name + nameSuffics + ".asset");
            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            T aT = (T)AssetDatabase.LoadAssetAtPath(assetPathAndName, typeof(T));
            Selection.activeObject = aT;
            return aT;
        }

        /// <summary>
        //	This makes it easy to create, name and place unique new ScriptableObject asset files in Resource/Subfolder .
        /// </summary>
        public static void DeleteResourceAsset(UnityEngine.Object o)
        {
            string path = AssetDatabase.GetAssetPath(o);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            EditorUtility.FocusProjectWindow();
        }

        /// <summary>
        /// Load all scriptable objects from resource folder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subFolder"></param>
        /// <returns></returns>

#endif

        public static T[] LoadResourceAssets<T>(string subFolder) where T : BaseScriptable
        {
            T[] t = Resources.LoadAll<T>(subFolder);
            if (t != null && t.Length > 0)
            {
                string s = "";
                foreach (var m in t)
                {
                    s += m.ToString() + "; ";
                }
                Debug.Log("Scriptable assets loaded," + typeof(T).ToString() + ", count: " + t.Length + "; sets : " + s);
            }
            else
            {
                Debug.Log("Scriptable assets " + typeof(T).ToString() + " not found!!!");
            }
            return t;
        }
    }

    /// <summary>
    /// Abstract class for making reload-proof singletons out of ScriptableObjects
    /// Returns the asset created on the editor, or null if there is none
    /// Based on https://www.youtube.com/watch?v=VBA1QCoEAX4
    /// https://baraujo.net/unity3d-making-singletons-from-scriptableobjects-automatically/
    /// </summary>
    /// <typeparam name="T">Singleton type</typeparam>
    public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
    {
        static T _instance = null;
        public static T Instance
        {
            get
            {
                if (!_instance)
                    _instance = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
                return _instance;
            }
        }
    }
}