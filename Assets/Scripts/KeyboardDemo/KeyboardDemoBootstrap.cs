using UnityEngine;

namespace AndroidXR.KeyboardDemo
{
    public static class KeyboardDemoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateDemo()
        {
            if (Object.FindFirstObjectByType<KeyboardDemoController>() != null)
            {
                return;
            }

            var root = new GameObject("Keyboard Demo Runtime");
            root.AddComponent<KeyboardDemoController>();
        }
    }
}
