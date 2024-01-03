using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;
using System.Text.RegularExpressions;

namespace OpenBehaviorTrees { 
[Serializable]
public class Blackboard : Dictionary<string, object>, ISerializationCallbackReceiver
{
    [Serializable]
    //struct represents the string key, and the serialized value from the dictionary.
    private struct SaveItem
    {
        public string key;
        public string value;
        public int index;

        public SaveItem(string key, string val, int index)
        {
            this.key = key;
            this.value = val;
            this.index = index;
        }
    }

    //All serialized items except for objects in a scene, which have to be handled separately.
    [SerializeField, HideInInspector]
    private List<SaveItem> saveItems;

    //We need a different struct and list for Object references in scene :(
    [Serializable]
    private struct NonAssetSaveItem
    {
        public string key;
        public UnityEngine.Object obj;
        public int index;

        public NonAssetSaveItem(string key, UnityEngine.Object obj, int index)
        {
            this.key = key;
            this.obj = obj;
            this.index = index;
        }
    }
    [SerializeField, HideInInspector]
    private List<NonAssetSaveItem> sceneObjectSaveItems;

    public Blackboard(){}
    public Blackboard(Blackboard original)
    {
        if(original != null)
        {
            List<string> originalKeys = original.Keys.ToList();
            for (int i = 0; i < original.Count; i++)
            {
                Add(originalKeys[i], original[originalKeys[i]]);
            }
        }
    }

    /// <summary>
    /// Takes all of the keyvalue pairs from the Dictionary and stores them as Serializable lists.
    /// </summary>
    public void OnBeforeSerialize()
    {
        sceneObjectSaveItems = new List<NonAssetSaveItem>();
        saveItems = new List<SaveItem>();
        List<string> keys = this.Keys.ToList();
        List<object> values = this.Values.ToList();

        for (int i = 0; i < Count; i++)
        {
            object value = values[i];
            string encode = "";

            //Unhandled Enum Types
            if (value is Enum)
            {
                Enum enumValue = (Enum)value;
                Console.WriteLine("Enum Value: " + enumValue.ToString());
                encode = $"({value.GetType().AssemblyQualifiedName}){enumValue}";
                saveItems.Add(new SaveItem(keys[i], encode, i));
                continue;
            }

            switch (value)
            {
                case null: encode = "(null)"; break;
                case int: encode = "(int)" + ((int)value).ToString("F9"); break;
                case float: encode = "(float)" + ((float)value).ToString("F9"); break;
                case double: encode = "(double)" + ((double)value).ToString("F9"); break;
                case long: encode = "(long)" + ((long)value).ToString(); break;
                case string: encode = "(string)" + (string)value; break;
                case bool: encode = "(bool)" + (((bool)value) == true ? "true" : "false"); break;
                case Vector2Int: encode = "(Vector2Int)" + ((Vector2Int)value).ToString(); break;
                case Vector3Int: encode = "(Vector3Int)" + ((Vector3Int)value).ToString(); break;
                case Vector2: encode = "(Vector2)" + ((Vector2)value).ToString(); break;
                case Vector3: encode = "(Vector3)" + ((Vector3)value).ToString(); break;
                case Vector4: encode = "(Vector4)" + ((Vector4)value).ToString(); break;
                case Bounds: encode = "(Bounds)" + ((Bounds)value).ToString(); break;
                case Rect: encode = "(Rect)" + ((Rect)value).ToString("F9"); break;
                case Color: encode = "(Color)" + JsonUtility.ToJson((Color)value); break;
                case AnimationCurve: encode = "(AnimationCurve)" + Serializer.SerializeAnimationCurve((AnimationCurve)value); break;
                case Gradient: encode = "(Gradient)" + Serializer.SerializeGradient((Gradient)value); break;
                case UnityEngine.Object obj:
                    string assetPath = Application.isEditor ? AssetDatabase.GetAssetPath(obj) : null;
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        encode = "(UnityEngine.Object)" + assetPath;
                    }
                    else
                    {
                        sceneObjectSaveItems.Add(new NonAssetSaveItem(keys[i], obj, i));
                    }
                    break;
                //Try to serialize to JSON. May be empty if type is not supported
                default: encode = $"({value.GetType().AssemblyQualifiedName}){JsonUtility.ToJson(value)}"; break;
            }

            if (!string.IsNullOrEmpty(encode))
            {
                saveItems.Add(new SaveItem(keys[i], encode, i));
            }
        }
    }

