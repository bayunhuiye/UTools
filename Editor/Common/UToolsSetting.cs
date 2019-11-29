using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class UToolsSetting
{
    public static string packagePath => Path.Combine(PathUtil.DataPath, "../Packages/com.maid.utools");
    public static string dataPath => Path.Combine(PathUtil.DataPath, "../Packages/Datas/UTools");
}