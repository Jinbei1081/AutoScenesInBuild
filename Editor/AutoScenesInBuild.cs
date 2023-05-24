using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// シーンを Build Settings の Scenes In Build へ自動で追加・削除するエディタ拡張
/// </summary>
internal sealed class AutoScenesInBuild
    : AssetPostprocessor,
        IPreprocessBuildWithReport
{
    //==============================================================================
    // プロパティ
    //==============================================================================
    public int callbackOrder => 0;

    //==============================================================================
    // 関数
    //==============================================================================
    /// <summary>
    /// ビルドを開始した時に呼び出されます
    /// </summary>
    void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
    {
        Remove();
    }

    //==============================================================================
    // 関数(static)
    //==============================================================================
    /// <summary>
    /// アセットがインポートされた時などに呼び出されます
    /// </summary>
    private static void OnPostprocessAllAssets
    (
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths
    )
    {
        const string path = "ProjectSettings/EditorBuildSettings.asset";

        bool isImportedScenes =
            !ArrayUtility.Contains(importedAssets, path);

        bool isDeletedScenes =
            ArrayUtility.Contains(importedAssets, path) ||
            deletedAssets.Any(x => x.EndsWith(".unity")
            );

        if (!(isImportedScenes || isDeletedScenes)) return;

        Add(importedAssets);
        Remove();
    }

    /// <summary>
    /// シーンを削除
    /// </summary>
    private static void Remove()
    {
        // Build Settings では Deleted なシーンかどうかは File.Exists で確認している
        // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/BuildPlayerSceneTreeView.cs
        var newScenes = EditorBuildSettings.scenes
                .Where(x => File.Exists(x.path))
                .ToArray()
            ;

        if (newScenes.Length == EditorBuildSettings.scenes.Length) return;

        EditorBuildSettings.scenes = newScenes;
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// シーンを追加
    /// </summary>
    private static void Add(string[] importedAssets)
    {
        var newScenes = new List<EditorBuildSettingsScene>();

        // 追加したシーンをEditorBuildSettingsSceneに詰める
        foreach (var importedAsset in importedAssets)
        {
            newScenes.Add(new EditorBuildSettingsScene(importedAsset, true));
        }

        // 既存のシーンを先頭に追加
        newScenes.InsertRange(0, EditorBuildSettings.scenes
            .Where(x => File.Exists(x.path))
            .ToArray());

        // 重複しているシーンを削除
        // https://atmarkit.itmedia.co.jp/ait/articles/1703/29/news027.html
        newScenes = newScenes
            .Distinct()
            .GroupBy(x => x.path)
            .Select(group => group.Last())
            .ToList();
        newScenes = newScenes
            .FindAll(x => x.path.EndsWith(".unity"));

        EditorBuildSettings.scenes = newScenes.ToArray();
        AssetDatabase.SaveAssets();
    }
}