    using UnityEngine;
    using System;
    using System.Linq;
    using System.Reflection;

    [CreateAssetMenu(menuName = "Game/Mystery/Data", fileName = "MysteryData_")]
    public class MysteryData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Header("Scene")]
        public string sceneName;

        // [Header("Handler (IMystery)")]
        // [SerializeField, HideInInspector] private string handlerTypeName; // AssemblyQualifiedName
        // public string HandlerTypeName => handlerTypeName;

        [Header("Selection (Optional)")]
        [Range(0f, 1f)] public float weight = 1f; // database ilk eklemede istersen buradan doldurulabilir

        /// <summary>Runtime’da handler tipini çöz.</summary>
        // public Type GetHandlerType()
        // {
        //     if (string.IsNullOrEmpty(handlerTypeName)) return null;

        //     var t = Type.GetType(handlerTypeName);
        //     if (t != null) return t;

        //     foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        //     {
        //         t = asm.GetType(handlerTypeName);
        //         if (t != null) return t;
        //     }
        //     return null;
        // }

    // #if UNITY_EDITOR
    //     // Sadece editor’dan setlenir
    //     public void __EditorSetHandlerType(Type t)
    //     {
    //         handlerTypeName = t != null ? t.AssemblyQualifiedName : null;
    //         UnityEditor.EditorUtility.SetDirty(this);
    //     }
    // #endif
    }
