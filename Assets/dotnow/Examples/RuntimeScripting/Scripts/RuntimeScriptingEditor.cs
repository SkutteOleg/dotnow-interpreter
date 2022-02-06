#if UNITY_EDITOR && ROSLYNCSHARP
using RoslynCSharp.Example;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RuntimeScripting))]
public class RuntimeScriptingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!Application.isPlaying)
        {
            EditorGUILayout.LabelField("Enter play mode to run script");
            return;
        }

        if (GUILayout.Button("Run Script"))
            ((RuntimeScripting) target).RunScript();
    }
}
#endif