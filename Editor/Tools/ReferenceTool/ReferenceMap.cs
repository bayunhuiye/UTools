using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
public class MapPair
{
    public string key;
    public List<string> values;

    public MapPair(string key, List<string> values)
    {
        this.key = key;
        this.values = values;
    }
}

[Serializable]
public struct ReferenceMapData
{
    public List<MapPair> pairs;
}

public class ReferenceMap
{
    public Dictionary<string, List<string>> mapDic = new Dictionary<string, List<string>>();
    public string dataPath;

    private static readonly object lockObj = new object();

    public ReferenceMap(string path)
    {
        loadFromPath(path);
    }

    public void loadFromPath(string path)
    {
        dataPath = path;
        if (File.Exists(dataPath))
        {
            ReferenceMapData data = JsonUtility.FromJson<ReferenceMapData>(File.ReadAllText(dataPath));
            mapDic = data.pairs.ToDictionary(v => v.key, v => v.values);
        }
    }

    public void clear()
    {
        mapDic.Clear();
    }

    public void remove(string key)
    {
        lock (lockObj)
        {
            mapDic.Remove(key);
        }
    }

    public void update(string key, List<string> values)
    {
        lock (lockObj)
        {
            mapDic[key] = values;
        }
    }

    public void update(Dictionary<string, List<string>> mapDic)
    {
        this.mapDic = mapDic;
    }

    public void writeToDisk()
    {
        ReferenceMapData data = new ReferenceMapData();
        data.pairs = mapDic.Select(v => new MapPair(v.Key, v.Value));

        foreach (var v in data.pairs)
        {
            v.values = v.values.Distinct().ToList();
        }

        File.WriteAllText(dataPath, JsonUtility.ToJson(data));
    }
}