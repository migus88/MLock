using Migs.MLock.Debugging;
using UnityEditor;

namespace Migs.MLock.Editor.DebugWindow
{
    /// <summary>
    /// Clears the debug registry when Play mode exits, so services from the finished session are not listed
    /// </summary>
    [InitializeOnLoad]
    internal static class DebugRegistryPlayModeReset
    {
        static DebugRegistryPlayModeReset()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                DebugDataHandler.Reset();
            }
        }
    }
}
