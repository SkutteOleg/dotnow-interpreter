#if UNITY_EDITOR
using dotnow.Examples.RuntimeScripting;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AssemblyLoader))]
public class AssemblyLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter play mode to load assembly", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Load Assembly"))
            ((AssemblyLoader)target).RunScript();
    }
}
#endif