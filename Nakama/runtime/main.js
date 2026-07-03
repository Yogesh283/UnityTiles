// Match IQ — Phase 2 Nakama realtime runtime (adapter-safe, FastAPI business logic unchanged)
// Handles only realtime: matchmaking room, presence, countdown, match start sync, match finished relay.

'use strict';

var OPCODE_HELLO = 1;
var OPCODE_ROOM_STATE = 10;
var OPCODE_MATCH_START = 11;
var OPCODE_MATCH_FINISHED = 12;
var OPCODE_REPORT_FINISH = 13;

var COUNTDOWN_SECONDS = 3;

function unixNowMs() {
    return Math.floor(new Date().getTime());
}

function safeParseInt(value, fallback) {
    var parsed = parseInt(value, 10);
    return isNaN(parsed) ? fallback : parsed;
}

function buildPlayersList(state) {
    var result = [];
    Object.keys(state.players).forEach(function (key) {
        result.push(state.players[key]);
    });
    return result;
}

function buildFinishResults(state) {
    var reports = state.finishReports || {};
    var players = buildPlayersList(state);
    var results = [];

    players.forEach(function (player) {
        var report = reports[player.user_uuid] || {};
        results.push({
            user_id: player.user_id,
            user_uuid: player.user_uuid,
            rank: safeParseInt(report.rank, 0),
            score: safeParseInt(report.score, 0),
            prize: safeParseInt(report.prize, 0),
            wallet_balance: report.wallet_balance != null ? safeParseInt(report.wallet_balance, 0) : null
        });
    });

    results.sort(function (a, b) {
        if (a.rank > 0 && b.rank > 0) return a.rank - b.rank;
        if (a.rank > 0) return -1;
        if (b.rank > 0) return 1;
        return b.score - a.score;
    });

    if (results.length > 0 && results[0].rank <= 0) {
        for (var i = 0; i < results.length; i++) {
            results[i].rank = i + 1;
        }
    }

    return results;
}

function broadcastMatchFinished(nk, dispatcher, state) {
    if (state.finishBroadcast) {
        return;
    }

    state.finishBroadcast = true;
    state.status = 'finished';

    var payload = {
        results: buildFinishResults(state)
    };

    dispatcher.broadcastMessage(
        OPCODE_MATCH_FINISHED,
        nk.stringToBinary(JSON.stringify(payload)),
        null,
        null,
        true
    );
}

function broadcastRoomState(nk, dispatcher, state) {
    var payload = {
        status: state.status,
        player_count: Object.keys(state.players).length,
        max_players: state.maxPlayers,
        start_countdown_seconds: state.status === 'starting' ? Math.max(0, Math.ceil((state.matchStartAtMs - unixNowMs()) / 1000)) : 0,
        match_start_at_ms: state.matchStartAtMs > 0 ? state.matchStartAtMs : null,
        server_now_ms: unixNowMs(),
        search_status: state.status === 'starting' ? 'match_found' : state.status,
        players: buildPlayersList(state)
    };

    dispatcher.broadcastMessage(
        OPCODE_ROOM_STATE,
        nk.stringToBinary(JSON.stringify(payload)),
        null,
        null,
        true
    );
}

function helloMatchInit(ctx, logger, nk, params) {
    var tournamentId = params && params.tournamentId ? params.tournamentId : 'duel_1v1';
    var maxPlayers = params && params.maxPlayers ? params.maxPlayers : 2;
    var invitedPlayers = params && params.invitedPlayers ? params.invitedPlayers : {};
    return {
        state: {
            players: {},
            helloCount: 0,
            status: 'waiting',
            countdownStartedAtMs: 0,
            matchStartAtMs: 0,
            tournamentId: tournamentId,
            maxPlayers: maxPlayers,
            invitedPlayers: invitedPlayers,
            finishReports: {},
            finishBroadcast: false
        },
        tickRate: 1,
        label: JSON.stringify({
            tournament_id: tournamentId,
            status: 'waiting',
            max_players: maxPlayers
        })
    };
}

function helloMatchJoinAttempt(ctx, logger, nk, dispatcher, tick, state, presence, metadata) {
    return { state: state, accept: true };
}

function helloMatchJoin(ctx, logger, nk, dispatcher, tick, state, presences) {
    presences.forEach(function (presence) {
        var invited = state.invitedPlayers[presence.userId] || {};
        var appUserId = safeParseInt(invited.app_user_id, safeParseInt(presence.username, 0));
        var displayName = invited.display_name ? String(invited.display_name) : (presence.username || ('Player ' + appUserId));
        state.players[presence.userId] = {
            user_id: appUserId,
            user_uuid: presence.userId,
            display_name: displayName,
            is_connected: true
        };
    });

    if (Object.keys(state.players).length >= state.maxPlayers && state.status === 'waiting') {
        state.status = 'starting';
        state.countdownStartedAtMs = unixNowMs();
        state.matchStartAtMs = state.countdownStartedAtMs + (COUNTDOWN_SECONDS * 1000);
    }

    broadcastRoomState(nk, dispatcher, state);
    return { state: state };
}

