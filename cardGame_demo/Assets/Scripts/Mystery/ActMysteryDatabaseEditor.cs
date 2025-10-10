// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;
// using System.IO;

// [CustomEditor(typeof(ActMysteryDatabase))]
// public class ActMysteryDatabaseEditor : Editor
// {
//     SerializedProperty propAct;
//     SerializedProperty propMysteries;

//     string _searchFolder = "Assets"; // Auto-Fill için başlangıç klasörü
//     string _filter       = "";       // Liste filtreleme

//     void OnEnable()
//     {
//         propAct       = serializedObject.FindProperty("act");
//         propMysteries = serializedObject.FindProperty("mysteries");
//     }

//     public override void OnInspectorGUI()
//     {
//         serializedObject.Update();

//         EditorGUILayout.PropertyField(propAct);

//         EditorGUILayout.Space(5);
//         EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);

//         using (new EditorGUILayout.HorizontalScope())
//         {
//             _filter = EditorGUILayout.TextField("Filter", _filter);
//             if (GUILayout.Button("Normalize", GUILayout.Width(100)))
//             {
//                 NormalizeWeights();
//             }
//         }

//         using (new EditorGUILayout.HorizontalScope())
//         {
//             if (GUILayout.Button("Rebuild From Data.weight"))
//             {
//                 RebuildWeightsFromData();
//             }

//             if (GUILayout.Button("Auto-Fill (All)"))
//             {
//                 AutoFillAllMysteryData();
//             }
//         }

//         using (new EditorGUILayout.HorizontalScope())
//         {
//             _searchFolder = EditorGUILayout.TextField("Folder", _searchFolder);
//             if (GUILayout.Button("Auto-Fill (Folder)"))
//             {
//                 AutoFillMysteryDataByFolder(_searchFolder);
//             }
//         }

//         EditorGUILayout.Space(5);
//         DrawEntryList();

//         serializedObject.ApplyModifiedProperties();
//     }

//     void DrawEntryList()
//     {
//         if (propMysteries == null) return;

//         // Görsel kutu
//         using (new EditorGUILayout.VerticalScope(GUI.skin.box))
//         {
//             int removeIndex = -1;

//             for (int i = 0; i < propMysteries.arraySize; i++)
//             {
//                 var elem  = propMysteries.GetArrayElementAtIndex(i);
//                 var pData = elem.FindPropertyRelative("data");
//                 var pWt   = elem.FindPropertyRelative("weight");

//                 var dataObj = pData.objectReferenceValue as MysteryData;
//                 string label = dataObj ? $"{dataObj.displayName} ({dataObj.name})" : "(null)";

//                 if (!string.IsNullOrEmpty(_filter) &&
//                     label.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) < 0)
//                 {
//                     continue; // filtre dışı
//                 }

//                 using (new EditorGUILayout.HorizontalScope())
//                 {
//                     EditorGUILayout.ObjectField(pData, GUIContent.none);

//                     float newW = Mathf.Max(0f, EditorGUILayout.FloatField(pWt.floatValue, GUILayout.Width(80)));
//                     if (!Mathf.Approximately(newW, pWt.floatValue)) pWt.floatValue = newW;

//                     if (GUILayout.Button("X", GUILayout.Width(24)))
//                         removeIndex = i;
//                 }

//                 // Duplicate uyarısı
//                 if (dataObj != null && IsDuplicate(i, dataObj))
//                 {
//                     EditorGUILayout.HelpBox("Duplicate: Bu MysteryData zaten listede mevcut.", MessageType.Warning);
//                 }

//                 EditorGUILayout.Space(2);
//             }

//             if (removeIndex >= 0)
//             {
//                 propMysteries.DeleteArrayElementAtIndex(removeIndex);
//             }

//             EditorGUILayout.Space(5);
//             // Add alanı
//             using (new EditorGUILayout.HorizontalScope())
//             {
//                 var newData = (MysteryData)EditorGUILayout.ObjectField(null, typeof(MysteryData), false);
//                 if (newData && !ContainsData(newData))
//                 {
//                     int idx = propMysteries.arraySize;
//                     propMysteries.InsertArrayElementAtIndex(idx);
//                     var elem = propMysteries.GetArrayElementAtIndex(idx);
//                     elem.FindPropertyRelative("data").objectReferenceValue = newData;
//                     elem.FindPropertyRelative("weight").floatValue = Mathf.Max(0f, newData.weight);
//                 }
//                 else if (newData && ContainsData(newData))
//                 {
//                     EditorGUILayout.HelpBox("Bu MysteryData zaten ekli.", MessageType.Info);
//                 }
//             }
//         }
//     }

