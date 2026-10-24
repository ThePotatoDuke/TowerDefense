using UnityEngine;

public static class ComponentExtensions
{
    // Returns the existing component, or adds one (e.g. visual FX not set up on the prefab)
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
    }
}
