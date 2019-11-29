using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;


public static class ReferenceToolUtil
{
    public static string GetContent(string path) => File.ReadAllText(path);

    public static List<string> GetGuidsByPath(string path) => GetRefsFromContent(GetContent(path));

    public const string refRegexString = @"fileID: -?\d*, guid: [a-zA-Z0-9]*";
    public static Regex refRegex = new Regex(refRegexString);

    public static List<string> GetRefsFromContent(string str)
    {
        var refInfos = new List<string>();
        var matchs = refRegex.Matches(str);
        foreach (Match v in matchs)
        {
            refInfos.Add(v.Value);
        }

        return refInfos;
    }

    public static string[] guidMapHandleExs =
    {
        ".asset",
        ".unity",
        ".prefab",
        ".mat",
        ".anim",
    };

    public static string[] rfMapHandleExs =
    {
        ".unity",
        ".prefab",
    };

    public static readonly Type type_object = typeof(UnityEngine.Object);
    public static readonly Type type_gameobject = typeof(GameObject);
    public static readonly Type type_scene = typeof(SceneAsset);
    public static readonly Type type_material = typeof(Material);
    public static readonly Type type_script = typeof(MonoScript);
    public static readonly Type type_anim = typeof(Animation);
    public static readonly Type type_scriptable = typeof(ScriptableObject);

    public static Type GetTypeByExtension(string ex)
    {
        switch (ex.ToLower())
        {
            case ".prefab":
                return type_gameobject;
            case ".unity":
                return type_scene;
            case ".mat":
                return type_material;
            case ".cs":
                return type_script;
            case ".anim":
                return type_anim;
            case ".asset":
                return type_scriptable;
        }

        return type_object;
    }
}