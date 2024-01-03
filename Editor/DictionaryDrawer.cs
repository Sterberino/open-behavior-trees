using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using System.Reflection;

//Based on: https://forum.unity.com/threads/finally-a-serializable-dictionary-for-unity-extracted-from-system-collections-generic.335797/ 
namespace OpenBehaviorTrees
{
public abstract class DictionaryDrawer<TK, TV> : PropertyDrawer
{
    private Dictionary<TK, TV> _Dictionary;
    private bool _Foldout;
    private const float kButtonWidth = 18f;
    private static float lineHeight = EditorGUIUtility.singleLineHeight + 4;
    private float spacing = 12f;
    private float fieldPadding = 1f;

    private GUIStyle addEntryStyle;
    private GUIContent addEntryContent;
    private GUIStyle clearDictionaryStyle;
    private GUIContent clearDictionaryContent;
    //reuses clearDictionaryStyle. I am adding it for readability
    private GUIStyle removeEntryStyle;
    private GUIContent removeEntryContent;

    private GUIStyle HeaderStyle;

    private Rect buttonRect;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        CheckInitialize(property, label);
        if (_Foldout)
        {
            //Height of the main Header and the two column headers + height of all the drawn dictionary entries + a little padding on the bottom.
            return (GetDictionaryElementsHeight() + (lineHeight * 2)) + 14f;
        }
        return lineHeight+ 4f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        CheckInitialize(property, label);

        position.height = 20f;
        DrawHeader(position, property, label);


        if (!_Foldout)
            return;
        position.y += 5f + lineHeight * 2;
        foreach (var item in _Dictionary)
        {
            var key = item.Key;
            var value = item.Value;

            var keyRect = position;
            keyRect.width /= 3;
            keyRect.x += 10;
            //Apply vertical padding
            keyRect.y += fieldPadding;
            keyRect.height -= fieldPadding * 2;
            
            EditorGUI.BeginChangeCheck();
            var newKey = DoField(keyRect, typeof(TK), (TK)key);
            if (EditorGUI.EndChangeCheck())
            {
                try
                {
                    _Dictionary.Remove(key);
                    _Dictionary.Add(newKey, value);
                }
                catch (Exception e)
                {
                    _Dictionary.Remove(key);
                    Debug.Log(e.Message);
                }
                break;
            }

            var valueRect = position;
            valueRect.x = keyRect.xMax + spacing;
            valueRect.y += fieldPadding;
            //Apply vertical padding
            valueRect.height -= fieldPadding * 2;
            valueRect.width = (position.width - keyRect.width) - ((kButtonWidth + 2) * 2f) - valueRect.size.y - (spacing* 2.5f);
            EditorGUI.BeginChangeCheck();
            value = DoField(valueRect, typeof(TV), (TV)value);


            Rect changeValueRect = new Rect(new Vector2(buttonRect.x - 2f, valueRect.position.y), new Vector2(kButtonWidth, valueRect.size.y));
            value = ChangeValueType(changeValueRect, key, value);

            if (EditorGUI.EndChangeCheck())
            {
                _Dictionary[key] = value;
                break;
            }
            EditorGUIUtility.AddCursorRect(changeValueRect, MouseCursor.Link);

            var removeRect = valueRect;
            removeRect.x = buttonRect.x + kButtonWidth;
            removeRect.width = kButtonWidth;
            if (GUI.Button(removeRect, removeEntryContent, removeEntryStyle))
            {
                RemoveItem(key);
                break;
            }
            EditorGUIUtility.AddCursorRect(removeRect, MouseCursor.Link);
            position.y += Mathf.Max(GetEntryHeight(key) ,GetEntryHeight(value));
        }
    }

    /// <summary>
    /// Gets the combined height of all dictionary elements
    /// </summary>
    /// <returns></returns>
    private float GetDictionaryElementsHeight()
    {
        float height = 0;
        foreach(var item in _Dictionary)
        {
            var key = item.Key;
            var value = item.Value;
            height += Mathf.Max(GetEntryHeight(key), GetEntryHeight(value));
        }

        return height;
    }

    private void DrawColumn(Rect position, GUIStyle style)
    {
        Rect columnRect = new Rect(position.x, position.yMax - 1, position.width, GetDictionaryElementsHeight() + 12f);
        GUI.Box(columnRect, GUIContent.none, style);
    }

