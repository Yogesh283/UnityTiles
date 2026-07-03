using System;
using Mkey.Network;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// TEMPORARY — Waiting Room → Match Start transition probe. Remove after investigation.
    /// </summary>
    public static class TournamentTransitionProbe
    {
        private const string Tag = "[TournamentTransition]";

        private static string _lastWaitingGateKey = string.Empty;
        private static float _lastWaitingGateLogAt;
        private static bool _runMatchStartLogged;
        private static bool _countdownStartedLogged;
        private static bool _countdownFinishedLogged;
        private static bool _vsRevealLogged;
        private static bool _vsRevealDoneLogged;
        private static bool _launchLogged;

        public static void Reset()
        {
            _lastWaitingGateKey = string.Empty;
            _lastWaitingGateLogAt = 0f;
            _runMatchStartLogged = false;
            _countdownStartedLogged = false;
            _countdownFinishedLogged = false;
            _vsRevealLogged = false;
            _vsRevealDoneLogged = false;
            _launchLogged = false;
        }

        public static void Step(int step, string name, bool pass, string detail = null)
        {
            string verdict = pass ? "PASS" : "FAIL";
            string msg = string.IsNullOrEmpty(detail)
                ? $"{Tag} STEP {step} {name} => {verdict}"
                : $"{Tag} STEP {step} {name} => {verdict} | {detail}";
            if (pass)
                Debug.Log(msg);
            else
                Debug.LogWarning(msg);
        }

        public static void LogClockState(string context)
        {
            long scheduled = TournamentServerClock.ScheduledStartMs;
            long serverNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Debug.Log(
                $"{Tag} CLOCK [{context}] " +
                $"match_start_at_ms={scheduled} serverNowMs~={serverNow} " +
                $"HasScheduledStart={TournamentServerClock.HasScheduledStart} " +
                $"IsServerStartTimeReached={TournamentServerClock.IsServerStartTimeReached()} " +
                $"IsStartTimeReached={TournamentServerClock.IsStartTimeReached()} " +
                $"secondsUntilStart={TournamentServerClock.SecondsUntilStart():F2}");
        }

        public static void LogRoomSnapshot(string context, TournamentRoomSnapshot snap, int maxPlayers)
        {
            RoomResponseDto room = TournamentApiBridge.CurrentRoom;
            long? apiMatchStart = room?.matchStartAtMs;
            long? apiServerNow = room?.serverNowMs;

            Debug.Log(
                $"{Tag} SNAPSHOT [{context}] " +
                $"status={snap.status} playerCount={snap.currentPlayers}/{maxPlayers} " +
                $"match_start_at_ms={snap.matchStartAtMs} api_match_start_at_ms={apiMatchStart?.ToString() ?? "null"} " +
                $"api_server_now_ms={apiServerNow?.ToString() ?? "null"} " +
                $"startCountdownSeconds={snap.startCountdownSeconds} countdownSeconds={snap.countdownSeconds:F1} " +
                $"shouldLaunch={snap.shouldLaunch}");
        }

        public static void LogWaitingGate(
            TournamentRoomSnapshot snap,
            int maxPlayers,
            bool roomFull,
            bool serverCountdown,
            bool serverActive,
            bool willEnterRunMatchStart)
        {
            string key =
                $"{snap.status}|{snap.currentPlayers}|{roomFull}|{serverCountdown}|{serverActive}|{willEnterRunMatchStart}";
            float now = Time.realtimeSinceStartup;
            if (key == _lastWaitingGateKey && now - _lastWaitingGateLogAt < 2f)
                return;

            _lastWaitingGateKey = key;
            _lastWaitingGateLogAt = now;

            bool gatePlayers = snap.currentPlayers >= maxPlayers;
            bool gateStatus = snap.status == "starting" || snap.status == "active";
            bool gateClockScheduled = TournamentServerClock.HasScheduledStart;
            bool gateClockReached = TournamentServerClock.IsServerStartTimeReached();

            Step(5, "playerCount == maxPlayers", gatePlayers,
                $"playerCount={snap.currentPlayers} maxPlayers={maxPlayers}");
            Step(5, "status == active|starting (for duel gate)", gateStatus,
                $"status={snap.status}");
            Step(5, "TournamentServerClock.HasScheduledStart", gateClockScheduled,
                $"match_start_at_ms={TournamentServerClock.ScheduledStartMs}");
            Step(5, "TournamentServerClock.IsServerStartTimeReached", gateClockReached,
                $"secondsUntilStart={TournamentServerClock.SecondsUntilStart():F2}");

            if (roomFull && !serverCountdown && !serverActive)
            {
                Debug.LogWarning(
                    $"{Tag} BLOCKED: room full but status still '{snap.status}' — " +
                    "RunMatchStartSequence will NOT run until status is starting|active");
            }

            if (willEnterRunMatchStart && !_runMatchStartLogged)
                LogRunMatchStartEntered(snap);
        }

        public static void LogWsEvent(string eventName, RoomResponseDto room)
        {
            if (room == null)
            {
                Step(3, $"WS event '{eventName}'", false, "room payload null");
                return;
            }

            bool pass = eventName is "room_updated" or "match_start" or "countdown";
            Step(3, $"WS event '{eventName}'", pass,
                $"status={room.status} players={room.playerCount} match_start_at_ms={room.matchStartAtMs}");
        }

        public static void LogRunMatchStartEntered(TournamentRoomSnapshot snap)
        {
            if (_runMatchStartLogged)
                return;
            _runMatchStartLogged = true;
            Debug.Log($"{Tag} RunMatchStartSequence ENTERED");
            LogRoomSnapshot("RunMatchStartSequence.enter", snap, snap.maxPlayers > 0 ? snap.maxPlayers : 2);
            LogClockState("RunMatchStartSequence.enter");
            Step(6, "RunMatchStartSequence executes", true);
        }

        public static void LogVsRevealStart()
        {
            if (_vsRevealLogged)
                return;
            _vsRevealLogged = true;
            Debug.Log($"{Tag} TournamentVsIntroView PlayVsRevealRoutine STARTED");
        }

        public static void LogVsRevealComplete()
        {
            if (_vsRevealDoneLogged)
                return;
            _vsRevealDoneLogged = true;
            Debug.Log($"{Tag} TournamentVsIntroView PlayVsRevealRoutine COMPLETE");
            Step(8, "TournamentVsIntroView VS reveal completes", true);
        }

        public static void LogCountdownStarted()
        {
            if (_countdownStartedLogged)
                return;
            _countdownStartedLogged = true;
            Debug.Log($"{Tag} PlayServerCountdownRoutine STARTED");
            LogClockState("PlayServerCountdownRoutine.start");
            Step(7, "PlayServerCountdownRoutine starts", true);
        }

        public static void LogCountdownFinished()
        {
            if (_countdownFinishedLogged)
                return;
            _countdownFinishedLogged = true;
            Debug.Log($"{Tag} PlayServerCountdownRoutine FINISHED");
            Step(7, "PlayServerCountdownRoutine finishes", true);
        }

        public static void LogLaunchGameFromWaitingRoom()
        {
            if (_launchLogged)
                return;
            _launchLogged = true;
            Debug.Log($"{Tag} LaunchGameFromWaitingRoom CALLED");
            Step(9, "LaunchGameFromWaitingRoom executes", true);
        }

        public static void LogServerStatusTransition(string fromStatus, string toStatus, string roomId, long? matchStartAtMs)
        {
            bool pass = (fromStatus == "waiting" && toStatus == "starting") ||
                        (fromStatus == "starting" && toStatus == "active");
            Step(1, $"Server status {fromStatus} -> {toStatus}", pass,
                $"room_id={roomId} match_start_at_ms={matchStartAtMs?.ToString() ?? "null"}");
            if ((toStatus == "starting" || toStatus == "active") && (matchStartAtMs ?? 0) > 0)
                Step(2, "match_start_at_ms generated", true, matchStartAtMs.ToString());
            else if (toStatus == "starting" || toStatus == "active")
                Step(2, "match_start_at_ms generated", false, "missing on server payload");
        }
    }
}
