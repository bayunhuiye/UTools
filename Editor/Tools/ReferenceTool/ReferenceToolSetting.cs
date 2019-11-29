public class ReferenceToolSetting
{
    public static string ripgrepPath;
    public static string assetChangeLogPath => UToolsSetting.dataPath + "/assetChangeLogPath.txt";
    public static string guidMapPath => UToolsSetting.dataPath + "/guidMap.json";

    public static void Initialize()
    {
#if UNITY_EDITOR_OSX
        ripgrepPath = "/usr/local/bin/rg";
#elif UNITY_EDITOR
        ripgrepPath = UToolsSetting.packagePath + "/Editor/Tools/ReferenceTool/Deps/rg.exe";
#endif
    }
}