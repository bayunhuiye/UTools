using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class UToolsUtil
{
    public static RuntimePlatform platform;

    static UToolsUtil() => platform = Application.platform;

    public static bool IsMac => platform == RuntimePlatform.OSXEditor;

    private static List<Type> types;

    public static Type GetBuiltinClassType(string name)
    {
        if (types == null)
        {
            types = new List<Type>();
            types.AddRange(Assembly.GetAssembly(typeof(UnityEngine.Object)).GetTypes());
            types.AddRange(Assembly.GetAssembly(typeof(UnityEngine.UI.Image)).GetTypes());
            types.AddRange(Assembly.GetAssembly(typeof(Editor)).GetTypes());
        }

        return types.Find(v => v.FullName == name || v.Name == name);
    }

    public static string GetAssetDir(UnityEngine.Object asset)
    {
        var tmsPath = AssetDatabase.GetAssetPath(asset);
        var dir = Path.GetDirectoryName(tmsPath);
        return dir;
    }

    public static T FindAsset<T>(
        string filter,
        string relativePath,
        bool recursive
    )
        where T : UnityEngine.Object
    {
        var paths = FindAssetPath(filter, relativePath, recursive, false);

        foreach (var path in paths)
        {
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (obj is T o)
            {
                return o;
            }
        }

        return default;
    }

    public static List<string> FindAllAssetPath(
        string filter,
        string relativePath,
        bool recursive
    ) =>
        FindAssetPath(filter, relativePath, recursive, false);

    private static List<string> FindAssetPath(
        string filter,
        string relativePath,
        bool recursive,
        bool firstReturn
    )
    {
        var results = new List<string>();

        var guids = AssetDatabase.FindAssets(filter, new[] {relativePath});

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (recursive)
            {
                results.Add(path);
                if (firstReturn)
                {
                    return results;
                }
            }
            else
            {
                var itemDir = Path.GetDirectoryName(path);
                if (itemDir == relativePath)
                {
                    results.Add(path);
                    if (firstReturn)
                    {
                        return results;
                    }
                }
            }
        }

        return results;
    }
}