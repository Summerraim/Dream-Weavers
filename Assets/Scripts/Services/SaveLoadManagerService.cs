using System;
using UnityEngine;

public class SaveLoadManagerService : Singleton<SaveLoadManagerService>
{
    private const string DefaultSaveKey = "SaveData";

    // 判断是否存在存档（无参数调用时使用默认键）
    public bool HasSaveData(string key = DefaultSaveKey)
    {
        return PlayerPrefs.HasKey(key);
    }

    // 保存任意可序列化的数据（使用 JsonUtility）
    public void Save<T>(T data, string key = DefaultSaveKey) where T : class
    {
        if (data == null) return;
        try
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e}");
        }
    }

    // 读取数据（若无返回 null）
    public T Load<T>(string key = DefaultSaveKey) where T : class
    {
        if (!HasSaveData(key)) return null;
        try
        {
            string json = PlayerPrefs.GetString(key);
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Load failed: {e}");
            return null;
        }
    }

    // 删除指定存档
    public void DeleteSave(string key = DefaultSaveKey)
    {
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