    /// <summary>
    /// Loads the two lists back into the Dictionary, using the Merge Linked Lists method.
    /// </summary>
    public void OnAfterDeserialize()
    {
        this.Clear();
        int i = 0;
        int j = 0;

        //Ensure that the lists are not null to ensure no errors when accessing list.Count
        saveItems = saveItems == null ? new List<SaveItem>() : saveItems;
        sceneObjectSaveItems = sceneObjectSaveItems == null ? new List<NonAssetSaveItem>() : sceneObjectSaveItems;

        while (i < saveItems.Count && j < sceneObjectSaveItems.Count)
        {
            if (saveItems[i].index < sceneObjectSaveItems[j].index)
            {
                string key = saveItems[i].key;
                int openIndex = saveItems[i].value.IndexOf('(');
                int closeIndex = saveItems[i].value.IndexOf(')');
                string contentType = saveItems[i].value.Substring(openIndex + 1, closeIndex - openIndex - 1);
                string encode = saveItems[i].value.Substring(closeIndex + 1);
                DeserializeItem(contentType, key, encode);
                i++;
            }
            else
            {
                Add(sceneObjectSaveItems[j].key, sceneObjectSaveItems[j].obj);
                j++;
            }
        }

        for(; i < saveItems.Count;i++)
        {
            string key = saveItems[i].key;
            int openIndex = saveItems[i].value.IndexOf('(');
            int closeIndex = saveItems[i].value.IndexOf(')');
            string contentType = saveItems[i].value.Substring(openIndex + 1, closeIndex - openIndex - 1);
            string encode = saveItems[i].value.Substring(closeIndex + 1);
            DeserializeItem(contentType, key, encode);
        }

        for (; j < sceneObjectSaveItems.Count; j++)
        {
            Add(sceneObjectSaveItems[j].key, sceneObjectSaveItems[j].obj);
        }
    }

    /// <summary>
    /// Takes the key and encoded string from a serialized item and adds it back into the dictionary.
    /// </summary>
    private void DeserializeItem(string contentType, string key, string encodedValue)
    {
        switch (contentType)
        {
            case "null": Add(key, null); return;
            case "int": Add(key, (int)int.Parse(encodedValue)); return;
            case "float": Add(key, (float)float.Parse(encodedValue)); return;
            case "double": Add(key, (double)double.Parse(encodedValue)); return;
            case "long": Add(key, (long)long.Parse(encodedValue)); return;
            case "string": Add(key, (string)encodedValue); return;
            case "bool": Add(key, (bool)(encodedValue == "true" ? true : false)); return;
            case "Vector2": Add(key, Serializer.ParseVector2(encodedValue)); return;
            case "Vector3": Add(key, Serializer.ParseVector3(encodedValue)); return;
            case "Vector2Int": Add(key, Serializer.ParseVector2Int(encodedValue)); return;
            case "Vector3Int": Add(key, Serializer.ParseVector3Int(encodedValue)); return;
            case "Vector4": Add(key, Serializer.ParseVector4(encodedValue)); return;
            case "Bounds": Add(key, Serializer.ParseBounds(encodedValue)); return;
            case "Rect": Add(key, Serializer.ParseRect(encodedValue)); return;
            case "Color": Add(key, JsonUtility.FromJson<Color>(encodedValue)); return;
            case "AnimationCurve": Add(key, Serializer.DeserializeAnimationCurve(encodedValue)); return;
            case "Gradient": Add(key, Serializer.DeserializeGradient(encodedValue)); return;
            case "UnityEngine.Object":
                if(Application.isEditor)
                {
                    EditorApplication.delayCall += () => Add(key, AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(encodedValue));
                }
                else
                {
                    Add(key, Resources.Load(encodedValue));
                }        
                return;
            default: break;
        }

        //Different process for enums (of any type)
        if(Serializer.EnumDeserialize(contentType, encodedValue, out object enumValue))
        {
            Add(key, enumValue);
        }

        //Tries to de-serialize a struct or class using JsonUtility.FromJson
        if(Serializer.TryDeserializeJSON(contentType, encodedValue, out object result))
        {
            Add(key, result);
        }
    }