//     bool ContainsData(MysteryData md)
//     {
//         for (int i = 0; i < propMysteries.arraySize; i++)
//         {
//             var elem = propMysteries.GetArrayElementAtIndex(i);
//             var pData = elem.FindPropertyRelative("data");
//             if (pData.objectReferenceValue == md)
//                 return true;
//         }
//         return false;
//     }

//     bool IsDuplicate(int index, MysteryData md)
//     {
//         for (int i = 0; i < propMysteries.arraySize; i++)
//         {
//             if (i == index) continue;
//             var elem = propMysteries.GetArrayElementAtIndex(i);
//             var pData = elem.FindPropertyRelative("data");
//             if (pData.objectReferenceValue == md)
//                 return true;
//         }
//         return false;
//     }

//     void NormalizeWeights()
//     {
//         var list = GetRuntimeList();
//         float sum = list.Sum(e => Mathf.Max(0f, e.weight));
//         if (sum <= 0f) return;

//         Undo.RecordObject(target, "Normalize Weights");
//         for (int i = 0; i < propMysteries.arraySize; i++)
//         {
//             var elem = propMysteries.GetArrayElementAtIndex(i);
//             var pWt  = elem.FindPropertyRelative("weight");
//             float w  = Mathf.Max(0f, pWt.floatValue) / sum;
//             pWt.floatValue = w;
//         }
//     }

//     void RebuildWeightsFromData()
//     {
//         Undo.RecordObject(target, "Rebuild Weights From Data");
//         for (int i = 0; i < propMysteries.arraySize; i++)
//         {
//             var elem = propMysteries.GetArrayElementAtIndex(i);
//             var pData = elem.FindPropertyRelative("data");
//             var pWt   = elem.FindPropertyRelative("weight");
//             var md = pData.objectReferenceValue as MysteryData;
//             pWt.floatValue = md ? Mathf.Max(0f, md.weight) : 0f;
//         }
//     }

//     void AutoFillAllMysteryData()
//     {
//         var guids = AssetDatabase.FindAssets("t:MysteryData");
//         AddByGuids(guids);
//     }

//     void AutoFillMysteryDataByFolder(string folder)
//     {
//         if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
//         {
//             EditorUtility.DisplayDialog("Folder Error", "Geçerli bir Assets klasörü girin.", "OK");
//             return;
//         }
//         var guids = AssetDatabase.FindAssets("t:MysteryData", new[] { folder });
//         AddByGuids(guids);
//     }

//     void AddByGuids(string[] guids)
//     {
//         if (guids == null || guids.Length == 0) return;

//         Undo.RecordObject(target, "Auto-Fill MysteryData");
//         foreach (var guid in guids)
//         {
//             string path = AssetDatabase.GUIDToAssetPath(guid);
//             var md = AssetDatabase.LoadAssetAtPath<MysteryData>(path);
//             if (!md) continue;
//             if (ContainsData(md)) continue;

//             int idx = propMysteries.arraySize;
//             propMysteries.InsertArrayElementAtIndex(idx);
//             var elem = propMysteries.GetArrayElementAtIndex(idx);
//             elem.FindPropertyRelative("data").objectReferenceValue = md;
//             elem.FindPropertyRelative("weight").floatValue = Mathf.Max(0f, md.weight);
//         }
//     }

//     List<ActMysteryDatabase.Entry> GetRuntimeList()
//     {
//         var list = new List<ActMysteryDatabase.Entry>();
//         for (int i = 0; i < propMysteries.arraySize; i++)
//         {
//             var elem = propMysteries.GetArrayElementAtIndex(i);
//             var pData = elem.FindPropertyRelative("data");
//             var pWt   = elem.FindPropertyRelative("weight");
//             list.Add(new ActMysteryDatabase.Entry
//             {
//                 data = pData.objectReferenceValue as MysteryData,
//                 weight = pWt.floatValue
//             });
//         }
//         return list;
//     }
// }
// #endif
