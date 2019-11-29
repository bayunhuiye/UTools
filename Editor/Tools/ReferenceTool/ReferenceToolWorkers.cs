using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public abstract class ReferenceToolWork
{
    public Action eventStateChanged;

    public bool working;

    protected void BeginWork(string workName)
    {
        working = true;
        eventStateChanged?.Invoke();
    }

    protected void EndWork()
    {
        GC.Collect();
        working = false;
        eventStateChanged?.Invoke();
    }
}

public abstract class MapGen : ReferenceToolWork
{
    public abstract void GenerateMap(
        ReferenceMap map,
        string searchFolder,
        string rgoption
    );
}

public class GUIDMapGen : MapGen
{
    public void SyncChangeLogToMap(ReferenceMap guidMap)
    {
        var logPath = ReferenceToolSetting.assetChangeLogPath;
        if (!File.Exists(logPath))
        {
            return;
        }

        BeginWork("SyncChangeLogToGUIDMap");

        //去重，有的资源可能会被重新导入很多遍，比如Scene，修改一次就会被导入3遍
        //todo 需要验证一下去重完之后是不是还保证原来的顺序
        var logs = File.ReadAllLines(logPath).Distinct().Select(
            v =>
            {
                var args = v.Split(',');
                var type = args[0];
                var assetGUID = args[1];
                var path = AssetDatabase.GUIDToAssetPath(assetGUID);
                if (!path.StartsWith("Assets"))
                {
                    return default;
                }

                return new ChangeLog(type, assetGUID, path);
            },
            true
        );

        void WorkAction(ChangeLog log)
        {
            switch (log.type)
            {
                case "update":
                    var assetAbsolutePath = PathUtil.AssetAbsolutePath(log.rpath);
                    if (!File.Exists(assetAbsolutePath))
                    {
                        break;
                    }

                    var guids = ReferenceToolUtil.GetGuidsByPath(assetAbsolutePath);
                    guidMap.update(log.guid, guids);
                    break;
                case "remove":
                    guidMap.remove(log.guid);
                    break;
            }
        }

        void DoneCallback()
        {
            File.Delete(logPath);
            guidMap.writeToDisk();
            EndWork();
        }

        EditorParallelUtil.RunParallel(logs, DoneCallback, WorkAction);
    }

    public override void GenerateMap(
        ReferenceMap map,
        string searchFolder,
        string rgoption
    )
    {
        BeginWork("GenerateGUIDMapRG");

        var logPath = ReferenceToolSetting.assetChangeLogPath;

        map.clear();

        var info = new RgArgumentsInfo(
            ReferenceToolUtil.refRegexString,
            searchFolder,
            "--heading -s -N -o -g '!*GUIDMap.asset'"
        );

        foreach (var v in ReferenceToolUtil.guidMapHandleExs)
        {
            info.InsertArgs($"-g '*{v}'");
        }

        info.AddArgs(rgoption);

        CommandUtil.ExecuteCommand(
            info,
            results =>
            {
                var tempMap = new Dictionary<string, List<string>>();
                List<string> currentFileGUIDList = null;
                for (var i = 0; i < results.Count; i++)
                {
                    var v = results[i];
                    var isPath = !v.StartsWith("fileID:");
                    if (isPath)
                    {
                        var rpath = PathUtil.AssetRelativePath(v);
                        var guid = AssetDatabase.AssetPathToGUID(rpath);
                        currentFileGUIDList = new List<string>();
                        tempMap.Add(guid, currentFileGUIDList);
                    }
                    else
                    {
                        currentFileGUIDList.Add(v);
                    }
                }

                map.update(tempMap);
                map.writeToDisk();
                if (File.Exists(logPath))
                {
                    File.Delete(logPath);
                }

                EndWork();
            }
        );
    }
}

[Serializable]
public class ReferenceToolBase : ReferenceToolWork
{
    public string curFindString;
    public List<UFileInfo> findResult = new List<UFileInfo>();

    public List<ReplaceInfo> replaceResult = new List<ReplaceInfo>();
//        public Dictionary<string, ReplaceInfo> replaceResult =
//            new Dictionary<string, ReplaceInfo>();

    public string replaceTargetString;

    public void EnsureReplaceToStr(string replaceTargetString) => this.replaceTargetString = replaceTargetString;

    public void ReplaceOne(UFileInfo fi)
    {
        ReplaceInternal(fi, curFindString, replaceTargetString);
        AssetDatabase.Refresh();
    }

    public void Replace(UFileInfo fi) => ReplaceInternal(fi, curFindString, replaceTargetString);

    public void ReplaceStringAll()
    {
        BeginWork("ReplaceStrAll");

        replaceResult.Clear();

        void AllDoneCallback()
        {
            AssetDatabase.Refresh();
            EndWork();
        }

        EditorParallelUtil.RunParallel(
            findResult,
            AllDoneCallback,
            Replace
        );
    }

    public void RevertReplace(ReplaceInfo ri)
    {
        var content = File.ReadAllText(ri.FileInfo.absolutePath);
        for (var i = 0; i < ri.StartIndexs.Count; i++)
        {
            var startIndex = ri.StartIndexs[i];
            var fileNewValue = content.Substring(startIndex, ri.newValue.Length);
            if (fileNewValue != ri.newValue)
            {
                Debug.LogError(
                    "replace fail ! this file already changed without unity! path:" +
                    ri.FileInfo.relativePath
                );
                continue;
            }

            content = content.Remove(startIndex, ri.newValue.Length);
            content = content.Insert(startIndex, ri.oldValue);
        }

        File.WriteAllText(ri.FileInfo.relativePath, content);

        lock (replaceResult)
        {
            replaceResult.Remove(ri);
        }

        AssetDatabase.Refresh();
    }