function helloMatchLeave(ctx, logger, nk, dispatcher, tick, state, presences) {
    presences.forEach(function (presence) {
        delete state.players[presence.userId];
        if (state.finishReports) {
            delete state.finishReports[presence.userId];
        }
    });
    if (state.status !== 'active' && state.status !== 'finished' && Object.keys(state.players).length < state.maxPlayers) {
        state.status = 'waiting';
        state.countdownStartedAtMs = 0;
        state.matchStartAtMs = 0;
    }
    broadcastRoomState(nk, dispatcher, state);
    return { state: state };
}

function helloMatchLoop(ctx, logger, nk, dispatcher, tick, state, messages) {
    if (state.status === 'starting' && state.matchStartAtMs > 0 && unixNowMs() >= state.matchStartAtMs) {
        state.status = 'active';
        var startPayload = {
            status: 'active',
            player_count: Object.keys(state.players).length,
            max_players: state.maxPlayers,
            start_countdown_seconds: 0,
            match_start_at_ms: state.matchStartAtMs,
            server_now_ms: unixNowMs(),
            search_status: 'starting',
            players: buildPlayersList(state)
        };
        dispatcher.broadcastMessage(
            OPCODE_MATCH_START,
            nk.stringToBinary(JSON.stringify(startPayload)),
            null,
            null,
            true
        );
    } else if (state.status !== 'finished') {
        broadcastRoomState(nk, dispatcher, state);
    }

    messages.forEach(function (message) {
        if (message.opCode === OPCODE_REPORT_FINISH) {
            try {
                var report = JSON.parse(nk.binaryToString(message.data));
                if (!state.finishReports) {
                    state.finishReports = {};
                }
                state.finishReports[message.sender.userId] = report || {};
                if (Object.keys(state.finishReports).length >= state.maxPlayers) {
                    broadcastMatchFinished(nk, dispatcher, state);
                }
            } catch (err) {
                logger.warn('MatchIQ finish report parse failed: %s', err);
            }
            return;
        }

        if (message.opCode !== OPCODE_HELLO) {
            return;
        }

        var text = nk.binaryToString(message.data);
        state.helloCount = (state.helloCount || 0) + 1;
        logger.info(
            'MatchIQ POC Hello from user=%s text=%s',
            message.sender.userId,
            text
        );

        dispatcher.broadcastMessage(
            OPCODE_HELLO,
            message.data,
            null,
            message.sender,
            true
        );
    });
    return { state: state };
}

function helloMatchTerminate(ctx, logger, nk, dispatcher, tick, state, graceSeconds) {
    logger.info('MatchIQ POC match terminate graceSeconds=%s', graceSeconds);
    if (!state.finishBroadcast && (state.status === 'active' || state.status === 'finished')) {
        broadcastMatchFinished(nk, dispatcher, state);
    }
    return { state: state };
}

function helloMatchSignal(ctx, logger, nk, dispatcher, tick, state, data) {
    return { state: state };
}

function onMatchmakerMatched(ctx, logger, nk, matches) {
    var first = matches[0];
    var tournamentId = 'duel_1v1';
    var maxPlayers = 2;
    if (first && first.properties) {
        if (first.properties.tournament_id) {
            tournamentId = String(first.properties.tournament_id);
        }
        if (first.properties.max_players) {
            maxPlayers = safeParseInt(first.properties.max_players, 2);
        }
    }

    var invitedPlayers = {};
    matches.forEach(function (m) {
        if (!m || !m.presence) return;
        invitedPlayers[m.presence.userId] = {
            app_user_id: m.properties && m.properties.app_user_id ? m.properties.app_user_id : '0',
            display_name: m.properties && m.properties.display_name ? m.properties.display_name : m.presence.username
        };
    });

    var matchId = nk.matchCreate('hello_match', {
        invited: matches,
        tournamentId: tournamentId,
        maxPlayers: maxPlayers,
        invitedPlayers: invitedPlayers
    });
    logger.info(
        'MatchIQ phase2 matched players=%s matchId=%s tournamentId=%s',
        matches.length,
        matchId,
        tournamentId
    );
    return matchId;
}

function InitModule(ctx, logger, nk, initializer) {
    logger.info('MatchIQ Phase 2 Nakama runtime loaded');

    initializer.registerMatch('hello_match', {
        matchInit: helloMatchInit,
        matchJoinAttempt: helloMatchJoinAttempt,
        matchJoin: helloMatchJoin,
        matchLeave: helloMatchLeave,
        matchLoop: helloMatchLoop,
        matchSignal: helloMatchSignal,
        matchTerminate: helloMatchTerminate
    });

    initializer.registerMatchmakerMatched(onMatchmakerMatched);
}