    public void SetOrAdd(string key, object ob)
    {
        if (this.ContainsKey(key))
        {
            this[key] = ob;
        }
        else
        {
            this.Add(key, ob);
        }
    }

    [System.Serializable]
    private static class Serializer
    {
#region GradientSerialization
        [System.Serializable]
        private class SerializableGradient
        {
            public SerializableColorKey[] colorKeys;
            public SerializableAlphaKey[] alphaKeys;
            public GradientMode mode;

            public SerializableGradient(Gradient gradient)
            {
                colorKeys = new SerializableColorKey[gradient.colorKeys.Length];
                for (int i = 0; i < gradient.colorKeys.Length; i++)
                {
                    colorKeys[i] = new SerializableColorKey(gradient.colorKeys[i]);
                }

                alphaKeys = new SerializableAlphaKey[gradient.alphaKeys.Length];
                for (int i = 0; i < gradient.alphaKeys.Length; i++)
                {
                    alphaKeys[i] = new SerializableAlphaKey(gradient.alphaKeys[i]);
                }

                mode = gradient.mode;
            }

            public Gradient ToGradient()
            {
                Gradient gradient = new Gradient();
                GradientColorKey[] gradientColorKeys = new GradientColorKey[colorKeys.Length];
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    gradientColorKeys[i] = colorKeys[i].ToGradientColorKey();
                }

                GradientAlphaKey[] gradientAlphaKeys = new GradientAlphaKey[alphaKeys.Length];
                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    gradientAlphaKeys[i] = alphaKeys[i].ToGradientAlphaKey();
                }

                gradient.SetKeys(gradientColorKeys, gradientAlphaKeys);
                gradient.mode = mode;

                return gradient;
            }
        }

        [System.Serializable]
        private class SerializableColorKey
        {
            public Color color;
            public float time;

            public SerializableColorKey(GradientColorKey colorKey)
            {
                color = colorKey.color;
                time = colorKey.time;
            }

            public GradientColorKey ToGradientColorKey()
            {
                return new GradientColorKey(color, time);
            }
        }

        [System.Serializable]
        private class SerializableAlphaKey
        {
            public float alpha;
            public float time;

            public SerializableAlphaKey(GradientAlphaKey alphaKey)
            {
                alpha = alphaKey.alpha;
                time = alphaKey.time;
            }

            public GradientAlphaKey ToGradientAlphaKey()
            {
                return new GradientAlphaKey(alpha, time);
            }
        }

        public static string SerializeGradient(Gradient gradient)
        {
            SerializableGradient serializableGradient = new SerializableGradient(gradient);
            return JsonUtility.ToJson(serializableGradient);
        }

        public static Gradient DeserializeGradient(string json)
        {
            SerializableGradient serializableGradient = JsonUtility.FromJson<SerializableGradient>(json);

            if (serializableGradient != null)
            {
                return serializableGradient.ToGradient();
            }

            Debug.LogError("Failed to deserialize Gradient from JSON: " + json);
            return new Gradient(); // Return a default Gradient or handle the error as needed
        }
#endregion
#region AnimationCurveSerialization
        [System.Serializable]
        private struct SerializableKeyframe
        {
            public float time;
            public float value;
            public float inTangent;
            public float outTangent;

            public SerializableKeyframe(Keyframe keyframe)
            {
                time = keyframe.time;
                value = keyframe.value;
                inTangent = keyframe.inTangent;
                outTangent = keyframe.outTangent;
            }
        }

        [System.Serializable]
        private struct SerializableAnimationCurve
        {
            public WrapMode preWrapMode;
            public WrapMode postWrapMode;
            public SerializableKeyframe[] keys;

            public SerializableAnimationCurve(AnimationCurve curve)
            {
                preWrapMode = curve.preWrapMode;
                postWrapMode = curve.postWrapMode;
                keys = new SerializableKeyframe[curve.length];
                for (int i = 0; i < curve.length; i++)
                {
                    keys[i] = new SerializableKeyframe(curve[i]);
                }
            }
        }

        /// <summary>
        /// Serializes an AnimationCurve to a JSON string.
        /// </summary>
        public static string SerializeAnimationCurve(AnimationCurve curve)
        {
            SerializableAnimationCurve serializableCurve = new SerializableAnimationCurve(curve);
            string json = JsonUtility.ToJson(serializableCurve);
            return json;
        }

        /// <summary>
        /// Produces an AnimationCurve from a json string.
        /// </summary>
        public static AnimationCurve DeserializeAnimationCurve(string json)
        {
            SerializableAnimationCurve serializableCurve = JsonUtility.FromJson<SerializableAnimationCurve>(json);

            Keyframe[] keyframes = new Keyframe[serializableCurve.keys.Length];
            for (int i = 0; i < keyframes.Length; i++)
            {
                keyframes[i] = new Keyframe(
                    serializableCurve.keys[i].time,
                    serializableCurve.keys[i].value,
                    serializableCurve.keys[i].inTangent,
                    serializableCurve.keys[i].outTangent
                );
            }

            AnimationCurve curve = new AnimationCurve(keyframes);
            curve.postWrapMode = serializableCurve.postWrapMode;
            curve.preWrapMode = serializableCurve.postWrapMode;
            return curve;
        }
