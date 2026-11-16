using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odyssey.Pvp
{
    public sealed class PvpLevelInfo
    {
        public string SceneName { get; }
        public int Number { get; }
        public string TitleKey { get; }
        public PvpLevelInfo(string scene, int number, string title) { SceneName = scene; Number = number; TitleKey = title; }
    }

    public static class PvpLevelCatalog
    {
        private static readonly PvpLevelInfo[] entries = Build();
        public static IReadOnlyList<PvpLevelInfo> Levels => entries;
        private static PvpLevelInfo[] Build()
        {
            var levels = new List<PvpLevelInfo> { new PvpLevelInfo("Tutorial", 0, "pvp.tutorial") };
            for (int i = 0; i < GameMenuUI.Scenes.Length; i++)
                levels.Add(new PvpLevelInfo(GameMenuUI.Scenes[i], i + 1, "level.name." + (i + 1)));
            return levels.ToArray();
        }
        public static bool Contains(string scene) => Array.Exists(entries, level => level.SceneName == scene);
        public static bool IsAvailable(string scene) => Contains(scene) && Application.CanStreamedLevelBeLoaded(scene);
        public static bool IsPvpScene(string scene)
        {
            var session = PvpSession.Current;
            return session && session.SelectedScene == scene && session.Phase >= PvpPhase.Loading && session.Phase <= PvpPhase.Results;
        }
    }
}
