using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class UToolsSetting
{
    public static string dataPath => Path.Combine(PathUtil.DataPath, "../local_packages/Datas/UTools");
}