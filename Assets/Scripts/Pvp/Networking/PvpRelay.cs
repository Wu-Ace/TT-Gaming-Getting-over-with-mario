using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;

namespace Odyssey.Pvp
{
    internal static class PvpRelay
    {
        private const string Protocol = "dtls";
        private const string RoomCodeAlphabet = "6789BCDFGHJKLMNPQRTW";
        private static Task authentication;

        internal static bool IsConfigured => !string.IsNullOrWhiteSpace(Application.cloudProjectId);

        internal static bool IsRoomCode(string code)
        {
            if (code.Length < 6 || code.Length > 12)
                return false;
            foreach (char character in code)
                if (RoomCodeAlphabet.IndexOf(character) < 0)
                    return false;
            return true;
        }

        internal static async Task<(string Code, RelayServerData Data)> RequestAsync(
            string code, CancellationToken cancellation)
        {
            if (authentication == null || authentication.IsCompleted)
                authentication = AuthenticateAsync();
            await authentication;
            cancellation.ThrowIfCancellationRequested();

            if (code != null)
            {
                var joined = await RelayService.Instance.JoinAllocationAsync(code);
                cancellation.ThrowIfCancellationRequested();
                return (code, new RelayServerData(joined, Protocol));
            }

            var hosted = await RelayService.Instance.CreateAllocationAsync(PvpTransport.MaxPlayers - 1);
            cancellation.ThrowIfCancellationRequested();
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(hosted.AllocationId);
            cancellation.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(joinCode))
                throw new InvalidOperationException("The relay returned no room code.");
            return (joinCode, new RelayServerData(hosted, Protocol));
        }

        private static async Task AuthenticateAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsAuthorized)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        internal static string ErrorKey(Exception error, bool joining)
        {
            if (error is RequestFailedException failed)
            {
                switch (failed.ErrorCode)
                {
                    case (int)RelayExceptionReason.InactiveProject:
                        return "pvp.error.relay_not_configured";
                    case (int)RelayExceptionReason.JoinCodeNotFound:
                    case (int)RelayExceptionReason.AllocationNotFound:
                    case (int)RelayExceptionReason.EntityNotFound:
                    case (int)RelayExceptionReason.Gone:
                        if (joining)
                            return "pvp.error.room_code_expired";
                        break;
                    case (int)RelayExceptionReason.RateLimited:
                    case CommonErrorCodes.TooManyRequests:
                        return "pvp.error.relay_rate_limited";
                    case (int)RelayExceptionReason.RequestTimeOut:
                    case (int)RelayExceptionReason.GatewayTimeout:
                    case CommonErrorCodes.Timeout:
                        return "pvp.error.relay_timeout";
                }
                Debug.LogWarning($"PvP online service request failed (code {failed.ErrorCode}).");
            }
            else
            {
                Debug.LogWarning($"PvP online connection setup failed ({error.GetType().Name}).");
            }
            return "pvp.error.relay_unavailable";
        }
    }
}
