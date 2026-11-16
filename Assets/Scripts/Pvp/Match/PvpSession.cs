using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Odyssey.Pvp
{
    public enum PvpPhase { Idle, Connecting, Waiting, Loading, Countdown, Racing, Results, Returning }

    public sealed class PvpMember
    {
        public ulong Id { get; internal set; }
        public string Name { get; internal set; }
        public bool Ready { get; internal set; }
        public bool Loaded { get; internal set; }
        public bool IsHost { get; internal set; }
        public bool IsBot { get; internal set; }
        public bool Disconnected { get; internal set; }
        public int Rank { get; internal set; }
        public float FinishTime { get; internal set; }
    }

    public sealed class PvpSession : MonoBehaviour
    {
        private enum Message : byte { Room, Ready, Loaded, Input, Snapshot, LoadFailed, WorldSnapshot }

        private sealed class Racer
        {
            public PvpActor Actor;
            public Vector2 Input;
            public Vector2 PreviousCenter;
            public double InputAt;
            public uint Sequence;
            public float[] Pose;
            public float[] Target;
            public double PoseAt;
            public int RenderedSpawnRevision;
        }

        private const string MenuScene = "MainMenu";
        private const int SeatCount = 4;
        private const ulong TestBotId = ulong.MaxValue - 1;
        private const double SnapshotInterval = 1.0 / 25;
        private const double InputInterval = 1.0 / 30;
        private const double InputTimeout = .5;
        private const int MaxRoomName = 32;
        private const double OnlineRequestTimeout = 20;

        private readonly List<PvpMember> members = new List<PvpMember>(SeatCount);
        private readonly Dictionary<ulong, Racer> racers = new Dictionary<ulong, Racer>();
        private readonly List<PvpActor> contactActors = new List<PvpActor>(SeatCount);
        private readonly List<(PvpMember Member, Racer Racer, float Arrival)> arrivals =
            new List<(PvpMember, Racer, float)>(SeatCount);
        private readonly PvpContact contacts = new PvpContact();
        private PvpTransport transport;
        private CancellationTokenSource onlineRequest;
        private double onlineDeadline;
        private PvpArena arena;
        private AsyncOperation load;
        private string desiredScene = MenuScene;
        private bool drivingScene;
        private int round;
        private int selectionRevision;
        private int acknowledgedRound = -1;
        private int failedLoadRound = -1;
        private double loadDeadline;
        private double startsAt;
        private double nextInput;
        private double nextSnapshot;
        private double nextWorldSnapshot;
        private double clockReceived;
        private double elapsedAtSnapshot;
        private uint inputSequence;
        private uint snapshotSequence;
        private uint lastSnapshot;

        public static PvpSession Current { get; private set; }
        public PvpPhase Phase { get; private set; }
        public string RoomName { get; private set; } = "";
        public string SelectedScene { get; private set; } = "Tutorial";
        public bool HasTestBot => members.Any(member => member.IsBot);
        public bool CanAddTestBot => IsHost && Phase == PvpPhase.Waiting && !HasTestBot &&
            members.Count < SeatCount && transport.CanReserveSeat;
        public string Invite => transport.Invite;
        public string AdvertisedEndpoint => transport.AdvertisedEndpoint;
        public bool IsHost => transport.IsHost;
        public bool IsRelay => transport.IsRelay;
        public bool IsCreatingOnline { get; private set; }
        public ulong LocalId => transport.LocalId;
        public string StatusKey { get; private set; } = "";
        public IReadOnlyList<PvpMember> Members => members;
        public double Elapsed => Phase == PvpPhase.Racing && !IsHost
            ? elapsedAtSnapshot + Math.Max(0, Now - clockReceived)
            : elapsedAtSnapshot;
        public int Countdown { get; private set; }
        public bool ControlsBlocked { get; set; }

        private static double Now => Time.unscaledTimeAsDouble;

        public static PvpSession GetOrCreate()
        {
            if (Current)
                return Current;
            return new GameObject("PvP Session").AddComponent<PvpSession>();
        }

        private void Awake()
        {
            Current = this;
            DontDestroyOnLoad(gameObject);
            transport = gameObject.AddComponent<PvpTransport>();
            transport.Connected += OnConnected;
            transport.Disconnected += OnDisconnected;
            transport.Failed += OnTransportFailed;
            transport.MessageReceived += OnMessage;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            CancelOnlineRequest();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (transport)
            {
                transport.Connected -= OnConnected;
                transport.Disconnected -= OnDisconnected;
                transport.Failed -= OnTransportFailed;
                transport.MessageReceived -= OnMessage;
            }
            if (Current == this)
                Current = null;
        }

        public void Create(string roomName, string address, ushort localPort, ushort publicPort)
        {
            if (Phase != PvpPhase.Idle || !PrepareRoomName(roomName))
                return;
            if (transport.Host(address, localPort, publicPort))
                EnterHostedRoom();
        }

        public void CreateOnline(string roomName)
        {
            if (Phase != PvpPhase.Idle || !PrepareRoomName(roomName))
                return;
            BeginOnlineRequest(null);
        }

        public void Join(string invite)
        {
            if (Phase != PvpPhase.Idle)
                return;
            invite = (invite ?? "").Trim();
            if (invite.StartsWith("ODP", StringComparison.Ordinal) && invite.Contains('-'))
            {
                StatusKey = "";
                Phase = PvpPhase.Connecting;
                transport.Join(invite);
                return;
            }

            string roomCode = invite.ToUpperInvariant();
            if (!PvpRelay.IsRoomCode(roomCode))
            {
                StatusKey = "pvp.error.room_code";
                return;
            }
            BeginOnlineRequest(roomCode);
        }

        private bool PrepareRoomName(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > MaxRoomName || name.Any(char.IsControl))
            {
                StatusKey = "pvp.error.room_name";
                return false;
            }
            RoomName = name;
            StatusKey = "";
            return true;
        }

        private void EnterHostedRoom()
        {
            Phase = PvpPhase.Waiting;
            AddMember(transport.ServerId);
        }

        private void BeginOnlineRequest(string roomCode)
        {
            if (!PvpRelay.IsConfigured)
            {
                StatusKey = "pvp.error.relay_not_configured";
                return;
            }

            StatusKey = "";
            Phase = PvpPhase.Connecting;
            IsCreatingOnline = roomCode == null;
            onlineDeadline = Now + OnlineRequestTimeout;
            onlineRequest = new CancellationTokenSource();
            _ = ConnectOnlineAsync(roomCode, onlineRequest);
        }

        private async Task ConnectOnlineAsync(string roomCode, CancellationTokenSource request)
        {
            try
            {
                var allocation = await PvpRelay.RequestAsync(roomCode, request.Token);
                if (request.IsCancellationRequested)
                    return;

                onlineRequest = null;
                IsCreatingOnline = false;
                if (roomCode != null)
                    transport.JoinRelay(allocation.Data);
                else if (transport.HostRelay(allocation.Code, allocation.Data))
                    EnterHostedRoom();
            }
            catch (Exception error)
            {
                if (!request.IsCancellationRequested)
                    OnTransportFailed(PvpRelay.ErrorKey(error, roomCode != null));
            }
            finally
            {
                request.Dispose();
            }
        }

        private void CancelOnlineRequest()
        {
            var request = onlineRequest;
            onlineRequest = null;
            IsCreatingOnline = false;
            request?.Cancel();
        }

        public void Leave()
        {
            StatusKey = "";
            ResetSession();
        }

        public void SetReady(bool ready)
        {
            if (Phase != PvpPhase.Waiting || IsHost || !transport.IsConnected)
                return;
            transport.Send(transport.ServerId, Pack(Message.Ready, writer =>
            {
                writer.Write(selectionRevision);
                writer.Write(ready);
            }));
        }

        public bool RegenerateInvite() =>
            IsHost && Phase == PvpPhase.Waiting && transport.RegenerateInvite();

        public bool AddTestBot()
        {
            if (!CanAddTestBot || !transport.TryReserveSeat())
                return false;
            members.Add(new PvpMember { Id = TestBotId, Name = "Test Bot", IsBot = true, Ready = true });
            PublishRoom();
            return true;
        }

        public bool RemoveTestBot()
        {
            if (!IsHost || Phase != PvpPhase.Waiting || !HasTestBot)
                return false;
            members.RemoveAll(member => member.IsBot);
            transport.ReleaseReservedSeat();
            PublishRoom();
            return true;
        }

        public bool SelectLevel(string scene)
        {
            if (!IsHost || Phase != PvpPhase.Waiting || load != null ||
                !PvpLevelCatalog.Levels.Any(level => level.SceneName == scene) ||
                !Application.CanStreamedLevelBeLoaded(scene))
                return false;
            if (SelectedScene == scene)
                return true;
            SelectedScene = scene;
            selectionRevision++;
            StatusKey = "";
            foreach (var member in members)
                member.Ready = member.IsHost || member.IsBot;
            PublishRoom();
            return true;
        }

        public void StartMatch()
        {
            if (!IsHost || Phase != PvpPhase.Waiting || load != null || members.Count < 2 ||
                members.Any(member => !member.IsHost && !member.Ready))
                return;
            if (!Application.CanStreamedLevelBeLoaded(SelectedScene))
            {
                StatusKey = "pvp.error.load";
                return;
            }
            transport.AcceptGuests = false;
            round++;
            StatusKey = "";
            Countdown = 0;
            elapsedAtSnapshot = 0;
            lastSnapshot = 0;
            foreach (var member in members)
            {
                member.Loaded = false;
                member.Rank = 0;
                member.FinishTime = 0;
            }
            Phase = PvpPhase.Loading;
            loadDeadline = Now + 30;
            SetScene(SelectedScene);
            PublishRoom();
        }

        public void ReturnToRoom()
        {
            if (IsHost && Phase == PvpPhase.Results)
                BeginReturn("");
        }

        private void BeginReturn(string status)
        {
            StatusKey = status;
            Countdown = 0;
            Phase = PvpPhase.Returning;
            PublishRoom();
            DestroyRacers();
            members.RemoveAll(member => member.Disconnected);
            foreach (var member in members)
            {
                member.Ready = member.IsHost || member.IsBot;
                member.Loaded = false;
                member.Rank = 0;
                member.FinishTime = 0;
            }
            SetScene(MenuScene);
        }

        private void ResetSession()
        {
            CancelOnlineRequest();
            Phase = PvpPhase.Idle;
            transport.Stop();
            members.Clear();
            DestroyRacers();
            RoomName = "";
            SelectedScene = "Tutorial";
            round = 0;
            selectionRevision = 0;
            acknowledgedRound = -1;
            failedLoadRound = -1;
            lastSnapshot = 0;
            ControlsBlocked = false;
            Countdown = 0;
            if (drivingScene)
                SetScene(MenuScene);
        }

        private void SetScene(string scene)
        {
            desiredScene = scene;
            drivingScene = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Phase != PvpPhase.Loading || scene.name != SelectedScene)
                return;
            try
            {
                arena = PvpArena.Prepare(scene, IsHost);
            }
            catch (Exception error)
            {
                Debug.LogError("PvP level setup failed: " + error.Message);
                HandleSceneLoadFailure();
            }
        }

        private void Update()
        {
            if (onlineRequest != null && Now >= onlineDeadline)
                OnTransportFailed("pvp.error.relay_timeout");
            UpdateScene();
            if (IsHost)
                UpdateHost();

            if (racers.Count == 0)
                return;
            if (racers.TryGetValue(LocalId, out var local))
            {
                var member = Member(LocalId);
                bool canPlay = Phase == PvpPhase.Racing && member != null && member.Rank == 0 && !ControlsBlocked;
                local.Actor.SetInputEnabled(canPlay);
                local.Input = local.Actor.ReadInput();
                if (!IsHost && canPlay && Now >= nextInput)
                {
                    nextInput = Now + InputInterval;
                    transport.Send(transport.ServerId, Pack(Message.Input, writer =>
                    {
                        writer.Write(round);
                        writer.Write(++inputSequence);
                        writer.Write(local.Input.x);
                        writer.Write(local.Input.y);
                    }), false);
                }
            }
            if (!IsHost)
                InterpolatePoses();
        }

        private void UpdateScene()
        {
            if (!drivingScene || load != null && !load.isDone)
                return;
            load = null;
            var active = SceneManager.GetActiveScene();
            if (active.name != desiredScene)
            {
                try
                {
                    if (Application.CanStreamedLevelBeLoaded(desiredScene))
                        load = SceneManager.LoadSceneAsync(desiredScene, LoadSceneMode.Single);
                }
                catch (ArgumentException error)
                {
                    Debug.LogWarning(error.Message);
                }
                catch (InvalidOperationException error)
                {
                    Debug.LogWarning(error.Message);
                }
                if (load == null)
                    HandleSceneLoadFailure();
                return;
            }
            if (desiredScene == MenuScene)
            {
                drivingScene = false;
                if (IsHost && Phase == PvpPhase.Returning)
                {
                    Phase = PvpPhase.Waiting;
                    transport.AcceptGuests = true;
                    PublishRoom();
                }
                return;
            }
            if (Phase != PvpPhase.Loading || acknowledgedRound == round)
                return;

            try
            {
                if (!arena)
                    arena = PvpArena.Prepare(active, IsHost);
                if (!arena || !arena.Valid)
                {
                    HandleSceneLoadFailure();
                    return;
                }
                foreach (var member in members)
                    Spawn(member.Id);
            }
            catch (InvalidOperationException error)
            {
                Debug.LogError(error.Message);
                HandleSceneLoadFailure();
                return;
            }
            acknowledgedRound = round;
            if (IsHost)
            {
                foreach (var member in members.Where(member => member.IsBot))
                    member.Loaded = true;
                MarkLoaded(LocalId);
            }
            else
                transport.Send(transport.ServerId, Pack(Message.Loaded, writer => writer.Write(round)));
        }

        private void HandleSceneLoadFailure()
        {
            const string error = "pvp.error.load";
            if (Phase != PvpPhase.Loading || desiredScene != SelectedScene)
            {
                drivingScene = false;
                OnTransportFailed(error);
                return;
            }
            if (IsHost)
            {
                BeginReturn(error);
                return;
            }
            failedLoadRound = round;
            StatusKey = error;
            DestroyRacers();
            SetScene(MenuScene);
            transport.Send(transport.ServerId, Pack(Message.LoadFailed, writer => writer.Write(round)));
        }

        private void Spawn(ulong id)
        {
            // Keep prefab scripts from running before the PvP rig has been configured.
            var staging = new GameObject("PvP spawn");
            staging.SetActive(false);
            var clone = Instantiate(arena.Character, staging.transform);
            clone.SetActive(false);
            clone.transform.SetParent(null);
            Destroy(staging);
            clone.name = Member(id)?.Name ?? "Player";
            var actor = clone.AddComponent<PvpActor>();
            try
            {
                actor.Setup(IsHost, id == LocalId, arena.Spawn);
                if (Member(id)?.IsBot == true)
                    actor.ConfigureTestBot();
                arena.RegisterActor(actor);
            }
            catch (InvalidOperationException)
            {
                Destroy(clone);
                throw;
            }
            clone.SetActive(true);
            actor.Freeze(true);
            racers.Add(id, new Racer { Actor = actor, PreviousCenter = arena.Spawn });
            if (id == LocalId)
                arena.Follow(actor);
        }

        private void DestroyRacers()
        {
            foreach (var racer in racers.Values)
            {
                if (!racer.Actor)
                    continue;
                racer.Actor.SetInputEnabled(false);
                Destroy(racer.Actor.gameObject);
            }
            racers.Clear();
            contacts.Clear();
            arena = null;
        }

        private void UpdateHost()
        {
            if (Phase == PvpPhase.Loading)
            {
                if (Now >= loadDeadline)
                    BeginReturn("pvp.error.load");
                return;
            }
            if (Phase == PvpPhase.Countdown)
            {
                Countdown = Math.Max(0, Mathf.CeilToInt((float)(startsAt - Now)));
                if (Now >= startsAt)
                {
                    Phase = PvpPhase.Racing;
                    contactActors.Clear();
                    foreach (var racer in racers.Values)
                    {
                        racer.Actor.Freeze(false);
                        racer.PreviousCenter = racer.Actor.BodyCollider.bounds.center;
                        contactActors.Add(racer.Actor);
                    }
                    contacts.Prime(contactActors);
                    PublishRoom();
                }
            }
            if (Phase == PvpPhase.Racing)
                elapsedAtSnapshot = Now - startsAt;
            if ((Phase == PvpPhase.Countdown || Phase == PvpPhase.Racing) && Now >= nextSnapshot)
            {
                nextSnapshot = Now + SnapshotInterval;
                Broadcast(Pack(Message.Snapshot, WriteSnapshot), false);
                if (arena && arena.World != null && Now >= nextWorldSnapshot)
                {
                    nextWorldSnapshot = Now + .1;
                    foreach (var chunk in arena.World.CaptureChunks())
                        Broadcast(Pack(Message.WorldSnapshot, writer =>
                        {
                            writer.Write(round);
                            writer.Write(chunk);
                        }), false);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsHost || Phase != PvpPhase.Racing)
                return;
            contactActors.Clear();
            arrivals.Clear();
            bool respawned = false;
            foreach (var member in members)
            {
                if (member.Rank != 0 || !racers.TryGetValue(member.Id, out var racer))
                    continue;
                var actor = racer.Actor;
                if (arena.ShouldRespawn(actor))
                {
                    contacts.Forget(actor);
                    actor.Respawn(arena.Spawn);
                    racer.PreviousCenter = arena.Spawn;
                    respawned = true;
                    continue;
                }
                if (!member.IsBot && arena.HasReachedFinish(actor))
                {
                    arrivals.Add((member, racer, arena.ArrivalFraction(actor, racer.PreviousCenter)));
                    continue;
                }
                racer.PreviousCenter = actor.BodyCollider.bounds.center;
                bool hasInput = !member.IsBot && (member.IsHost ? !ControlsBlocked && Application.isFocused :
                    Now - racer.InputAt < InputTimeout);
                if (hasInput)
                    actor.ApplyInput(racer.Input);
                else
                    actor.HoldInput();
                contactActors.Add(actor);
            }
            contacts.Step(contactActors);
            if (arrivals.Count == 0)
            {
                if (respawned)
                    Broadcast(Pack(Message.Snapshot, WriteSnapshot));
                return;
            }
            int rank = members.Count(player => player.Rank > 0);
            foreach (var arrival in arrivals.OrderBy(result => result.Arrival))
            {
                arrival.Member.Rank = ++rank;
                arrival.Member.FinishTime = (float)Math.Max(0,
                    Now - startsAt - Time.fixedUnscaledDeltaTime * (1 - arrival.Arrival));
                arrival.Racer.Actor.Freeze(true);
                contacts.Forget(arrival.Racer.Actor);
            }
            if (members.All(member => member.IsBot || member.Rank != 0))
            {
                elapsedAtSnapshot = members.Max(member => member.FinishTime);
                Phase = PvpPhase.Results;
                foreach (var racer in racers.Values)
                    racer.Actor.Freeze(true);
            }
            Broadcast(Pack(Message.Snapshot, WriteSnapshot));
            PublishRoom();
        }

        private void OnConnected(ulong id)
        {
            if (!IsHost || (id != transport.ServerId && Phase != PvpPhase.Waiting))
                return;
            AddMember(id);
            PublishRoom();
        }

        private void OnDisconnected(ulong id)
        {
            if (!IsHost)
                return; // The transport reports the host loss through Failed.
            var member = Member(id);
            if (member == null || member.IsHost)
                return;
            if (racers.TryGetValue(id, out var racer))
            {
                contacts.Forget(racer.Actor);
                Destroy(racer.Actor.gameObject);
                racers.Remove(id);
            }
            if (Phase == PvpPhase.Waiting || Phase == PvpPhase.Returning)
                members.Remove(member);
            else
                member.Disconnected = true;
            if (Phase == PvpPhase.Loading || Phase == PvpPhase.Countdown ||
                Phase == PvpPhase.Racing && member.Rank == 0)
            {
                BeginReturn("pvp.error.match_interrupted");
                return;
            }
            PublishRoom();
        }

        private void OnTransportFailed(string key)
        {
            StatusKey = key;
            ResetSession();
        }

        private PvpMember Member(ulong id) => members.Find(member => member.Id == id);

        private void AddMember(ulong id)
        {
            if (Member(id) != null || members.Count >= SeatCount)
                return;
            bool host = id == transport.ServerId;
            members.Add(new PvpMember { Id = id, IsHost = host, Ready = host, Name = host ? "Host" : "Player " + id });
        }

        private void MarkLoaded(ulong id)
        {
            var member = Member(id);
            if (member == null || member.Loaded)
                return;
            member.Loaded = true;
            if (members.All(player => player.Loaded))
            {
                Phase = PvpPhase.Countdown;
                Countdown = 3;
                startsAt = Now + Countdown;
            }
            PublishRoom();
        }

        private void OnMessage(ulong sender, byte[] packet)
        {
            if (packet.Length == 0 || IsHost && Member(sender) == null ||
                !IsHost && sender != transport.ServerId)
                return;
            try
            {
                using var reader = new BinaryReader(new MemoryStream(packet));
                var type = (Message)reader.ReadByte();
                if (IsHost)
                    ReadClientMessage(sender, type, reader);
                else if (type == Message.Room)
                    ReadRoom(reader);
                else if (type == Message.Snapshot)
                    ReadSnapshot(reader);
                else if (type == Message.WorldSnapshot && reader.ReadInt32() == round && arena && arena.World != null)
                    arena.World.ReadChunk(reader);
            }
            catch (IOException) { }
            catch (ArgumentException) { }
            catch (FormatException) { }
        }

        private void ReadClientMessage(ulong sender, Message type, BinaryReader reader)
        {
            var member = Member(sender);
            if (type == Message.Ready && Phase == PvpPhase.Waiting && reader.BaseStream.Length == 6 &&
                reader.ReadInt32() == selectionRevision)
            {
                bool ready = reader.ReadBoolean();
                if (member.Ready == ready)
                    return;
                member.Ready = ready;
                PublishRoom();
            }
            else if ((type == Message.Loaded || type == Message.LoadFailed) && Phase == PvpPhase.Loading &&
                reader.BaseStream.Length == 5 && reader.ReadInt32() == round)
            {
                if (type == Message.LoadFailed)
                    BeginReturn("pvp.error.load");
                else
                    MarkLoaded(sender);
            }
            else if (type == Message.Input && Phase == PvpPhase.Racing && member.Rank == 0 &&
                reader.BaseStream.Length == 17 && reader.ReadInt32() == round &&
                racers.TryGetValue(sender, out var racer))
            {
                uint sequence = reader.ReadUInt32();
                var input = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                if (sequence <= racer.Sequence || !Finite(input.x) || !Finite(input.y) ||
                    Mathf.Abs(input.x) > 4 || Mathf.Abs(input.y) > 4 || Now - racer.InputAt < .01)
                    return;
                racer.Sequence = sequence;
                racer.Input = input;
                racer.InputAt = Now;
            }
        }

        private void PublishRoom()
        {
            Broadcast(Pack(Message.Room, writer =>
            {
                writer.Write((byte)Phase);
                writer.Write(round);
                writer.Write(RoomName);
                writer.Write(SelectedScene);
                writer.Write(selectionRevision);
                writer.Write(StatusKey);
                writer.Write((byte)Countdown);
                writer.Write((float)elapsedAtSnapshot);
                writer.Write((byte)members.Count);
                foreach (var member in members)
                {
                    writer.Write(member.Id);
                    writer.Write(member.IsBot);
                    writer.Write(member.Ready);
                    writer.Write(member.Loaded);
                    writer.Write(member.Disconnected);
                    writer.Write((byte)member.Rank);
                    writer.Write(member.FinishTime);
                }
            }));
        }

        private void ReadRoom(BinaryReader reader)
        {
            var phase = (PvpPhase)reader.ReadByte();
            int nextRound = reader.ReadInt32();
            string room = reader.ReadString();
            string scene = reader.ReadString();
            int selectedRevision = reader.ReadInt32();
            string status = reader.ReadString();
            int countdown = reader.ReadByte();
            float elapsed = reader.ReadSingle();
            int count = reader.ReadByte();
            if (count < 1 || count > SeatCount || phase < PvpPhase.Waiting || phase > PvpPhase.Returning ||
                room.Length > MaxRoomName || status.Length > 80 || !Finite(elapsed) || nextRound < round ||
                !PvpLevelCatalog.Levels.Any(level => level.SceneName == scene) || selectedRevision < selectionRevision)
                return;
            var updated = new List<PvpMember>(count);
            for (int i = 0; i < count; i++)
            {
                ulong id = reader.ReadUInt64();
                bool bot = reader.ReadBoolean();
                if (bot != (id == TestBotId) || updated.Any(member => member.Id == id))
                    return;
                updated.Add(new PvpMember
                {
                    Id = id,
                    IsHost = id == transport.ServerId,
                    IsBot = bot,
                    Name = bot ? "Test Bot" : id == transport.ServerId ? "Host" : "Player " + id,
                    Ready = reader.ReadBoolean(),
                    Loaded = reader.ReadBoolean(),
                    Disconnected = reader.ReadBoolean(),
                    Rank = reader.ReadByte(),
                    FinishTime = reader.ReadSingle()
                });
            }

            if (nextRound != round)
                lastSnapshot = 0;
            if (phase == PvpPhase.Racing && Phase != PvpPhase.Racing && racers.TryGetValue(LocalId, out var local))
                local.Actor.Freeze(false);
            round = nextRound;
            Phase = phase;
            RoomName = room;
            SelectedScene = scene;
            selectionRevision = selectedRevision;
            StatusKey = status;
            Countdown = countdown;
            elapsedAtSnapshot = elapsed;
            clockReceived = Now;
            members.Clear();
            members.AddRange(updated);
            if (phase == PvpPhase.Loading && failedLoadRound != round)
                SetScene(SelectedScene);
            else if (phase == PvpPhase.Returning || phase == PvpPhase.Waiting)
            {
                if (drivingScene || SceneManager.GetActiveScene().name != MenuScene)
                {
                    DestroyRacers();
                    SetScene(MenuScene);
                }
            }
            foreach (var member in members)
            {
                if (member.Disconnected && racers.TryGetValue(member.Id, out var racer))
                {
                    Destroy(racer.Actor.gameObject);
                    racers.Remove(member.Id);
                }
            }
        }

        private void WriteSnapshot(BinaryWriter writer)
        {
            writer.Write(round);
            writer.Write(++snapshotSequence);
            writer.Write((byte)Countdown);
            writer.Write((float)elapsedAtSnapshot);
            writer.Write((byte)racers.Count);
            foreach (var pair in racers)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.Actor.SpawnRevision);
                foreach (float value in pair.Value.Actor.CapturePose())
                    writer.Write(value);
            }
        }

        private void ReadSnapshot(BinaryReader reader)
        {
            if (reader.ReadInt32() != round || racers.Count == 0)
                return;
            uint sequence = reader.ReadUInt32();
            if (sequence <= lastSnapshot)
                return;
            int countdown = reader.ReadByte();
            float elapsed = reader.ReadSingle();
            int count = reader.ReadByte();
            if (count > SeatCount || !Finite(elapsed) ||
                reader.BaseStream.Length != 15 + count * (12 + 4 * PvpActor.PoseLength))
                return;
            lastSnapshot = sequence;
            if (Phase == PvpPhase.Countdown)
                Countdown = countdown;
            if (Phase == PvpPhase.Racing)
            {
                elapsedAtSnapshot = elapsed;
                clockReceived = Now;
            }
            for (int slot = 0; slot < count; slot++)
            {
                ulong id = reader.ReadUInt64();
                int spawnRevision = reader.ReadInt32();
                var target = new float[PvpActor.PoseLength];
                for (int i = 0; i < target.Length; i++)
                {
                    target[i] = reader.ReadSingle();
                    if (!Finite(target[i]))
                        return;
                }
                if (!racers.TryGetValue(id, out var racer))
                    continue;
                if (racer.RenderedSpawnRevision != spawnRevision)
                {
                    racer.RenderedSpawnRevision = spawnRevision;
                    racer.Actor.ApplyPose(target);
                    racer.Target = null;
                    continue;
                }
                racer.Pose = racer.Actor.CapturePose();
                racer.Target = target;
                racer.PoseAt = Now;
            }
        }

        private void InterpolatePoses()
        {
            foreach (var racer in racers.Values)
            {
                if (racer.Target == null)
                    continue;
                float t = Mathf.Clamp01((float)((Now - racer.PoseAt) / SnapshotInterval));
                racer.Actor.ApplyPose(racer.Pose, racer.Target, t);
                if (t >= 1)
                    racer.Target = null;
            }
        }

        private void Broadcast(byte[] packet, bool reliable = true)
        {
            foreach (var member in members)
                if (!member.IsHost && !member.IsBot && !member.Disconnected)
                    transport.Send(member.Id, packet, reliable);
        }

        private static byte[] Pack(Message type, Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((byte)type);
            write(writer);
            writer.Flush();
            return stream.ToArray();
        }

        private static bool Finite(float number) => !float.IsNaN(number) && !float.IsInfinity(number);
    }
}