    private void DrawHeader(Rect position, SerializedProperty property, GUIContent label)
    {
        Rect headerRect = new Rect(position.position, new Vector2(position.size.x - kButtonWidth * 1.5f, lineHeight));
        GUI.Box(headerRect, GUIContent.none, HeaderStyle);
        var foldoutRect = position;
        foldoutRect.x +=  kButtonWidth;
        foldoutRect.width -= 2 * kButtonWidth;
        EditorGUI.BeginChangeCheck();
        if(_Dictionary.Count > 0)
        {
            _Foldout = EditorGUI.Foldout(foldoutRect, _Foldout, label, true);
        }
        else
        {
            EditorGUI.LabelField(foldoutRect, label);
            _Foldout = false;
        }
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(label.text, _Foldout);
        }

        //Draw the Add Item Button
        buttonRect = position;
        buttonRect.x = position.width - 20 - kButtonWidth + position.x + 1;
        buttonRect.width = kButtonWidth;

        GUIStyle headerButtonStyle = new GUIStyle(HeaderStyle);
        headerButtonStyle.padding = new RectOffset(0, 0, 0, 0);
        Rect headerButtonRect = new Rect(buttonRect.position, new Vector2(kButtonWidth * 1.5f, lineHeight));
        if (GUI.Button(headerButtonRect, addEntryContent, headerButtonStyle))
        {
            AddNewItem();
        }
        EditorGUIUtility.AddCursorRect(headerButtonRect, MouseCursor.Link);
        buttonRect.x -= kButtonWidth;

        //Draw the Item count label
        GUIStyle headerItemCountLabelStyle = new GUIStyle("MiniLabel");
        GUIContent headerItemCountLabelContent = new GUIContent();
        if(_Dictionary.Count == 0)
        {
            headerItemCountLabelContent = new GUIContent("Empty");
        }
        else
        {
            headerItemCountLabelContent = new GUIContent($"{_Dictionary.Count} Item{(_Dictionary.Count == 1 ? "" : "s")}");
        }

        GUI.Label(new Rect(buttonRect.x - 30f, buttonRect.y, 50f, headerRect.height), headerItemCountLabelContent, headerItemCountLabelStyle);


        //Draw the header labels (Keys - Values)
        if(_Foldout)
        {
            //Draw "Keys" header
            position.y += headerRect.height;
            Rect keyHeaderRect = new Rect(position.x, position.y - 1, position.width /3f + kButtonWidth - 1,  headerRect.height);
            GUIStyle columnHeaderStyle = new GUIStyle("GroupBox");
            columnHeaderStyle.padding = new RectOffset(0, 0, 0, 0);
            columnHeaderStyle.contentOffset = new Vector2(0, 3f);
            GUI.Box(keyHeaderRect, new GUIContent("Keys"), columnHeaderStyle);
            
            //Draw "Values" header
            Rect valuesHeaderRect = new Rect(keyHeaderRect.xMax - 1, keyHeaderRect.y, (position.width - keyHeaderRect.width - kButtonWidth * 0.5f), keyHeaderRect.height);
            GUI.Box(valuesHeaderRect, new GUIContent("Values"), columnHeaderStyle);
            //Draw the Columns for the keys and values.
            DrawColumn(keyHeaderRect, columnHeaderStyle);
            DrawColumn(valuesHeaderRect, columnHeaderStyle);
            
            position.y += headerRect.height;
        }

