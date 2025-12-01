using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerService : Singleton<SceneManagerService>
{
    // 简单封装异步加载场景
    public AsyncOperation LoadSceneAsync(string sceneName)
    {
        return SceneManager.LoadSceneAsync(sceneName);
    }
}
