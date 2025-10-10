// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEngine;
// using System;
// using System.Linq;
// using System.Collections.Generic;

// [CustomEditor(typeof(MysteryData))]
// public class MysteryDataEditor : Editor
// {
//     // Cache
//     static Type[] _handlerTypes;
//     static Type[] HandlerTypes => _handlerTypes ??= GetHandlerTypes();

//     public override void OnInspectorGUI()
//     {
//         // 1) Varsayılan alanları çiz (id, displayName, description vs.)
//         DrawDefaultInspector();

//         var data = (MysteryData)target;
//         string currentFull  = data?.HandlerTypeName ?? string.Empty;
//         string currentShort = ShortName(currentFull);

//         EditorGUILayout.Space(8);
//         EditorGUILayout.LabelField("Handler (IMystery)", EditorStyles.boldLabel);

//         using (new EditorGUILayout.VerticalScope(GUI.skin.box))
//         {
//             EditorGUILayout.LabelField("Current", string.IsNullOrEmpty(currentShort) ? "(none)" : currentShort);

//             using (new EditorGUILayout.HorizontalScope())
//             {
//                 if (GUILayout.Button("Change Type", GUILayout.Width(110)))
//                 {
//                     ShowTypeMenu(data);
//                 }

//                 if (GUILayout.Button("Clear", GUILayout.Width(80)))
//                 {
//                     Undo.RecordObject(data, "Clear Mystery Handler");
//                     data.__EditorSetHandlerType(null);
//                     EditorUtility.SetDirty(data);
//                 }
//             }
//         }

//         EditorGUILayout.HelpBox(
//             "IMystery implement eden bir MonoBehaviour tipi seç. Seçim AssemblyQualifiedName olarak saklanır ve runtime'da çözülür.",
//             MessageType.Info
//         );
//     }

//     // --- Helpers ---

//     static void ShowTypeMenu(MysteryData data)
//     {
//         var menu = new GenericMenu();

//         // Tipler (alfabetik)
//         foreach (var t in HandlerTypes)
//         {
//             string label = t.FullName; // istersen t.Name göster
//             menu.AddItem(new GUIContent(label), false, () =>
//             {
//                 Undo.RecordObject(data, "Change Mystery Handler");
//                 data.__EditorSetHandlerType(t);
//                 EditorUtility.SetDirty(data);
//             });
//         }

//         // Ekstra: (none) seçeneği
//         menu.AddSeparator("");
//         menu.AddItem(new GUIContent("(none)"), false, () =>
//         {
//             Undo.RecordObject(data, "Clear Mystery Handler");
//             data.__EditorSetHandlerType(null);
//             EditorUtility.SetDirty(data);
//         });

//         menu.ShowAsContext();
//     }

//     static Type[] GetHandlerTypes()
//     {
//         // IMystery uygulayan ve MonoBehaviour’dan türeyen, somut tipler
//         IEnumerable<Type> AllTypesSafe(System.Reflection.Assembly a)
//         {
//             try { return a.GetTypes(); }
//             catch { return Array.Empty<Type>(); }
//         }

//         var types = AppDomain.CurrentDomain
//             .GetAssemblies()
//             .SelectMany(AllTypesSafe)
//             .Where(t =>
//                 t != null &&
//                 !t.IsAbstract &&
//                 typeof(MonoBehaviour).IsAssignableFrom(t) &&
//                 typeof(IMystery).IsAssignableFrom(t))
//             .OrderBy(t => t.FullName)
//             .ToArray();

//         return types;
//     }

//     static string ShortName(string assemblyQualifiedName)
//     {
//         if (string.IsNullOrEmpty(assemblyQualifiedName)) return null;
//         int comma = assemblyQualifiedName.IndexOf(',');
//         return (comma > 0) ? assemblyQualifiedName.Substring(0, comma) : assemblyQualifiedName;
//     }
// }
// #endif