        /*
        if (GUI.Button(buttonRect, clearDictionaryContent, clearDictionaryStyle))
        {
            ClearDictionary();
        }
        */
    }

    #region TypeControls
    private static float GetEntryHeight<T>(T value)
    {
        switch (value)
        {
            case Bounds: return lineHeight * 2;
            case BoundsInt: return lineHeight * 2;
            case Rect: return lineHeight * 2;
            case RectInt: return lineHeight * 2;
            default: return lineHeight;
        }
    }

    private static T DoField<T>(Rect rect, Type type, T value)
    {            
        if (typeof(UnityObject).IsAssignableFrom(type))
            return (T)(object)EditorGUI.ObjectField(rect, (UnityObject)(object)value, type, true);
        switch (value)
        {
            case null: EditorGUI.LabelField(rect, "null"); return value;
            case long: return (T)(object)EditorGUI.LongField(rect, (long)(object)value);
            case int: return (T)(object)EditorGUI.IntField(rect, (int)(object)value);
            case float: return (T)(object)EditorGUI.FloatField(rect, (float)(object)value);
            case double: return (T)(object)EditorGUI.DoubleField(rect, (double)(object)value);
            case string: return (T)(object)EditorGUI.TextField(rect, (string)(object)value);
            case bool: return (T)(object)EditorGUI.Toggle(rect, (bool)(object)value);
            case Vector2Int: return (T)(object)EditorGUI.Vector2IntField(rect, GUIContent.none, (Vector2Int)(object)value);
            case Vector3Int: return (T)(object)EditorGUI.Vector3IntField(rect, GUIContent.none, (Vector3Int)(object)value);
            case Vector2: return (T)(object)EditorGUI.Vector2Field(rect, GUIContent.none, (Vector2)(object)value);
            case Vector3: return (T)(object)EditorGUI.Vector3Field(rect, GUIContent.none, (Vector3)(object)value);
            case Vector4: return (T)(object)EditorGUI.Vector4Field(rect, GUIContent.none, (Vector4)(object)value);
            case BoundsInt: return (T)(object)EditorGUI.BoundsIntField(rect, (BoundsInt)(object)value);
            case Bounds: return (T)(object)EditorGUI.BoundsField(rect, (Bounds)(object)value);
            case RectInt: return (T)(object)EditorGUI.RectIntField(rect, (RectInt)(object)value);
            case Rect: return (T)(object)EditorGUI.RectField(rect, (Rect)(object)value);
            case Color: return (T)(object)EditorGUI.ColorField(rect, (Color)(object)value);
            case AnimationCurve: return (T)(object)EditorGUI.CurveField(rect, (AnimationCurve)(object)value);
            case Gradient: return (T)(object)EditorGUI.GradientField(rect, (Gradient)(object)value);
            case UnityObject: return (T)(object)EditorGUI.ObjectField(rect, (UnityObject)(object)value, type, true);
        }

        if (value.GetType().IsEnum)
        {
            if (Enum.TryParse(value.GetType(), value.ToString(), out object enumValue))
            {
                return (T)(object)EditorGUI.EnumPopup(rect, (Enum)enumValue);
            }
        }

        //Setup GUIStyle and GUIContent for the "Clear Dictionary" button
        GUIStyle style = new GUIStyle(EditorStyles.miniButton);
        style.padding = new RectOffset(2, 2, 2, 2);
        GUIContent content = new GUIContent(EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow@2x"));
        content.tooltip = "Debug Values";

        Type fieldType = value.GetType();
        bool isStruct = fieldType.IsValueType && !fieldType.IsEnum;
        EditorGUI.LabelField(rect, $"{fieldType.ToString().Replace("+", ".")} {(isStruct ? "struct" : "class")} instance");
        if( GUI.Button(new Rect(rect.xMax - kButtonWidth, rect.y, kButtonWidth, kButtonWidth), content, style))
        {
            Debug.Log(JsonUtility.ToJson(value));
        }

        //DrawSerializableObject(rect, value);
        return value;
    }

    //Unfinished
    /*
    public static void DrawSerializableObject(Rect rect, object obj)
    {
        if (obj == null)
        {
            Console.WriteLine("Object is null.");
            return;
        }
        
        Type type = obj.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(obj);
            rect.y += GetEntryHeight(value);
            Debug.Log($"{field.Name}: {value}");
            if(value != null)
            {
                DoField(rect, value.GetType(), value);
            }
        }
    
    }
    */

private TV ChangeValueType(Rect rect, TK key, TV value)
    {
        GUIContent content = EditorGUIUtility.IconContent("_Popup");
        content.tooltip = "Change Value Type";
        GUIStyle changeItemStyle = new GUIStyle(EditorStyles.miniButton);
        changeItemStyle.padding = new RectOffset(2, 2, 2, 2);

        if (GUI.Button(rect, content, changeItemStyle))
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Numbers/int"), value is int, () => { _Dictionary[key] = (TV)(object)default(int); });
            genericMenu.AddItem(new GUIContent("Numbers/float"), value is float, () => { _Dictionary[key] = (TV)(object)default(float); });
            genericMenu.AddItem(new GUIContent("Numbers/double"), value is double, () => { _Dictionary[key] = (TV)(object)default(double); });
            genericMenu.AddItem(new GUIContent("Numbers/long"), value is long, () => { _Dictionary[key] = (TV)(object)default(long); });
            genericMenu.AddItem(new GUIContent("Vectors/Vector2"), (value is Vector2 && !(value is Vector2Int)), () => { _Dictionary[key] = (TV)(object)default(Vector2); });
            genericMenu.AddItem(new GUIContent("Vectors/Vector3"), (value is Vector3 && !(value is Vector3Int)), () => { _Dictionary[key] = (TV)(object)default(Vector3); });
            genericMenu.AddItem(new GUIContent("Vectors/Vector4"), value is Vector4, () => { _Dictionary[key] = (TV)(object)default(Vector4); });
            genericMenu.AddItem(new GUIContent("Vectors/Vector2Int"), value is Vector2Int, () => { _Dictionary[key] = (TV)(object)default(Vector2Int); });
            genericMenu.AddItem(new GUIContent("Vectors/Vector3Int"), value is Vector3Int, () => { _Dictionary[key] = (TV)(object)default(Vector3Int); });
            genericMenu.AddItem(new GUIContent("Bounds/Bounds"), value is Bounds && value is not BoundsInt, () => { _Dictionary[key] = (TV)(object)default(Bounds); });
            genericMenu.AddItem(new GUIContent("Bounds/BoundsInt"), value is BoundsInt, () => { _Dictionary[key] = (TV)(object)default(BoundsInt); });
            genericMenu.AddItem(new GUIContent("Rects/Rect"), value is Rect && value is not RectInt, () => { _Dictionary[key] = (TV)(object)default(Rect); });
            genericMenu.AddItem(new GUIContent("Rects/RectInt"), value is RectInt, () => { _Dictionary[key] = (TV)(object)default(RectInt); });
            genericMenu.AddItem(new GUIContent("string"), value is string, () => { _Dictionary[key] = (TV)(object)""; });
            genericMenu.AddItem(new GUIContent("bool"), value is bool, () => { _Dictionary[key] = (TV)(object)default(bool); });
            genericMenu.AddItem(new GUIContent("Color"), value is Color, () => { _Dictionary[key] = (TV)(object)default(Color); });
            genericMenu.AddItem(new GUIContent("AnimationCurve"), value is AnimationCurve, () => { _Dictionary[key] = (TV)(object)(new AnimationCurve()); });
            genericMenu.AddItem(new GUIContent("Gradient"), value is Gradient, () => { _Dictionary[key] = (TV)(object)(new Gradient()); });
            genericMenu.AddItem(new GUIContent("Unity Object"), value is UnityObject, () => { _Dictionary[key] = (TV)(object)(new UnityObject()); });
            genericMenu.ShowAsContext();
        }

        return (TV)value;
    }
