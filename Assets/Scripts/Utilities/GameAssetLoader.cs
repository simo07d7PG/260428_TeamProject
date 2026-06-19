using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources 폴더의 ScriptableObject를 로드합니다.
/// 에디터·빌드 모두 동일하게 동작합니다.
/// </summary>
public static class GameAssetLoader
{
    public static void LoadAll<T>(IReadOnlyList<string> resourcesPaths, List<T> destination) where T : Object
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
    }
}