using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


public enum FindThingType
{
    Assets,
    CustomStr,
    BuiltinComponent,
}

public enum FindAssetsMode
{
    Selection,
    Field,
}

public enum AssetFrom
{
    StringFinder,
    OtherAsset,
    Resources,
}

public enum RefType
{
    Asset,
    String,
}

public enum WhenShouldFind
{
    ClickFind,
    SelectionChange,
}

[Serializable]
public class ReplaceInfo
{
    public UFileInfo FileInfo;

    public string oldValue;
    public string newValue;

    //记录替换的所有guid在文本内的startIndex,以便于准确的还原replace
    public List<int> StartIndexs = new List<int>();
}

[Serializable]
public class UFileInfo
{
    public string relativePath;
    public string absolutePath;
    public string fileName;
    public string extension;
    public bool isWaiting;
    public AssetFrom assetFrom;

    [NonSerialized] private Object _asset;

    public Object asset => _asset != null
        ? _asset
        : _asset = AssetDatabase.LoadAssetAtPath(relativePath, assetType);

    [NonSerialized] private Type _assetType;

    public Type assetType => _assetType != null
        ? _assetType
        : _assetType = ReferenceToolUtil.GetTypeByExtension(extension);

    public UFileInfo(string relativePath)
    {
        this.relativePath = relativePath;
        absolutePath = PathUtil.AssetAbsolutePath(relativePath);
        extension = Path.GetExtension(relativePath);
        fileName = Path.GetFileNameWithoutExtension(relativePath);
    }
}

public struct ChangeLog
{
    public string type;
    public string guid;
    public string rpath;

    public ChangeLog(
        string type,
        string guid,
        string rpath
    )
    {
        this.type = type;
        this.guid = guid;
        this.rpath = rpath;
    }

    public override string ToString() => $"{type},{guid},{rpath}";
}