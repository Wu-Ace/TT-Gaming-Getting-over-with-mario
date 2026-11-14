using UnityEditor;

[InitializeOnLoad]
public static class LocalizationResourceCleanup
{
    static LocalizationResourceCleanup()
    {
        AssemblyReloadEvents.beforeAssemblyReload += LocalizedWindowsText.Release;
        EditorApplication.quitting += LocalizedWindowsText.Release;
    }
}
