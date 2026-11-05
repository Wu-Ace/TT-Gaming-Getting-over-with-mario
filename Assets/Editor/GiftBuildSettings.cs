using UnityEditor;
namespace LiveGifts.Editor
{
    public static class GiftBuildSettings
    {
        const string Menu = "Tools/Live Gifts/Include Keyboard Tests In Build";
        const string Symbol = "GIFT_TESTING";
        [MenuItem(Menu)]
        static void Toggle()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var symbols = new System.Collections.Generic.List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';'));
            if (symbols.Contains(Symbol)) symbols.RemoveAll(s => s == Symbol);
            else symbols.Add(Symbol);
            symbols.RemoveAll(string.IsNullOrWhiteSpace);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", symbols));
        }
        [MenuItem(Menu, true)]
        static bool Validate()
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';');
            UnityEditor.Menu.SetChecked(Menu, System.Array.IndexOf(symbols, Symbol) >= 0);
            return !EditorApplication.isCompiling;
        }
    }
}