#endregion

    private void RemoveItem(TK key)
    {
        _Dictionary.Remove(key);
    }

    private void CheckInitialize(SerializedProperty property, GUIContent label)
    {
        if (_Dictionary == null)
        {
            SetupStyles();
            var target = property.serializedObject.targetObject;
            _Dictionary = fieldInfo.GetValue(target) as Dictionary<TK, TV>;
            if (_Dictionary == null)
            {
                _Dictionary = new Dictionary<TK, TV>();
                fieldInfo.SetValue(target, _Dictionary);
            }

            _Foldout = EditorPrefs.GetBool(label.text);
        }
    }

    private void SetupStyles()
    {
        //Setup GUIStyle and GUIContent for the "Add Item" button
        addEntryStyle = new GUIStyle(EditorStyles.miniButton);
        addEntryStyle.padding = new RectOffset(3, 3, 3, 3);
        addEntryContent = new GUIContent(EditorGUIUtility.IconContent("d_CreateAddNew@2x"));
        addEntryContent.tooltip = "Add Item";

        //Setup GUIStyle and GUIContent for the "Clear Dictionary" button
        clearDictionaryStyle = new GUIStyle(EditorStyles.miniButton);
        clearDictionaryStyle.padding = new RectOffset(2, 2, 2, 2);
        clearDictionaryContent = new GUIContent(EditorGUIUtility.IconContent("d_winbtn_win_close@2x"));
        clearDictionaryContent.tooltip = "Clear dictionary";

        removeEntryContent = new GUIContent(EditorGUIUtility.IconContent("d_winbtn_win_close@2x"));
        removeEntryContent.tooltip = "Remove Item";
        removeEntryStyle = new GUIStyle(clearDictionaryStyle);

        HeaderStyle = new GUIStyle("MiniToolbarButton");
        HeaderStyle.fixedHeight = 0;
        HeaderStyle.fixedWidth = 0;
        HeaderStyle.padding = new RectOffset(2,2,2,2);
    }

    private void ClearDictionary()
    {
        _Dictionary.Clear();
    }

    private void AddNewItem()
    {
        TK key;
        if (typeof(TK) == typeof(string))
            key = (TK)(object)"";
        else key = default(TK);

        if (typeof(TV) == typeof(object))
        {
            var value = (TV)(object)1;
            try
            {
                _Dictionary.Add(key, value);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
        else
        {
            var value = default(TV);
            try
            {
                _Dictionary.Add(key, value);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }
}


[CustomPropertyDrawer(typeof(Blackboard))]
public class BlackboardDrawer : DictionaryDrawer<string, object> {}
}