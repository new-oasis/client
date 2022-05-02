// Reenable with assembly definition

// using UnityEditor;
// using UnityEngine;
// using Unity.Mathematics;
// using Unity.Mathematics.Editor;
//
// [CustomEditor(typeof(Int3Event))]
// public class EventEditor : Editor
// {
//
//     public override void OnInspectorGUI()
//     {
//         base.OnInspectorGUI();
//
//         Int3Event e = target as Int3Event;
//         GUI.enabled = Application.isPlaying;
//         // EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
//
//         if (GUILayout.Button("Invoke Custom"))
//             e.Invoke(e.custom);
//     }
// }
