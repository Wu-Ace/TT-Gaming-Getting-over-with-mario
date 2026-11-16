using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Odyssey.Pvp
{
    public sealed class PvpTransport : MonoBehaviour
    {
        public const int MaxPlayers = 4;
        public const int MaxMessageBytes = 1024;

        private const ushort NgoProtocolVersion = 1;
        private const byte Version = 2;
        // Increment for incompatible changes to the PvP scene, character rig or match rules.
        private const string ContentRevision = "2";
        private const string InvitePrefix = "ODP2-";
        private const string MessageName = "Odyssey.Pvp.v2";
        private const int CompatibilityBytes = 16;
        private const int SecretBytes = 16;
        private const int InviteCompatibilityOffset = 6;
        private const int InviteSecretOffset = InviteCompatibilityOffset + CompatibilityBytes;
        private const int InviteBytes = InviteSecretOffset + SecretBytes;
        private const int InviteTextLength = (InviteBytes * 8 + 5) / 6;
        private const int NonceBytes = 16;
        private const int ProofBytes = 32;
        private const int ApprovalCompatibilityOffset = 1;
        private const int ApprovalNonceOffset = ApprovalCompatibilityOffset + CompatibilityBytes;
        private const int ApprovalProofOffset = ApprovalNonceOffset + NonceBytes;
        private const int ApprovalBytes = ApprovalProofOffset + ProofBytes;
        private const int MaxAcceptedNonces = 8192;
        private const int MessagesPerSecond = 240;
        private const int ApprovalTimeout = 10;
        private const int PeerTimeoutMs = 10000;
        private const float JoinTimeout = 12f;

        private readonly Dictionary<ulong, GuestReservation> pending = new Dictionary<ulong, GuestReservation>();
        private readonly Dictionary<ulong, PacketBudget> guests = new Dictionary<ulong, PacketBudget>();
        private readonly HashSet<Guid> acceptedNonces = new HashSet<Guid>();
        private readonly PacketBudget serverBudget = new PacketBudget();
        private readonly List<ulong> expiredReservations = new List<ulong>(MaxPlayers - 1);
        private NetworkManager manager;
        private UnityTransport transport;
        private IPAddress advertisedAddress;
        private ushort advertisedPort;
        private int inviteGeneration;
        private byte[] roomCompatibility;
        private byte[] roomSecret;
        private bool hosting;
        private bool joining;
        private bool localConnected;
        private bool starting;
        private bool listeningForMessages;
        private double joinDeadline;

        public bool IsHost => hosting;
        public bool IsActive => hosting || joining || localConnected;
        public bool IsConnected => localConnected;
        public ulong ServerId => NetworkManager.ServerClientId;
        public ulong LocalId => IsConnected ? manager.LocalClientId : ServerId;
        public bool AcceptGuests { get; set; }
        // A local test bot occupies one of the same four seats as network players.
        // Count approved-but-not-yet-connected peers so adding it cannot overbook a room.
        public int ReservedSeats { get; private set; }
        public bool CanReserveSeat => hosting && localConnected && AcceptGuests &&
            ReservedSeats == 0 && guests.Count + pending.Count < MaxPlayers - 1;
        public string Invite { get; private set; } = string.Empty;
        public string AdvertisedEndpoint { get; private set; } = string.Empty;
        public string LastErrorKey { get; private set; } = string.Empty;

        public event Action<ulong> Connected;
        public event Action<ulong> Disconnected;
        public event Action<ulong, byte[]> MessageReceived;
        public event Action<string> Failed;

        public bool Host(string advertisedIpv4, ushort listenPort, ushort advertisedPort)
        {
            if (IsBusy()) return Report("pvp.error.busy");
            if (listenPort == 0) return Report("pvp.error.port");
            if (advertisedPort == 0) advertisedPort = listenPort;

            IPAddress address;
            if (string.IsNullOrWhiteSpace(advertisedIpv4))
                address = FindLocalAddress();
            else if (!TryParseAddress(advertisedIpv4.Trim(), out address))
                return Report("pvp.error.address");

            LastErrorKey = string.Empty;
            PrepareManager();
            advertisedAddress = address;
            this.advertisedPort = advertisedPort;
            AdvertisedEndpoint = address + ":" + advertisedPort.ToString(CultureInfo.InvariantCulture);
            roomCompatibility = CreateCompatibilityTag(Application.version);
            IssueInvite();
            AcceptGuests = true;
            hosting = true;
            return StartNetwork(Array.Empty<byte>(), IPAddress.Loopback, listenPort, true);
        }

        public bool RegenerateInvite()
        {
            if (!hosting || !localConnected || !AcceptGuests || manager == null || manager.ShutdownInProgress)
                return false;
            IssueInvite();
            return true;
        }

        public bool TryReserveSeat()
        {
            if (!CanReserveSeat)
                return false;
            ReservedSeats = 1;
            return true;
        }

        public void ReleaseReservedSeat() => ReservedSeats = 0;

        public bool Join(string invite)
        {
            if (IsBusy()) return Report("pvp.error.busy");
            byte[] compatibility = CreateCompatibilityTag(Application.version);
            if (!TryReadInvite(invite, compatibility, out var address, out var port, out var secret, out var error))
                return Report(error);

            LastErrorKey = string.Empty;
            PrepareManager();
            joinDeadline = Time.realtimeSinceStartupAsDouble + JoinTimeout;
            joining = true;
            var proof = CreateApproval(secret, compatibility);
            Array.Clear(secret, 0, secret.Length);
            return StartNetwork(proof, address, port, false);
        }

        public bool Send(ulong receiver, byte[] payload, bool reliable = true)
        {
            if (!localConnected || payload == null || payload.Length == 0 || payload.Length > MaxMessageBytes)
                return false;
            var messaging = manager != null && !manager.ShutdownInProgress ? manager.CustomMessagingManager : null;
            if (messaging == null)
                return false;
            if (hosting ? !guests.ContainsKey(receiver) : receiver != ServerId)
                return false;

            using (var writer = new FastBufferWriter(payload.Length, Allocator.Temp))
            {
                writer.WriteBytesSafe(payload, payload.Length);
                messaging.SendNamedMessage(MessageName, receiver, writer,
                    reliable ? NetworkDelivery.ReliableSequenced : NetworkDelivery.UnreliableSequenced);
            }
            return true;
        }

        public void Disconnect(ulong guestId)
        {
            if (hosting && guests.ContainsKey(guestId)) manager.DisconnectClient(guestId);
        }

        public void Stop()
        {
            hosting = false;
            joining = false;
            localConnected = false;
            AcceptGuests = false;
            ReservedSeats = 0;
            Invite = string.Empty;
            AdvertisedEndpoint = string.Empty;
            advertisedAddress = null;
            advertisedPort = 0;
            inviteGeneration = 0;
            roomCompatibility = null;
            pending.Clear();
            guests.Clear();
            acceptedNonces.Clear();
            serverBudget.Reset();
            if (roomSecret != null)
            {
                Array.Clear(roomSecret, 0, roomSecret.Length);
                roomSecret = null;
            }

            if (manager == null) return;
            if (listeningForMessages)
            {
                manager.CustomMessagingManager?.UnregisterNamedMessageHandler(MessageName);
                listeningForMessages = false;
            }
            manager.ConnectionApprovalCallback = null;
            manager.OnClientConnectedCallback -= OnConnected;
            manager.OnClientDisconnectCallback -= OnDisconnected;
            manager.OnTransportFailure -= OnTransportFailure;
            if ((manager.IsClient || manager.IsServer) && !manager.ShutdownInProgress) manager.Shutdown();
            manager.NetworkConfig.ConnectionData = Array.Empty<byte>();
        }

        private void Update()
        {
            var now = Time.realtimeSinceStartupAsDouble;
            if (joining && now >= joinDeadline)
                EndWithError("pvp.error.connect_timeout");

            if (!hosting || pending.Count == 0) return;
            expiredReservations.Clear();
            foreach (var reservation in pending)
                if (now >= reservation.Value.Deadline) expiredReservations.Add(reservation.Key);
            foreach (var id in expiredReservations) pending.Remove(id);
        }

        private void OnDestroy()
        {
            Stop();
            if (manager != null) Destroy(manager.gameObject);
        }

        private bool IsBusy()
        {
            var singleton = NetworkManager.Singleton;
            return IsActive || (manager != null && (manager.IsListening || manager.ShutdownInProgress))
                || (singleton != null && singleton != manager);
        }

        private void PrepareManager()
        {
            if (manager == null)
            {
                var networkObject = new GameObject("PvP Network Manager");
                networkObject.SetActive(false);
                transport = networkObject.AddComponent<UnityTransport>();
                manager = networkObject.AddComponent<NetworkManager>();
                manager.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = transport,
                    ProtocolVersion = NgoProtocolVersion,
                    ConnectionApproval = true,
                    ClientConnectionBufferTimeout = ApprovalTimeout,
                    EnableSceneManagement = false
                };
                transport.ConnectTimeoutMS = 1000;
                transport.DisconnectTimeoutMS = PeerTimeoutMs;
                transport.MaxConnectAttempts = 8;
                networkObject.SetActive(true);
            }

            manager.ConnectionApprovalCallback = Approve;
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
            manager.OnTransportFailure += OnTransportFailure;
        }

        private bool StartNetwork(byte[] connectionData, IPAddress address, ushort port, bool host)
        {
            var error = host ? "pvp.error.start_host" : "pvp.error.start_client";
            starting = true;
            try
            {
                manager.NetworkConfig.ConnectionData = connectionData;
                transport.SetConnectionData(address.ToString(), port, host ? "0.0.0.0" : null);
                if (host ? manager.StartHost() : manager.StartClient())
                {
                    RegisterMessages();
                    return true;
                }
            }
            catch (SocketException exception)
            {
                Debug.LogWarning($"PvP UDP startup failed ({exception.SocketErrorCode}).");
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogWarning($"PvP network startup failed ({exception.GetType().Name}).");
            }
            finally { starting = false; }

            EndWithError(error);
            return false;
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            response.Pending = false;
            if (hosting && request.ClientNetworkId == ServerId)
            {
                response.Approved = true;
                return;
            }

            var rejection = GetRejection(request.Payload);
            response.Approved = rejection == null;
            response.Reason = rejection ?? string.Empty;
            if (response.Approved)
                pending.Add(request.ClientNetworkId,
                    new GuestReservation(Time.realtimeSinceStartupAsDouble + ApprovalTimeout, inviteGeneration));
        }

        private string GetRejection(byte[] payload)
        {
            if (!hosting || !AcceptGuests || roomSecret == null) return "pvp.error.room_closed";
            if (payload == null || payload.Length == 0) return "pvp.error.invite";
            if (payload[0] != Version) return "pvp.error.version";
            if (payload.Length != ApprovalBytes) return "pvp.error.invite";
            if (!BytesMatch(payload, ApprovalCompatibilityOffset, roomCompatibility, 0, CompatibilityBytes))
                return "pvp.error.version";

            byte[] expected;
            using (var hmac = new HMACSHA256(roomSecret))
                expected = hmac.ComputeHash(payload, 0, ApprovalProofOffset);

            if (!BytesMatch(payload, ApprovalProofOffset, expected, 0, ProofBytes)) return "pvp.error.invite";
            if (guests.Count + pending.Count + ReservedSeats >= MaxPlayers - 1) return "pvp.error.room_full";

            if (acceptedNonces.Count >= MaxAcceptedNonces) return "pvp.error.room_closed";
            var nonce = new byte[NonceBytes];
            Buffer.BlockCopy(payload, ApprovalNonceOffset, nonce, 0, NonceBytes);
            return acceptedNonces.Add(new Guid(nonce)) ? null : "pvp.error.replay";
        }

        private void RegisterMessages()
        {
            if (listeningForMessages || manager.CustomMessagingManager == null) return;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, Receive);
            listeningForMessages = true;
        }

        private void OnConnected(ulong id)
        {
            RegisterMessages();
            if (hosting && id != ServerId)
            {
                bool reserved = pending.TryGetValue(id, out var reservation);
                pending.Remove(id);
                if (!reserved || reservation.InviteGeneration != inviteGeneration)
                {
                    manager.DisconnectClient(id, "pvp.error.invite");
                    return;
                }
                if (!AcceptGuests)
                {
                    manager.DisconnectClient(id, "pvp.error.room_closed");
                    return;
                }
                guests.Add(id, new PacketBudget());
            }
            else
            {
                joining = false;
                localConnected = true;
            }
            Connected?.Invoke(id);
        }

        private void OnDisconnected(ulong id)
        {
            if (hosting)
            {
                pending.Remove(id);
                if (guests.Remove(id) && !manager.ShutdownInProgress) Disconnected?.Invoke(id);
                return;
            }

            if (!joining && !localConnected) return;
            bool wasConnected = localConnected;
            var reason = manager.DisconnectReason;
            switch (reason)
            {
                case "pvp.error.room_closed":
                case "pvp.error.room_full":
                case "pvp.error.invite":
                case "pvp.error.version":
                case "pvp.error.replay":
                    break;
                default:
                    reason = wasConnected ? "pvp.error.host_disconnected" : "pvp.error.connect_timeout";
                    break;
            }

            EndWithError(reason);
            if (wasConnected) Disconnected?.Invoke(id);
        }

        private void OnTransportFailure()
        {
            if (IsActive && !starting) EndWithError("pvp.error.transport");
        }

        private void Receive(ulong sender, FastBufferReader reader)
        {
            if (!localConnected) return;
            var budget = serverBudget;
            if (hosting ? !guests.TryGetValue(sender, out budget) : sender != ServerId) return;

            int length = reader.Length - reader.Position;
            if (length <= 0 || length > MaxMessageBytes || !budget.Consume(Time.realtimeSinceStartupAsDouble))
                return;

            var payload = new byte[length];
            reader.ReadBytesSafe(ref payload, length);
            MessageReceived?.Invoke(sender, payload);
        }

        private void EndWithError(string key)
        {
            Stop();
            Report(key);
        }

        private bool Report(string key)
        {
            LastErrorKey = key;
            Failed?.Invoke(key);
            return false;
        }

        private void IssueInvite()
        {
            var secret = RandomBytes(SecretBytes);
            var nextInvite = FormatInvite(advertisedAddress, advertisedPort, roomCompatibility, secret);
            if (roomSecret != null) Array.Clear(roomSecret, 0, roomSecret.Length);
            roomSecret = secret;
            inviteGeneration++;
            acceptedNonces.Clear();
            Invite = nextInvite;
        }

        private static byte[] CreateCompatibilityTag(string applicationVersion)
        {
            using (var sha = SHA256.Create())
            {
                var input = Encoding.UTF8.GetBytes(applicationVersion + "\n" + ContentRevision);
                var hash = sha.ComputeHash(input);
                var tag = new byte[CompatibilityBytes];
                Buffer.BlockCopy(hash, 0, tag, 0, tag.Length);
                return tag;
            }
        }

        private static byte[] CreateApproval(byte[] secret, byte[] compatibility)
        {
            var payload = new byte[ApprovalBytes];
            payload[0] = Version;
            Buffer.BlockCopy(compatibility, 0, payload, ApprovalCompatibilityOffset, CompatibilityBytes);
            Buffer.BlockCopy(RandomBytes(NonceBytes), 0, payload, ApprovalNonceOffset, NonceBytes);
            using (var hmac = new HMACSHA256(secret))
            {
                var proof = hmac.ComputeHash(payload, 0, ApprovalProofOffset);
                Buffer.BlockCopy(proof, 0, payload, ApprovalProofOffset, ProofBytes);
            }
            return payload;
        }

        private static bool BytesMatch(byte[] left, int leftOffset, byte[] right, int rightOffset, int size)
        {
            int difference = 0;
            for (int i = 0; i < size; i++) difference |= left[leftOffset + i] ^ right[rightOffset + i];
            return difference == 0;
        }

        private static byte[] RandomBytes(int size)
        {
            var data = new byte[size];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(data);
            return data;
        }

        private static string FormatInvite(IPAddress address, ushort port, byte[] compatibility, byte[] secret)
        {
            var bytes = new byte[InviteBytes];
            Buffer.BlockCopy(address.GetAddressBytes(), 0, bytes, 0, 4);
            bytes[4] = (byte)(port >> 8);
            bytes[5] = (byte)port;
            Buffer.BlockCopy(compatibility, 0, bytes, InviteCompatibilityOffset, CompatibilityBytes);
            Buffer.BlockCopy(secret, 0, bytes, InviteSecretOffset, SecretBytes);
            return InvitePrefix + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static bool TryReadInvite(string text, byte[] compatibility, out IPAddress address, out ushort port,
            out byte[] secret, out string error)
        {
            address = null;
            port = 0;
            secret = null;
            error = "pvp.error.invite";
            if (text == null || text.Length > 128) return false;
            text = text.Trim();
            if (!text.StartsWith(InvitePrefix, StringComparison.Ordinal))
            {
                if (text.StartsWith("ODP", StringComparison.Ordinal)) error = "pvp.error.version";
                return false;
            }

            if (text.Length != InvitePrefix.Length + InviteTextLength) return false;
            var encoded = text.Substring(InvitePrefix.Length).Replace('-', '+').Replace('_', '/');
            byte[] bytes;
            try { bytes = Convert.FromBase64String(encoded.PadRight((encoded.Length + 3) / 4 * 4, '=')); }
            catch (FormatException) { return false; }
            if (bytes.Length != InviteBytes) return false;
            if (!BytesMatch(bytes, InviteCompatibilityOffset, compatibility, 0, CompatibilityBytes))
            {
                error = "pvp.error.version";
                return false;
            }

            address = new IPAddress(new[] { bytes[0], bytes[1], bytes[2], bytes[3] });
            port = (ushort)((bytes[4] << 8) | bytes[5]);
            if (port == 0 || !IsUsableAddress(address)) return false;

            secret = new byte[SecretBytes];
            Buffer.BlockCopy(bytes, InviteSecretOffset, secret, 0, SecretBytes);
            return true;
        }

        private static bool TryParseAddress(string text, out IPAddress address)
        {
            return IPAddress.TryParse(text, out address) && address.ToString() == text && IsUsableAddress(address);
        }

        private static bool IsUsableAddress(IPAddress address)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork) return false;
            byte first = address.GetAddressBytes()[0];
            return first > 0 && first < 224;
        }

        private static IPAddress FindLocalAddress()
        {
            IPAddress bestAddress = IPAddress.Loopback;
            int bestScore = 0;
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    var properties = nic.GetIPProperties();
                    int score = 1;
                    foreach (var gateway in properties.GatewayAddresses)
                        if (IsUsableAddress(gateway.Address)) { score = 3; break; }

                    foreach (var local in properties.UnicastAddresses)
                    {
                        if (score <= bestScore || !IsUsableAddress(local.Address) || IPAddress.IsLoopback(local.Address))
                            continue;
                        bestAddress = local.Address;
                        bestScore = score;
                    }
                }
            }
            catch (NetworkInformationException)
            {
                // An explicit IPv4 address can be used when the platform cannot enumerate interfaces.
            }
            return bestAddress;
        }

        private readonly struct GuestReservation
        {
            internal readonly double Deadline;
            internal readonly int InviteGeneration;

            internal GuestReservation(double deadline, int inviteGeneration)
            {
                Deadline = deadline;
                InviteGeneration = inviteGeneration;
            }
        }

        private sealed class PacketBudget
        {
            private double windowEnd;
            private int count;

            public bool Consume(double now)
            {
                if (now >= windowEnd)
                {
                    windowEnd = now + 1;
                    count = 0;
                }
                if (count >= MessagesPerSecond) return false;
                count++;
                return true;
            }

            public void Reset()
            {
                windowEnd = 0;
                count = 0;
            }
        }
    }
}