#endregion
#region VectorSerialization
        public static Vector2 ParseVector2(string vectorString)
        {
            vectorString = vectorString.Replace("(", "").Replace(")", "");
            string[] components = vectorString.Split(',');

            if (components.Length == 2 &&
                float.TryParse(components[0], out float x) &&
                float.TryParse(components[1], out float y))
            {
                return new Vector2(x, y);
            }

            Debug.LogError("Failed to parse Vector2 from string: " + vectorString);
            return Vector2.zero;
        }

        public static Vector2Int ParseVector2Int(string vectorString)
        {
            vectorString = vectorString.Replace("(", "").Replace(")", "");
            string[] components = vectorString.Split(',');

            if (components.Length == 2 &&
                int.TryParse(components[0], out int x) &&
                int.TryParse(components[1], out int y))
            {
                return new Vector2Int(x, y);
            }

            Debug.LogError("Failed to parse Vector2 from string: " + vectorString);
            return Vector2Int.zero;
        }

        public static Vector3 ParseVector3(string vectorString)
        {
            vectorString = vectorString.Replace("(", "").Replace(")", "");
            string[] components = vectorString.Split(',');

            if (components.Length == 3 &&
                float.TryParse(components[0], out float x) &&
                float.TryParse(components[1], out float y) &&
                float.TryParse(components[2], out float z))
            {
                return new Vector3(x, y, z);
            }

            Debug.LogError("Failed to parse Vector3 from string: " + vectorString);
            return Vector3.zero;
        }

        public static Vector3Int ParseVector3Int(string vectorString)
        {
            vectorString = vectorString.Replace("(", "").Replace(")", "");
            string[] components = vectorString.Split(',');

            if (components.Length == 3 &&
                int.TryParse(components[0], out int x) &&
                int.TryParse(components[1], out int y) &&
                int.TryParse(components[2], out int z))
            {
                return new Vector3Int(x, y, z);
            }

            Debug.LogError("Failed to parse Vector3Int from string: " + vectorString);
            return Vector3Int.zero;
        }

        public static Vector4 ParseVector4(string vectorString)
        {
            vectorString = vectorString.Replace("(", "").Replace(")", "");
            string[] components = vectorString.Split(',');

            if (components.Length == 4 &&
                float.TryParse(components[0], out float x) &&
                float.TryParse(components[1], out float y) &&
                float.TryParse(components[2], out float z) &&
                float.TryParse(components[3], out float w))
            {
                return new Vector4(x, y, z, w);
            }

            Debug.LogError("Failed to parse Vector4 from string: " + vectorString);
            return Vector4.zero;
        }
