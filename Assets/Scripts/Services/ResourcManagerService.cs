// ...existing code...
using UnityEngine;

public class ResourceManagerService : Singleton<ResourceManagerService>
{
    public T LoadResource<T>(string path)
        where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }
}
// ...existing code...
