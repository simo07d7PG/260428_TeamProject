using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 게임 진행(코인, 일차)을 JSON으로 저장·로드합니다.
/// 해금 레시피는 일차로부터 파생되므로 별도 저장하지 않습니다.
/// </summary>
public static class SaveLoadUtility
{
    const string FileName = "cafesim_save.json";

    [Serializable]
    class SaveData
    {
        public int coin;
        public int day = 1;
        public int totalServed;
    }

    static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void Save(GameManager gameManager, int totalServed = 0)
    {
        if (gameManager == null)
            return;

        SaveData data = new SaveData
        {
            coin = gameManager.Coin,
            day = gameManager.CurrentDay,
            totalServed = totalServed
        };

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveLoadUtility] 저장 실패: {e.Message}");
        }
    }

    public static void LoadInto(GameManager gameManager)
    {
        if (gameManager == null || !HasSave())
            return;

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null)
                return;

            gameManager.Coin = data.coin;
            gameManager.CurrentDay = Mathf.Max(1, data.day);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveLoadUtility] 로드 실패: {e.Message}");
        }
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveLoadUtility] 삭제 실패: {e.Message}");
        }
    }
}
