using System;
using System.Globalization;
using System.Linq;
using Odyssey.Pvp;
using UnityEngine;

public sealed partial class GameMenuUI
{
    enum PvpForm
    {
        Entry,
        Create,
        Join
    }

    const string PvpMode = "who is really odyssey？";
    const ushort DefaultPvpPort = 7777;
    static readonly Color32 PvpGold = new Color32(245, 186, 85, 255);
    static readonly Color32 PvpMuted = new Color32(222, 230, 237, 255);
    static readonly Color32 PvpWarning = new Color32(255, 218, 165, 255);
    static readonly Color32 PvpInk = new Color32(23, 33, 46, 255);
    bool pvpOpen;
    bool pvpChoosingLevel;
    PvpForm pvpForm;
    string pvpName = "";
    string pvpAddress = "";
    string pvpListenPort = "7777";
    string pvpPublicPort = "";
    string pvpJoinInvite = "";
    string pvpFormError;
    float pvpCopiedUntil;
    float pvpRenewedUntil;
    GUIStyle pvpInput;
    string pvpInputLocale;
    Texture2D pvpBoardArt, pvpCardArt, pvpTabArt, pvpCheckArt, pvpWinnerArt;

    bool PvpVisible => pvpOpen || (PvpSession.Current != null && PvpSession.Current.Phase != PvpPhase.Idle);
    bool OnlinePvp => PvpSession.Current != null && PvpSession.Current.Phase != PvpPhase.Idle;

    static string PvpText(string key) => GameLocalization.Get("pvp." + key);

    void OpenPvp()
    {
        PvpSession.GetOrCreate();
        pvpOpen = true;
        SelectPvpForm(PvpForm.Entry);
    }

    void ClosePvp()
    {
        pvpOpen = false;
        SelectPvpForm(PvpForm.Entry);
    }

    void SelectPvpForm(PvpForm form)
    {
        pvpForm = form;
        pvpChoosingLevel = false;
        pvpFormError = null;
        if (Event.current != null) GUI.FocusControl(null);
    }

    void LeavePvp()
    {
        CloseSettings();
        SelectPvpForm(PvpForm.Entry);
        PvpSession.Current?.Leave();
    }

    void HandlePvpEscape()
    {
        var session = PvpSession.Current;
        if (session == null || session.Phase == PvpPhase.Idle)
        {
            if (pvpForm == PvpForm.Entry) ClosePvp();
            else SelectPvpForm(PvpForm.Entry);
            return;
        }

        if (pvpChoosingLevel)
        {
            pvpChoosingLevel = false;
            return;
        }
        if (session.Phase == PvpPhase.Connecting) LeavePvp();
        else OpenSettings();
    }

    void DrawPvp(PvpSession session)
    {
        if (session == null) return;
        if (pvpCardArt == null)
        {
            pvpBoardArt = Load("panel_bg_2");
            pvpCardArt = Load("level_bg_1");
            pvpTabArt = Load("level_tab_1");
            pvpCheckArt = Load("panel_icon_yes");
            pvpWinnerArt = Load("panel_icon_start_1");
        }
        switch (session.Phase)
        {
            case PvpPhase.Countdown:
            case PvpPhase.Racing:
                DrawRaceHud(session);
                return;
            case PvpPhase.Waiting:
                if (pvpChoosingLevel && session.IsHost) DrawPvpLevelPicker(session);
                else DrawWaitingRoom(session);
                return;
            case PvpPhase.Loading:
                DrawPvpLoading(session);
                return;
            case PvpPhase.Results:
                DrawRaceResults(session);
                return;
            case PvpPhase.Connecting:
            case PvpPhase.Returning:
                DrawPvpTransition(session);
                return;
            default:
                DrawPvpForm(session);
                return;
        }
    }

