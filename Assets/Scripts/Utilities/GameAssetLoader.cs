using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ScriptableObject 에셋을 프로젝트 실제 경로 기준으로 로드합니다.
/// 에디터: Assets/ScriptableObject/... | 빌드: Assets/Resources/... 동일 상대 경로
/// </summary>
public static class GameAssetLoader
{
    public static void LoadAll<T>(
        IReadOnlyList<string> editorFolderPaths,
        IReadOnlyList<string> resourcesPaths,
        List<T> destination) where T : Object
    {
        HashSet<T> loaded = new HashSet<T>();

        foreach (string path in resourcesPaths)
        {
            T[] assets = Resources.LoadAll<T>(path);
            foreach (T asset in assets)
            {
                if (asset == null || !loaded.Add(asset))
                    continue;

                destination.Add(asset);
            }
        }

#if UNITY_EDITOR
        foreach (string folderPath in editorFolderPaths)
        {
            foreach (T asset in LoadFromEditorFolder<T>(folderPath))
            {
                if (asset == null || !loaded.Add(asset))
                    continue;

                destination.Add(asset);
            }
        }
#endif
    }

#if UNITY_EDITOR
    static IEnumerable<T> LoadFromEditorFolder<T>(string folderPath) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                yield return asset;
        }
    }
#endif
}