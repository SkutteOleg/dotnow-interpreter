#if UNITY_EDITOR && ROSLYNCSHARP
using dotnow.Examples.RuntimeScripting;
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
            EditorGUILayout.HelpBox("Enter play mode to run script", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Run Script"))
            ((RuntimeScripting)target).RunScript();
    }
}
#endif