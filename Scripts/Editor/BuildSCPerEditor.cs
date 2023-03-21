using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;

namespace CodySource
{
    [CustomEditor(typeof(BuildSCPer))]
    public class BuildSCPerEditor : Editor
    {

        #region PUBLIC METHODS

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = (!(File.Exists("Assets/BuildUploader_ActiveBuildInfo.json")) || ((BuildSCPer)target).isRunning) ? Color.gray : Color.white;
            if (GUILayout.Button((!(File.Exists("Assets/BuildUploader_ActiveBuildInfo.json")) || ((BuildSCPer)target).isRunning)? "Running..." : "Run") && !((BuildSCPer)target).isRunning) ((BuildSCPer)target).Run();
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            DrawDefaultInspector();
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Reset if broken")) ((BuildSCPer)target).ResetProcesses();
        }

        #endregion

    }
}

#else

namespace CodySource { public class BuildUploaderEditor {} }

#endif