    void BeginPvpScreen(string heading, bool entrance = false)
    {
        var matrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        var screen = new Rect(0, 0, Screen.width, Screen.height);
        var art = entrance ? homeBackground : levelSelectBackground;
        if (art != null) GUI.DrawTexture(screen, art, ScaleMode.ScaleAndCrop);
        else Fill(screen, PvpInk);
        if (entrance) Fill(screen, new Color32(11, 19, 31, 72));
        GUI.matrix = matrix;

        Text(new Rect(75, 48, 1450, 87), heading, 72, Color.white);
        Text(new Rect(79, 146, 760, 42), PvpMode, 31, PvpGold);
    }

    void DrawPvpForm(PvpSession session)
    {
        string heading = pvpForm == PvpForm.Create ? "create" : pvpForm == PvpForm.Join ? "join" : "online";
        BeginPvpScreen(PvpText(heading), entrance: true);
        if (pvpForm == PvpForm.Create) DrawPvpCreate(session);
        else if (pvpForm == PvpForm.Join) DrawPvpJoin(session);
        else
        {
            PvpButton(new Rect(75, 495, 365, 105), "create", () => SelectPvpForm(PvpForm.Create));
            PvpButton(new Rect(462, 495, 365, 105), "join", () => SelectPvpForm(PvpForm.Join), primary: false);
        }
        DrawPvpStatus(session, pvpForm != PvpForm.Entry);
        PvpButton(new Rect(75, 760, 255, 73), "menu.back", () =>
        {
            if (pvpForm == PvpForm.Entry) ClosePvp();
            else SelectPvpForm(PvpForm.Entry);
        }, primary: false);
    }

    void DrawPvpCreate(PvpSession session)
    {
        HudArt(new Rect(74, 191, 970, 565), pvpBoardArt);
        Text(new Rect(212, 253, 650, 32), PvpText("room_name"), 26, Color.white);
        pvpName = PvpField(new Rect(212, 287, 650, 51), "pvp-room", pvpName, 32);
        Text(new Rect(212, 363, 650, 30), PvpText("host_address"), 26, Color.white);
        pvpAddress = PvpField(new Rect(212, 399, 650, 51), "pvp-address", pvpAddress, 45);
        Text(new Rect(212, 454, 650, 56), PvpText("host_address_hint"), 20, PvpMuted);
        Text(new Rect(212, 516, 301, 32), PvpText("listen_port"), 25, Color.white);
        Text(new Rect(555, 516, 307, 32), PvpText("public_port"), 25, Color.white);
        pvpListenPort = PvpField(new Rect(212, 555, 307, 51), "pvp-local-port", pvpListenPort, 5);
        pvpPublicPort = PvpField(new Rect(555, 555, 307, 51), "pvp-public-port", pvpPublicPort, 5);
        PvpButton(new Rect(633, 746, 335, 96), "create", () => SubmitPvpCreate(session));
    }

    void SubmitPvpCreate(PvpSession session)
    {
        if (!TryPvpPort(pvpListenPort, DefaultPvpPort, out ushort listenPort) ||
            !TryPvpPort(pvpPublicPort, 0, out ushort publicPort))
        {
            pvpFormError = "pvp.error.port";
            return;
        }
        pvpFormError = null;
        GUI.FocusControl(null);
        string room = string.IsNullOrWhiteSpace(pvpName) ? PvpText("default_room") : pvpName.Trim();
        session.Create(room, pvpAddress.Trim(), listenPort, publicPort);
    }

