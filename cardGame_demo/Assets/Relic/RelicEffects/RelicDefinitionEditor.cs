#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

[CustomEditor(typeof(RelicDefinition))]
public class RelicDefinitionEditor : Editor
{
    SerializedProperty effectsProp;

    Type[] _effectTypes;
    Type[] EffectTypes => _effectTypes ??= AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
        .Where(t => typeof(IRelicEffect).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
        .OrderBy(t => t.Name)
        .ToArray();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // default alanları çiz
        DrawPropertiesExcluding(serializedObject, "m_Script", "effects");

        // Effects başlığı
        if (effectsProp == null) effectsProp = serializedObject.FindProperty("effects");
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

        // Elemanları çiz
        if (effectsProp != null)
        {
            for (int i = 0; i < effectsProp.arraySize; i++)
            {
                var elem = effectsProp.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    // üst satır: tip adı + butonlar
                    string curTypeName = elem.managedReferenceFullTypename;
                    string shortName = string.IsNullOrEmpty(curTypeName)
                        ? "(None)"
                        : curTypeName.Split('.').Last();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Element {i} — {shortName}", EditorStyles.miniBoldLabel);

                    if (GUILayout.Button("Change Type", GUILayout.Width(100)))
                        ShowTypeMenuFor(elem);

                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        effectsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    // alanları çiz
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(elem, includeChildren: true);
                    EditorGUI.indentLevel--;
                }
            }

            // Add butonu
            if (GUILayout.Button("+ Add Effect"))
            {
                effectsProp.InsertArrayElementAtIndex(effectsProp.arraySize);
                var newElem = effectsProp.GetArrayElementAtIndex(effectsProp.arraySize - 1);
                newElem.managedReferenceValue = null; // boş başlat
                ShowTypeMenuFor(newElem);             // ekler eklemez tip seçtir
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    void ShowTypeMenuFor(SerializedProperty elem)
    {
        var menu = new GenericMenu();
        foreach (var t in EffectTypes)
        {
            menu.AddItem(new GUIContent(t.FullName), false, () =>
            {
                object inst = Activator.CreateInstance(t);
                elem.managedReferenceValue = inst;
                elem.serializedObject.ApplyModifiedProperties();
            });
        }
        menu.ShowAsContext();
    }
}
#endif
