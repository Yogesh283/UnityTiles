using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*Component for quickly attaching data to a game object.*/
// 18.01.2024
namespace Mkey
{
    public class DataComponent : MonoBehaviour
    {
        public string key;
        public string textValue;
        public int intValue;
        public float floatValue;
        public decimal decimalValue;

        public static DataComponent FindFirstByKey(bool includeInactive, string key)
        {
            DataComponent[] dataComponents = FindObjectsByType<DataComponent>(includeInactive? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (string.IsNullOrEmpty(key))
            {
                for (int i = 0; i < dataComponents.Length; i++)
                {
                    if (string.IsNullOrEmpty(dataComponents[i].key)) return dataComponents[i];
                }
            }
            else
            {
                for (int i = 0; i < dataComponents.Length; i++)
                {
                    if (key.CompareTo(dataComponents[i].key) == 0) return dataComponents[i];
                }
            }
            return null;
        }

        public static List<DataComponent> FindAllByKey(bool includeInactive, string key)
        {
            List<DataComponent> dataComponents = new List<DataComponent>(FindObjectsByType<DataComponent>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            if (string.IsNullOrEmpty(key))
            {
                dataComponents.RemoveAll((dc) => { return string.IsNullOrEmpty(dc.key); });
            }
            else
            {
                dataComponents.RemoveAll((dc) => { return key.CompareTo(dc.key) != 0; });
            }
            return dataComponents;
        }

        public static DataComponent FindFirstByTextValue(bool includeInactive, string textValue)
        {
            DataComponent[] dataComponents = FindObjectsByType<DataComponent>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (string.IsNullOrEmpty(textValue))
            {
                for (int i = 0; i < dataComponents.Length; i++)
                {
                    if (string.IsNullOrEmpty(dataComponents[i].textValue)) return dataComponents[i];
                }
            }
            else
            {
                for (int i = 0; i < dataComponents.Length; i++)
                {
                    if (dataComponents[i].textValue.CompareTo(textValue) == 0) return dataComponents[i];
                }
            }
            return null;
        }

        public static List<DataComponent> FindAllByTextValue(bool includeInactive, string textValue)
        {
            List<DataComponent> dataComponents = new List<DataComponent>(FindObjectsByType<DataComponent>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            if (string.IsNullOrEmpty(textValue))
            {
                dataComponents.RemoveAll((dc) => { return string.IsNullOrEmpty(dc.textValue); });
            }
            else
            {
                dataComponents.RemoveAll((dc) => { return textValue.CompareTo(dc.textValue) != 0; });
            }
            return dataComponents;
        }
    }
}