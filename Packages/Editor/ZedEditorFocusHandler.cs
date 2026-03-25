using UnityEditor;

namespace HFS.ZedEditor
{
    [InitializeOnLoad]
    public static class ZedEditorFocusHandler
    {
        static ZedEditorFocusHandler()
        {
            EditorApplication.focusChanged += OnFocusChanged;
        }

        private static void OnFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
