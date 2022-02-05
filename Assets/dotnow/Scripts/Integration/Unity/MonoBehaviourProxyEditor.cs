#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using dotnow;
using dotnow.Interop;
using dotnow.Reflection;
using UnityEditor;
using UnityEngine;
using AppDomain = dotnow.AppDomain;
using Object = UnityEngine.Object;

[CustomEditor(typeof(MonoBehaviourProxy))]
public class MonoBehaviourProxyEditor : Editor
{
    private int _depth;
    private int _maxDepth = 30;
    private string _path;
    private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
    
    public override void OnInspectorGUI()
    {
        var instance = ((ICLRProxy)target).GetInstance();
        object obj = instance;
        
        if (instance == null)
            return;

        _maxDepth = Mathf.Max(EditorGUILayout.IntField("Max Depth", _maxDepth), 0);
        GUILayout.Space(20);
        EditorGUI.indentLevel-=2;
        EditorGUILayout.LabelField(string.Format("{0} (Interpreted Script)", instance.Type.Name), GUI.skin.FindStyle("IN Title"));
        EditorGUI.indentLevel+=2;

        foreach (CLRField field in instance.Type.GetFields())
            DrawFieldRecursively(field.GetValue(instance), field.Name, field.FieldType, ref obj, field);
    }
    
    private bool DrawFieldRecursively(object obj, string name, Type type, ref object target, FieldInfo field)
    {
        if (_depth >= _maxDepth)
        {
            EditorGUILayout.LabelField("Maximum depth reached!");
            return false;
        }

        EditorGUI.BeginChangeCheck();

        _depth++;
        var oldPath = _path;
        _path += string.Format(".{0}", name);

        var value = obj;
        
        try
        {
            if (type == typeof(AnimationCurve))
                value = EditorGUILayout.CurveField(name, (AnimationCurve) obj);
            else if (type == typeof(bool))
                value = EditorGUILayout.Toggle(name, (bool) obj);
            else if (type == typeof(Bounds))
                value = EditorGUILayout.BoundsField(name, (Bounds) obj);
            else if (type == typeof(BoundsInt))
                value = EditorGUILayout.BoundsIntField(name, (BoundsInt) obj);
            else if (type == typeof(Color))
                value = EditorGUILayout.ColorField(name, (Color) obj);
            else if (type == typeof(double))
                value = EditorGUILayout.DoubleField(name, (double) obj);
            else if (type == typeof(Enum))
                value = EditorGUILayout.EnumFlagsField(name, (Enum) obj);
            else if (type == typeof(float))
                value = EditorGUILayout.FloatField(name, (float) obj);
            else if (type == typeof(int))
                value = EditorGUILayout.IntField(name, (int) obj);
            else if (type == typeof(LayerMask))
                value = EditorGUILayout.LayerField(name, (LayerMask) obj);
            else if (type == typeof(long))
                value = EditorGUILayout.LongField(name, (long) obj);
            else if (typeof(Object).IsAssignableFrom(type))
                value = EditorGUILayout.ObjectField(name, (Object) obj, type, true);
            else if (type == typeof(Rect))
                value = EditorGUILayout.RectField(name, (Rect) obj);
            else if (type == typeof(RectInt))
                value = EditorGUILayout.RectIntField(name, (RectInt) obj);
            else if (type == typeof(string))
                value = EditorGUILayout.TextField(name, (string) obj);
            else if (type == typeof(Vector2))
                value = EditorGUILayout.Vector2Field(name, (Vector2) obj);
            else if (type == typeof(Vector2Int))
                value = EditorGUILayout.Vector2IntField(name, (Vector2Int) obj);
            else if (type == typeof(Vector3))
                value = EditorGUILayout.Vector3Field(name, (Vector3) obj);
            else if (type == typeof(Vector3Int))
                value = EditorGUILayout.Vector3IntField(name, (Vector3Int) obj);
            else if (type == typeof(Vector4))
                value = EditorGUILayout.Vector4Field(name, (Vector4) obj);
            else if (obj != null)
            {
                // ReSharper disable once AssignmentInConditionalExpression
                if (_foldouts[_path] = EditorGUILayout.Foldout(!_foldouts.ContainsKey(_path) || _foldouts[_path], name))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        if (typeof(IList).IsAssignableFrom(type))
                        {
                            var list = (IList) obj;
                            EditorGUI.BeginChangeCheck();
                            object size = EditorGUILayout.DelayedIntField("Size", list.Count);
                            var listType = type.GetGenericArguments().Length > 0 ? type.GetGenericArguments()[0] : type.GetElementType();

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (list.IsFixedSize)
                                {
                                    var newArray = (IList) Array.CreateInstance(listType, (int) size);

                                    for (var a = 0; a < (int) size; a++)
                                    {
                                        if (a < list.Count)
                                            newArray[a] = list[a];
                                        else
                                            newArray[a] = listType.IsValueType ? Activator.CreateInstance(listType) : null;
                                    }

                                    list = newArray;
                                }
                                else
                                {
                                    while (list.Count < (int) size)
                                        list.Add(listType.IsValueType ? Activator.CreateInstance(listType) : null);
                                    while (list.Count > (int) size)
                                        list.RemoveAt(list.Count - 1);
                                }
                            }

                            for (var i = 0; i < list.Count; i++)
                            {
                                var o = list[i];
                                if (DrawFieldRecursively(o, string.Format("Element {0}", i), listType, ref o, null))
                                    list[i] = o;
                            }

                            value = list;
                        }
                        else foreach (var f in type.GetFields())
                            DrawFieldRecursively(f.GetValue(obj), f.Name, f.FieldType, ref obj, f);
                    }
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(name, null, type.IsCLRType() ? typeof(object) : type, false);

                if (GUILayout.Button("Initialize field"))
                {
                    try
                    {
                        value = type.IsCLRType() ? AppDomain.Active.CreateInstance((CLRType) type) : Activator.CreateInstance(type);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(string.Format("{0}\n{1}", e.Message, e.StackTrace));
                        value = FormatterServices.GetUninitializedObject(type);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(string.Format("{0}\n{1}", e.Message, e.StackTrace));
        }

        _depth--;
        _path = oldPath;

        if (!EditorGUI.EndChangeCheck())
            return false;
        
        if (field != null)
            field.SetValue(target, value);
        else
            target = value;

        return true;
    }
}
#endif