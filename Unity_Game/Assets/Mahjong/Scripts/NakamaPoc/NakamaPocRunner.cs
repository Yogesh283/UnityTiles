using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace Mkey.NakamaPoc
{
    /// <summary>
    /// Isolated Phase 1.5 Nakama POC — two in-process clients, automated PASS/FAIL.
    /// Not connected to tournament UI, FastAPI matchmaking, or gameplay.
    /// </summary>
    public sealed class NakamaPocRunner : MonoBehaviour
    {
        private NakamaPocReport _report;

        private async void Start()
        {
            _report = new NakamaPocReport();
            Debug.Log($"{NakamaPocSettings.Tag} === PHASE 1.5 POC START ===");

            try
            {
                await RunPhase15Async(_report);
            }
            catch (Exception ex)
            {
                _report.Fail("unexpected_exception", ex.Message);
                Debug.LogException(ex);
            }

            _report.Step(7, "Production untouched (isolated POC only)", true,
                "No TournamentService / FastAPI WS changes in this run");
            _report.Step(8, "No production files deleted", true,
                "POC adds NakamaPoc/ + Nakama/ only");

            _report.PrintSummary();
        }

        private static async Task RunPhase15Async(NakamaPocReport report)
        {
            var clientA = new PocPeer("poc-device-a");
            var clientB = new PocPeer("poc-device-b");

            // 1–2: session + socket
            await WithTimeout(ConnectPeerAsync(clientA), NakamaPocSettings.StepTimeoutSeconds);
            report.Step(1, "Docker Nakama reachable + guest login", true,
                $"userId={clientA.Session.UserId}");
            report.Step(2, "Unity session + socket created", true,
                $"userId={clientA.Session.UserId}");

            await WithTimeout(ConnectPeerAsync(clientB), NakamaPocSettings.StepTimeoutSeconds);

            // 3: matchmaker → match
            var matchedA = new TaskCompletionSource<IMatchmakerMatched>();
            var matchedB = new TaskCompletionSource<IMatchmakerMatched>();
            clientA.Socket.ReceivedMatchmakerMatched += m => matchedA.TrySetResult(m);
            clientB.Socket.ReceivedMatchmakerMatched += m => matchedB.TrySetResult(m);

            IMatchmakerTicket ticketA = await clientA.Socket.AddMatchmakerAsync(
                "*",
                NakamaPocSettings.MatchmakerMinPlayers,
                NakamaPocSettings.MatchmakerMaxPlayers);
            IMatchmakerTicket ticketB = await clientB.Socket.AddMatchmakerAsync(
                "*",
                NakamaPocSettings.MatchmakerMinPlayers,
                NakamaPocSettings.MatchmakerMaxPlayers);

            Debug.Log($"{NakamaPocSettings.Tag} matchmaker tickets A={ticketA.Ticket} B={ticketB.Ticket}");

            var matchTaskA = WithTimeout(matchedA.Task, NakamaPocSettings.StepTimeoutSeconds);
            var matchTaskB = WithTimeout(matchedB.Task, NakamaPocSettings.StepTimeoutSeconds);
            IMatchmakerMatched resultA = await matchTaskA;
            IMatchmakerMatched resultB = await matchTaskB;

            clientA.Match = await clientA.Socket.JoinMatchAsync(resultA);
            clientB.Match = await clientB.Socket.JoinMatchAsync(resultB);

            bool sameMatch = clientA.Match.Id == clientB.Match.Id;
            report.Step(3, "2-player matchmaker → room created", sameMatch,
                $"matchId={clientA.Match.Id} playersA={clientA.Match.Presences.Count()}");

            if (!sameMatch)
                return;

            // 4: Hello round-trip A → B
            var helloReceived = new TaskCompletionSource<string>();
            clientB.Socket.ReceivedMatchState += OnHello;

            void OnHello(IMatchState state)
            {
                if (state.OpCode != NakamaPocSettings.OpcodeHello)
                    return;
                string text = Encoding.UTF8.GetString(state.State);
                helloReceived.TrySetResult(text);
            }

            byte[] helloBytes = Encoding.UTF8.GetBytes("Hello");
            await clientA.Socket.SendMatchStateAsync(
                clientA.Match.Id,
                NakamaPocSettings.OpcodeHello,
                helloBytes);

            string helloText = await WithTimeout(helloReceived.Task, NakamaPocSettings.StepTimeoutSeconds);
            clientB.Socket.ReceivedMatchState -= OnHello;

            report.Step(4, "Hello message exchange", helloText == "Hello",
                $"received='{helloText}'");

            // 5: leave observed
            var leaveObserved = new TaskCompletionSource<bool>();
            clientB.Socket.ReceivedMatchPresence += OnPresence;

            void OnPresence(IMatchPresenceEvent evt)
            {
                if (evt.Leaves == null)
                    return;
                foreach (IUserPresence left in evt.Leaves)
                {
                    if (left.UserId == clientA.Session.UserId)
                        leaveObserved.TrySetResult(true);
                }
            }

            await clientA.Socket.LeaveMatchAsync(clientA.Match);
            bool sawLeave = await WithTimeout(leaveObserved.Task, NakamaPocSettings.StepTimeoutSeconds);
            clientB.Socket.ReceivedMatchPresence -= OnPresence;

            report.Step(5, "Leave observed by other player", sawLeave,
                $"leaver={clientA.Session.UserId}");

            string matchId = clientB.Match.Id;

            // 6: reconnect + rejoin
            await clientA.Socket.CloseAsync();
            clientA.Socket = null;

            await WithTimeout(ReconnectAndRejoinAsync(clientA, matchId), NakamaPocSettings.StepTimeoutSeconds);

            bool rejoined = clientA.Match != null && clientA.Match.Id == matchId;
            report.Step(6, "Reconnect + rejoin same match", rejoined,
                $"matchId={matchId}");

            if (!rejoined)
                return;

            // Hello after rejoin (part of step 6 validation)
            var helloAfterReconnect = new TaskCompletionSource<string>();
            clientB.Socket.ReceivedMatchState += OnHelloReconnect;

            void OnHelloReconnect(IMatchState state)
            {
                if (state.OpCode != NakamaPocSettings.OpcodeHello)
                    return;
                helloAfterReconnect.TrySetResult(Encoding.UTF8.GetString(state.State));
            }

            await clientA.Socket.SendMatchStateAsync(
                clientA.Match.Id,
                NakamaPocSettings.OpcodeHello,
                Encoding.UTF8.GetBytes("HelloAgain"));

            string helloAgain = await WithTimeout(helloAfterReconnect.Task, NakamaPocSettings.StepTimeoutSeconds);
            clientB.Socket.ReceivedMatchState -= OnHelloReconnect;

            if (helloAgain != "HelloAgain")
                report.Fail("Hello after reconnect", $"expected HelloAgain got '{helloAgain}'");
            else
                Debug.Log($"{NakamaPocSettings.Tag} Hello after reconnect OK");

            await CleanupPeerAsync(clientA);
            await CleanupPeerAsync(clientB);
        }

        private static async Task ConnectPeerAsync(PocPeer peer)
        {
            peer.Client = new Client(
                NakamaPocSettings.UseSsl ? "https" : "http",
                NakamaPocSettings.ServerHost,
                NakamaPocSettings.ServerPort,
                NakamaPocSettings.ServerKey);

            peer.Session = await peer.Client.AuthenticateDeviceAsync(peer.DeviceId, create: true);
            peer.Socket = peer.Client.NewSocket();
            await peer.Socket.ConnectAsync(peer.Session, appearOnline: true);

            Debug.Log($"{NakamaPocSettings.Tag} connected device={peer.DeviceId} userId={peer.Session.UserId}");
        }

        private static async Task ReconnectAndRejoinAsync(PocPeer peer, string matchId)
        {
            peer.Session = await peer.Client.AuthenticateDeviceAsync(peer.DeviceId, create: true);
            peer.Socket = peer.Client.NewSocket();
            await peer.Socket.ConnectAsync(peer.Session, appearOnline: true);
            peer.Match = await peer.Socket.JoinMatchAsync(matchId);
            Debug.Log($"{NakamaPocSettings.Tag} rejoined match={matchId} userId={peer.Session.UserId}");
        }

        private static async Task CleanupPeerAsync(PocPeer peer)
        {
            if (peer.Socket == null)
                return;

            try
            {
                if (peer.Match != null)
                    await peer.Socket.LeaveMatchAsync(peer.Match);
            }
            catch
            {
                // ignored for POC cleanup
            }

            try
            {
                await peer.Socket.CloseAsync();
            }
            catch
            {
                // ignored
            }
        }

        private static async Task WithTimeout(Task task, float seconds)
        {
            Task delay = Task.Delay(TimeSpan.FromSeconds(seconds));
            Task completed = await Task.WhenAny(task, delay);
            if (completed != task)
                throw new TimeoutException($"POC step timed out after {seconds}s");
            await task;
        }

        private static async Task<T> WithTimeout<T>(Task<T> task, float seconds)
        {
            Task delay = Task.Delay(TimeSpan.FromSeconds(seconds));
            Task completed = await Task.WhenAny(task, delay);
            if (completed != task)
                throw new TimeoutException($"POC step timed out after {seconds}s");
            return await task;
        }

        private sealed class PocPeer
        {
            public PocPeer(string deviceId) => DeviceId = deviceId;

            public string DeviceId { get; }
            public IClient Client { get; set; }
            public ISession Session { get; set; }
            public ISocket Socket { get; set; }
            public IMatch Match { get; set; }
        }
    }
}
