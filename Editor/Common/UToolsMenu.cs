using UnityEditor;
using UnityEngine;

public static class UToolsMenu
{
    [MenuItem("Window/UTools/" + nameof(BookmarkTool))]
    public static void BookmarkTool() => openWindow<BookmarkTool>();

    [MenuItem("Window/UTools/" + nameof(FindStringTool))]
    public static void FindStringTool() => openWindow<FindStringTool>();

    [MenuItem("Window/UTools/" + nameof(ReferenceTool))]
    public static void ReferenceTool()
    {
        var win = openWindow<ReferenceTool>();
        win.minSize = new Vector2(400, 0);
    }

    [MenuItem("Assets/Create/UTools/TexImportSettings", priority = 1)]
    public static void createTextureImporterSetting() =>
        EdUtil.CreateAssetInSelectionPath<TexImporterSetting>("TexMaidImporterSetting");

    private static T openWindow<T>() where T : EditorWindow
    {
        var win = EditorWindow.GetWindow<T>();
        win.Show();
        return win;
    }
}