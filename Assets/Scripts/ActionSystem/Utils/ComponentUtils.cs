using UnityEngine;

namespace ActionSystem
{
    public static class ComponentUtils
    {
        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
    }
}
