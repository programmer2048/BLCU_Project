using UnityEngine;

public class ResourceLoader : MonoBehaviour
{
    public static T Load<T>(string path) where T : Object
    {
        return Resources.Load<T>(path);
    }
}