#endregion
#region BoundsSerialization
        /// <summary>
        /// Produces a Bounds object from the result of Bounds.ToString(). Returns a Bounds with all zero values if unable to parse.
        /// </summary>
        public static Bounds ParseBounds(string boundsString)
        {
            // Remove parentheses and labels from the string
            boundsString = Regex.Replace(boundsString, @"[^\d\.\-,]", "");

            string[] components = boundsString.Split(',');

            if (components.Length == 6 &&
                float.TryParse(components[0], out float center_x) &&
                float.TryParse(components[1], out float center_y) &&
                float.TryParse(components[2], out float center_z) &&
                float.TryParse(components[3], out float extent_x) &&
                float.TryParse(components[4], out float extent_y) &&
                float.TryParse(components[5], out float extent_z))
            {
                Vector3 center = new Vector3(center_x, center_y, center_z);
                Vector3 size = new Vector3(extent_x, extent_y, extent_z) * 2f;
                return new Bounds(center, size);
            }

            Debug.LogWarning("Failed to parse Bounds from string: " + boundsString);
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        /// <summary>
        /// Produces a BoundsInt object from the result of BoundsInt.ToString(). Returns a Bounds with all zero values if unable to parse.
        /// </summary>
        public static BoundsInt ParseBoundsInt(string boundsString)
        {
            // Remove parentheses and labels and any unwanted decimals from the string
            boundsString = Regex.Replace(boundsString, @"[^\d\-,]", "");

            string[] components = boundsString.Split(',');

            if (components.Length == 6 &&
                int.TryParse(components[0], out int center_x) &&
                int.TryParse(components[1], out int center_y) &&
                int.TryParse(components[2], out int center_z) &&
                int.TryParse(components[3], out int extent_x) &&
                int.TryParse(components[4], out int extent_y) &&
                int.TryParse(components[5], out int extent_z))
            {
                Vector3Int center = new Vector3Int(center_x, center_y, center_z);
                Vector3Int size = new Vector3Int(extent_x, extent_y, extent_z) * 2;
                return new BoundsInt(center, size);
            }

            Debug.LogWarning("Failed to parse BoundsInt from string: " + boundsString);
            return new BoundsInt(Vector3Int.zero, Vector3Int.zero);
        }
#endregion
#region RectSerialization

        /// <summary>
        /// Takes the string result of Rect.ToString() and produces the original Rect. Returns a zero-rect if unable to parse.
        /// </summary>
        public static Rect ParseRect(string rectString)
        {
            // Remove parentheses and labels from the string
            rectString = Regex.Replace(rectString, @"[^\d\.\-,]", "");

            string[] components = rectString.Split(',');

            if (components.Length == 4 &&
                float.TryParse(components[0], out float x) &&
                float.TryParse(components[1], out float y) &&
                float.TryParse(components[2], out float width) &&
                float.TryParse(components[3], out float height))
            {
                Rect rect = new Rect(x, y, width, height);
                return rect;
            }

            Debug.LogWarning("Failed to parse Rect from string: " + rectString);
            return new Rect(0, 0, 0, 0);
        }

        /// <summary>
        /// Takes the string result of RectInt.ToString() and produces the original RectInt. Returns a zero-rect if unable to parse.
        /// </summary>
        public static RectInt ParseRectInt(string rectString)
        {
            // Remove parentheses and labels from the string
            rectString = Regex.Replace(rectString, @"[^\d\-,]", "");

            string[] components = rectString.Split(',');

            if (components.Length == 4 &&
                int.TryParse(components[0], out int x) &&
                int.TryParse(components[1], out int y) &&
                int.TryParse(components[2], out int width) &&
                int.TryParse(components[3], out int height))
            {
                RectInt rect = new RectInt(x, y, width, height);
                return rect;
            }

            Debug.LogWarning("Failed to parse RectInt from string: " + rectString);
            return new RectInt(0, 0, 0, 0);
        }

#endregion

        /// <summary>
        /// Takes the type, encoded as string, and the enum value and produces an Enum of the proper type.
        /// </summary>
        public static bool EnumDeserialize(string contentType, string encodedValue, out object enumValue)
        {
            Type type = Type.GetType(contentType);
            if (type != null && type.IsEnum)
            {
                if (Enum.TryParse(type, encodedValue, out object enumIntermediateValue))
                {
                    enumValue = Convert.ChangeType(enumIntermediateValue, type);
                    return true;
                }
            }
            enumValue = null;
            return false;
        }

        /// <summary>
        /// Takes as input a string to be converted to a type, which is used to produce a Deserialized object.
        /// </summary>
        public static bool TryDeserializeJSON(string contentType, string json, out object result)
        {
            Type type = Type.GetType(contentType);
            if (type != null)
            {
                result = JsonUtility.FromJson(json, type);
                if (result != null)
                {
                    return true;
                }
            }
            result = null;
            return false;
        }
    }
}


}