    static bool TryPvpPort(string value, ushort fallback, out ushort port)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            port = fallback;
            return true;
        }
        return ushort.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port > 0;
    }

    void DrawPvpJoin(PvpSession session)
    {
        HudArt(new Rect(74, 191, 970, 565), pvpBoardArt);
        Text(new Rect(212, 342, 650, 47), PvpText("invite"), 32, Color.white);
        pvpJoinInvite = PvpField(new Rect(212, 402, 470, 58), "pvp-invite", pvpJoinInvite, 256);
        PvpButton(new Rect(701, 398, 186, 61), "paste", () =>
        {
            string clipboard = GUIUtility.systemCopyBuffer.Trim();
            pvpJoinInvite = clipboard.Length <= 256 ? clipboard : clipboard.Substring(0, 256);
            GUI.FocusControl(null);
        }, primary: false);
        PvpButton(new Rect(633, 746, 335, 96), "join", () =>
        {
            GUI.FocusControl(null);
            session.Join(pvpJoinInvite.Trim());
        }, interactable: !string.IsNullOrWhiteSpace(pvpJoinInvite));
    }

    string PvpField(Rect rect, string controlName, string value, int maxLength)
    {
        if (pvpInput == null || pvpInputLocale != GameLocalization.Current)
        {
            pvpInputLocale = GameLocalization.Current;
            pvpInput = new GUIStyle(GUI.skin.textField)
            {
                font = GameFont.Main,
                fontSize = 26,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(16, 16, 0, 0)
            };
            foreach (var state in new[] { pvpInput.normal, pvpInput.hover, pvpInput.active, pvpInput.focused })
            {
                state.background = null;
                state.textColor = Color.white;
            }
        }
        LevelPaper(rect, PvpInk, -.8f);
        LevelPaper(new Rect(rect.x + 4, rect.y + 3, rect.width - 8, rect.height - 7), new Color32(35, 49, 63, 255), -.8f);
        GUI.SetNextControlName(controlName);
        var cursor = GUI.skin.settings.cursorColor;
        GUI.skin.settings.cursorColor = Color.white;
        string current = GUI.TextField(rect, value, maxLength, pvpInput);
        GUI.skin.settings.cursorColor = cursor;
        if (GUI.GetNameOfFocusedControl() == controlName)
            Fill(new Rect(rect.x + 12, rect.yMax - 5, rect.width - 24, 2), PvpGold);
        return current;
    }

    void DrawWaitingRoom(PvpSession session)
    {
        BeginPvpScreen(session.RoomName);
        DrawPvpSeats(session, loadingSeats: false);
        Text(new Rect(550, 203, 685, 45), PvpLevelName(session.SelectedScene), 28, PvpGold, true);
        if (session.IsHost)
        {
            PvpButton(new Rect(1260, 178, 265, 64), "choose_level", () => pvpChoosingLevel = true, primary: false);
            PvpButton(new Rect(75, 599, 245, 83), session.HasTestBot ? "remove_bot" : "add_bot", () =>
            {
                if (session.HasTestBot) session.RemoveTestBot();
                else session.AddTestBot();
            }, primary: false, interactable: session.HasTestBot || session.CanAddTestBot);
            Text(new Rect(338, 598, 538, 44), PvpText("invite_hint"), 23, PvpMuted);
            Text(new Rect(338, 642, 538, 37), PvpText("invite_endpoint") + " " + session.AdvertisedEndpoint,
                22, Color.white);
            string inviteAction = Time.unscaledTime < pvpCopiedUntil ? "copied" : "copy";
            PvpButton(new Rect(890, 599, 275, 83), inviteAction, () =>
            {
                GUIUtility.systemCopyBuffer = session.Invite;
                pvpCopiedUntil = Time.unscaledTime + 2f;
            }, primary: false, interactable: !string.IsNullOrEmpty(session.Invite));
            string renewAction = Time.unscaledTime < pvpRenewedUntil ? "invite_renewed" : "renew_invite";
            PvpButton(new Rect(1180, 599, 270, 83), renewAction, () =>
            {
                if (!session.RegenerateInvite())
                    return;
                pvpCopiedUntil = 0;
                pvpRenewedUntil = Time.unscaledTime + 2;
            }, primary: false, interactable: !string.IsNullOrEmpty(session.Invite));
            bool canStart = session.Members.Count >= 2 && session.Members.All(member =>
                !member.Disconnected && (member.IsHost || member.IsBot || member.Ready));
            PvpButton(new Rect(1190, 746, 335, 96), "start", session.StartMatch, interactable: canStart);
        }
        else
        {
            var local = session.Members.FirstOrDefault(member => member.Id == session.LocalId);
            bool ready = local != null && local.Ready;
            PvpButton(new Rect(1190, 746, 335, 96), ready ? "cancel_ready" : "ready", () => session.SetReady(!ready),
                interactable: local != null);
        }
        DrawPvpStatus(session);
        PvpButton(new Rect(75, 760, 255, 73), "leave", LeavePvp, primary: false);
    }

    static string PvpLevelName(string scene)
    {
        var level = PvpLevelCatalog.Levels.FirstOrDefault(candidate => candidate.SceneName == scene);
        return level == null ? scene : GameLocalization.Get(level.TitleKey);
    }

    void DrawPvpLevelPicker(PvpSession session)
    {
        BeginPvpScreen(PvpText("choose_level"));
        Text(new Rect(79, 190, 1440, 35), PvpText("choose_level_hint"), 23, PvpMuted);
        int index = 0;
        foreach (var level in PvpLevelCatalog.Levels)
        {
            var card = new Rect(75 + index % 3 * 495, 240 + index / 3 * 122, 460, 108);
            bool selected = level.SceneName == session.SelectedScene;
            HudArt(card, homeButtons[selected ? 0 : 1]);
            string number = level.Number == 0 ? PvpText("tutorial") :
                GameLocalization.Format("level.number", "number", level.Number.ToString("00"));
            PvpAngledText(new Rect(card.x + 40, card.y + 8, card.width - 80, 28),
                selected ? number + " · " + PvpText("level_selected") : number,
                20, selected ? Color.white : PvpInk, -3);
            PvpAngledText(new Rect(card.x + 40, card.y + 36, card.width - 80, 48),
                GameLocalization.Get(level.TitleKey), 28, selected ? Color.white : PvpInk, -3);
            if (FeedbackButton(card, GUIContent.none, GUIStyle.none, keyboard: true))
            {
                Sound();
                if (session.SelectLevel(level.SceneName)) pvpChoosingLevel = false;
            }
            index++;
        }
        PvpButton(new Rect(75, 760, 255, 73), "menu.back", () => pvpChoosingLevel = false, primary: false);
    }

    void DrawPvpLoading(PvpSession session)
    {
        BeginPvpScreen(PvpText("loading"));
        DrawPvpSeats(session, loadingSeats: true);
        Text(new Rect(550, 203, 975, 45), PvpLevelName(session.SelectedScene), 28, PvpGold, true);
        int loaded = session.Members.Count(member => member.Loaded);
        string loadingStatus = GameLocalization.Format("pvp.loaded_count", "loaded", loaded)
            .Replace("{count}", session.Members.Count.ToString(CultureInfo.InvariantCulture));
        Text(new Rect(400, 618, 800, 55), loadingStatus, 32, PvpGold, true);
        DrawPvpStatus(session);
        PvpButton(new Rect(75, 760, 255, 73), "leave", LeavePvp, primary: false);
    }

    static Rect PvpCardRect(int index, int count)
    {
        const float width = 330;
        const float spacing = 365;
        float left = (1600 - width - (count - 1) * spacing) * .5f;
        return new Rect(left + index * spacing, 292, width, 286);
    }

    void DrawPvpCardFace(Rect card, string name, bool local)
    {
        HudArt(card, pvpCardArt);
        var tab = new Rect(card.x + 40, card.y - 48, 250, 120);
        HudArt(tab, pvpTabArt);
        PvpAngledText(new Rect(tab.x + 13, tab.y + 22, tab.width - 26, 73), name, 30, local ? PvpGold : Color.white, -5);
    }

    void DrawPvpSeats(PvpSession session, bool loadingSeats)
    {
        Text(new Rect(75, 205, 520, 44), GameLocalization.Format("pvp.seats", "count", session.Members.Count),
            29, Color.white);
        for (int index = 0; index < 4; index++)
        {
            Rect card = PvpCardRect(index, 4);
            var member = index < session.Members.Count ? session.Members[index] : null;
            if (member == null)
            {
                var color = GUI.color;
                GUI.color = new Color(color.r * .69f, color.g * .77f, color.b * .86f, color.a);
                HudArt(card, pvpCardArt);
                GUI.color = color;
                Text(new Rect(card.x + 36, card.y + 110, 258, 65), PvpText("available"), 32, PvpMuted, true);
                continue;
            }

            bool local = member.Id == session.LocalId;
            bool active = loadingSeats ? member.Loaded : member.Ready;
            DrawPvpCardFace(card, PvpMemberName(member, local), local);
            if (member.IsBot)
                Text(new Rect(card.x + 35, card.y + 89, 260, 52), PvpText("bot_unranked"), 23, PvpMuted, true);
            else if (active && !member.Disconnected)
                HudArt(new Rect(card.center.x - 38, card.y + 94, 76, 52), pvpCheckArt);
            var state = new Rect(card.x + 54, card.y + 155, 222, 64);
            var stateArt = member.Disconnected ? homeButtons[2] : active ? homeButtons[0] : hudPlate;
            HudArt(state, stateArt);
            PvpAngledText(new Rect(state.x + 10, state.y + 2, state.width - 20, state.height - 8),
                PvpSeatStatus(member, loadingSeats), 24, active ? Color.white : PvpGold, -3);
        }
    }

    static string PvpMemberName(PvpMember member, bool local)
    {
        string name = member.IsBot ? PvpText("bot_name") : member.IsHost ? PvpText("host") :
            GameLocalization.Format("pvp.player", "number", member.Id);
        return local ? GameLocalization.Format("pvp.you", "name", name) : name;
    }

    static string PvpSeatStatus(PvpMember member, bool loadingSeats)
    {
        if (member.Disconnected) return PvpText("disconnected");
        if (loadingSeats) return PvpText(member.Loaded ? "loaded" : "loading");
        if (member.IsBot) return PvpText("bot_stationary");
        return PvpText(member.Ready ? "ready" : "not_ready");
    }

    void DrawRaceHud(PvpSession session)
    {
        HudArt(new Rect(32, 34, 332, 104), hudPlate);
        Text(new Rect(55, 53, 283, 41), PvpTime(session.Elapsed), 34, Color.white, true);
        int finished = session.Members.Count(member => !member.IsBot && member.Rank > 0);
        string progress = GameLocalization.Format("pvp.finished_count", "finished", finished)
            .Replace("{count}", session.Members.Count(member => !member.IsBot).ToString(CultureInfo.InvariantCulture));
        Text(new Rect(55, 94, 283, 29), progress, 20, PvpGold, true);
        Text(new Rect(1210, 35, 265, 55), PvpText("options_hint"), 21, Color.white, true);
        var options = new Rect(1475, 31, 90, 90);
        HudArt(options, hudSettings);
        if (FeedbackButton(options, GUIContent.none, GUIStyle.none)) OpenSettings();

        if (session.Phase == PvpPhase.Countdown)
        {
            var signal = new Rect(710, 353, 180, 156);
            HudArt(signal, pvpCardArt);
            Text(new Rect(signal.x + 19, signal.y + 9, signal.width - 38, signal.height - 25),
                session.Countdown.ToString(CultureInfo.InvariantCulture), 88, Color.white, true);
            return;
        }
        var winner = session.Members.FirstOrDefault(member => !member.IsBot && member.Rank == 1);
        if (winner == null) return;
        var local = session.Members.FirstOrDefault(member => member.Id == session.LocalId);
        HudArt(new Rect(554, 23, 492, 155), hudPlate);
        Text(new Rect(580, 55, 440, 43), GameLocalization.Format("pvp.winner", "name", PvpMemberName(winner, false)),
            30, PvpGold, true);
        if (local != null && local.Rank > 0)
            Text(new Rect(580, 98, 440, 37), PvpText("wait_finish"), 21, PvpMuted, true);
    }

    void DrawRaceResults(PvpSession session)
    {
        BeginPvpScreen(PvpText("results"));
        Text(new Rect(75, 205, 1450, 44), PvpLevelName(session.SelectedScene), 29, PvpGold);
        var ordered = session.Members.Where(member => !member.IsBot)
            .OrderBy(member => member.Rank > 0 ? member.Rank : int.MaxValue).ToArray();
        for (int index = 0; index < ordered.Length; index++)
        {
            var member = ordered[index];
            bool local = session.LocalId == member.Id;
            var card = PvpCardRect(index, ordered.Length);
            DrawPvpCardFace(card, PvpMemberName(member, local), local);
            string rank = member.Rank > 0 ? "#" + member.Rank.ToString(CultureInfo.InvariantCulture) : "–";
            if (member.Rank == 1)
                HudArt(new Rect(card.x + 74, card.y + 79, 55, 54), pvpWinnerArt);
            Text(new Rect(card.x + 50, card.y + 71, 230, 76), rank, 55, member.Rank == 1 ? PvpGold : Color.white, true);
            string time = member.Rank > 0 ? PvpTime(member.FinishTime, precise: true) : PvpText("unfinished");
            Text(new Rect(card.x + 37, card.y + 162, 256, 61), time, 34, Color.white, true);
        }
        if (session.IsHost)
            PvpButton(new Rect(1190, 746, 335, 96), "return", session.ReturnToRoom);
        else
            Text(new Rect(430, 755, 740, 68), PvpText("wait_host"), 26, PvpMuted, true);
        PvpButton(new Rect(75, 760, 255, 73), "leave", LeavePvp, primary: false);
    }

    static string PvpTime(double seconds, bool precise = false)
    {
        int value = Mathf.Max(0, (int)seconds);
        if (precise)
        {
            int hundredths = Mathf.Clamp((int)((seconds - value) * 100), 0, 99);
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}.{2:00}", value / 60, value % 60, hundredths);
        }
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", value / 60, value % 60);
    }

    void DrawPvpTransition(PvpSession session)
    {
        bool connecting = session.Phase == PvpPhase.Connecting;
        BeginPvpScreen(PvpText(connecting ? "connecting" : "returning"), entrance: connecting);
        DrawPvpStatus(session);
        if (connecting) PvpButton(new Rect(75, 760, 255, 73), "leave", LeavePvp, primary: false);
    }

    void DrawPvpStatus(PvpSession session, bool inForm = false)
    {
        string key = pvpFormError ?? session.StatusKey;
        if (string.IsNullOrEmpty(key)) return;
        var rect = inForm ? new Rect(214, 622, 648, 69) : new Rect(350, 683, 900, 54);
        Text(new Rect(rect.x + 1, rect.y + 2, rect.width, rect.height), GameLocalization.Get(key), 23, PvpInk, !inForm);
        Text(rect, GameLocalization.Get(key), 23, PvpWarning, !inForm);
    }

    void PvpButton(Rect rect, string key, Action action, bool primary = true, bool interactable = true)
    {
        bool exit = key == "leave" || key == "menu.back";
        var art = interactable ? homeButtons[primary ? 0 : exit ? 2 : 1] : hudPlate;
        bool previouslyEnabled = GUI.enabled;
        GUI.enabled = previouslyEnabled && interactable;
        HudArt(rect, art);
        if (FeedbackButton(rect, GUIContent.none, GUIStyle.none, keyboard: GUI.enabled))
        {
            Sound();
            action();
        }
        GUI.enabled = previouslyEnabled;
        string caption = key.StartsWith("menu.", StringComparison.Ordinal) ? GameLocalization.Get(key) : PvpText(key);
        var label = new Rect(rect.x + rect.width * .095f, rect.y + rect.height * .05f, rect.width * .81f, rect.height * .78f);
        PvpAngledText(label, caption, rect.height >= 88 ? 34 : 26,
            !interactable ? PvpMuted : !primary && !exit ? PvpInk : Color.white, -3);
    }

    void PvpAngledText(Rect rect, string value, int size, Color color, float angle)
    {
        var matrix = GUI.matrix;
        RotateInCanvas(angle, rect.center);
        Text(rect, value, size, color, true);
        GUI.matrix = matrix;
    }
}