    public void RevertReplaceInfoAll()
    {
        BeginWork("RevertReplaceStrAll");

        void AllDoneCallback()
        {
            AssetDatabase.Refresh();
            EndWork();
        }

        EditorParallelUtil.RunParallel(replaceResult, AllDoneCallback, RevertReplace);
    }

    protected void AddFindResult(string path, AssetFrom assetFrom)
    {
        lock (findResult)
        {
            var info = new UFileInfo(path);
            info.assetFrom = assetFrom;
            findResult.Add(info);
        }
    }

    protected void AddFindResult(List<string> paths, AssetFrom assetFrom)
    {
        lock (findResult)
        {
            foreach (var v in paths)
            {
                var info = new UFileInfo(v);
                info.assetFrom = assetFrom;
                findResult.Add(info);
            }
        }
    }

    protected void ClearFindResult()
    {
        findResult.Clear();
        replaceResult.Clear();
    }

    public bool ContainsReplace(string rpath) => FindReplaceInfo(rpath) != null;

    public ReplaceInfo FindReplaceInfo(string rpath) => replaceResult.Find(v => v.FileInfo.relativePath == rpath);

    protected void ReplaceInternal(
        UFileInfo fi,
        string oldValue,
        string newValue
    )
    {
        if (ContainsReplace(fi.relativePath))
        {
            return;
        }

        if (oldValue.IsNOE())
        {
            return;
        }

        var content = File.ReadAllText(fi.absolutePath);

        var ri = new ReplaceInfo();
        ri.FileInfo = fi;
        ri.oldValue = oldValue;
        ri.newValue = newValue;

        var offset = 0;
        while (true)
        {
            var index = content.IndexOf(
                oldValue,
                offset,
                content.Length - offset,
                StringComparison.Ordinal
            );
            if (index >= 0)
            {
                content = content.Remove(index, oldValue.Length);
                content = content.Insert(index, newValue);
                ri.StartIndexs.Add(index);
                offset = index + newValue.Length;
            }
            else
            {
                break;
            }
        }

        File.WriteAllText(fi.absolutePath, content);

        lock (replaceResult)
        {
            replaceResult.Add(ri);
        }
    }

    protected void SortResult() =>
        findResult.Sort(
            (x, y) =>
            {
                var compType = string.Compare(
                    x.extension,
                    y.extension,
                    StringComparison.Ordinal
                );
                if (compType != 0)
                {
                    return compType;
                }

                return string.Compare(
                    x.relativePath,
                    y.relativePath,
                    StringComparison.Ordinal
                );
            }
        );
}

[Serializable]
public class ReferenceToolMap : ReferenceToolBase
{
    public void FindOnlyRefAsset(
        ReferenceMap map,
        Object asset,
        bool clearResult = true
    )
    {
        var (guid, fileID) = EdUtil.GetAssetGUIDAndFileID(asset);
        var findString = $"guid: {guid}";
        curFindString = findString;
        FindFromMap(map, findString, clearResult, AssetFrom.OtherAsset);
    }

    public void FindDllComponent(ReferenceMap map, Type type)
    {
        var (guid, fileID, _) = EdUtil.GetGuidAndFileIdByType(type);
        var findString = $"fileID: {fileID}, guid: {guid}";
        curFindString = findString;
        FindFromMap(map, findString, true, AssetFrom.OtherAsset);
    }

    public void FindFromMap(
        ReferenceMap map,
        string findString,
        bool clearResult,
        AssetFrom assetFrom
    )
    {
        BeginWork("FindFromMap");
        if (clearResult)
        {
            ClearFindResult();
        }

        foreach (var v in map.mapDic)
        {
            if (v.Value.Exists(
                v1 => v1.Contains(findString) || v1.Equals(findString)
            ))
            {
                var path = AssetDatabase.GUIDToAssetPath(v.Key);
                if (path.IsNOE() || path == v.Key || path.Contains("__DELETED_GUID_Trash"))
                {
                    continue;
                }

                AddFindResult(path, assetFrom);
            }
        }

        SortResult();
        EndWork();
    }
}

[Serializable]
public class ReferenceToolString : ReferenceToolBase
{
    /// <summary>
    ///     reference https://github.com/BurntSushi/ripgrep
    /// </summary>
    public void FindString(
        string str,
        string rgoption,
        Action workDoneCB
    )
    {
        if (str.IsNOE())
        {
            return;
        }

        ClearFindResult();

        curFindString = str;

        BeginWork("FindString");

        var argsInfo = new RgArgumentsInfo(str, "-l -s -F " + rgoption);
        CommandUtil.ExecuteCommand(
            argsInfo,
            results =>
            {
                AddFindResult(
                    results.Select(PathUtil.AssetRelativePath).ToList(),
                    AssetFrom.StringFinder
                );
                SortResult();
                EndWork();
                workDoneCB?.Invoke();
            }
        );
    }
}

public static class ReplacePrefabUtil
{
    public static void ReplaceAll()
    {
    }

    public static void ReplaceOne(UFileInfo refer, GameObject oldPrefab, GameObject targetPrefab)
    {
    }
}