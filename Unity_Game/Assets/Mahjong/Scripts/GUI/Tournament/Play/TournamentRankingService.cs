using System;
using System.Collections.Generic;
using System.Linq;
using Mkey;
using UnityEngine;

namespace Mkey.Tournament
{
    public static class TournamentRankingService
    {
        private static readonly string[] BotNames =
        {
            "TileMaster", "DragonFan", "BambooKing", "WindRider", "LotusPro",
            "GoldenPair", "SwiftMatch", "ZenPlayer", "ClashHero", "MahjongStar",
            "PandaWin", "JadeFox", "CoinHunter", "MatchWizard", "SolitairePro"
        };

        public static string GetBotName(int index) =>
            BotNames[index % BotNames.Length] + (index >= BotNames.Length ? (index + 1).ToString() : string.Empty);

        /// <summary>
        /// Rank by: fastest time → highest score → lowest moves → earliest server timestamp.
        /// </summary>
        public static void AssignRanks(List<TournamentParticipantResult> entries)
        {
            List<TournamentParticipantResult> sorted = entries
                .OrderBy(e => e.timeSeconds)
                .ThenByDescending(e => e.score)
                .ThenBy(e => e.moves)
                .ThenBy(e => e.completionServerMs)
                .ThenBy(e => e.name)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
                sorted[i].rank = i + 1;
        }

        public static TournamentMatchResult BuildFinalResult(
            TournamentDefinition tournament,
            TournamentMatchParticipant local,
            List<TournamentMatchParticipant> allParticipants,
            int prizeWon,
            int levelIndex)
        {
            var entries = allParticipants.Select(p => new TournamentParticipantResult
            {
                name = p.displayName,
                score = p.score,
                timeSeconds = p.timeSeconds,
                moves = p.moves,
                completionServerMs = p.completionServerMs,
                isPlayer = p.isLocal
            }).ToList();

            AssignRanks(entries);
            TournamentParticipantResult player = entries.First(e => e.isPlayer);

            return new TournamentMatchResult
            {
                tournamentId = tournament.id,
                tournamentName = tournament.displayName,
                maxPlayers = tournament.maxPlayers,
                levelIndex = levelIndex,
                playerRank = player.rank,
                playerScore = player.score,
                playerTimeSeconds = player.timeSeconds,
                playerMoves = player.moves,
                prizeWon = prizeWon,
                entryFee = tournament.entryFee,
                leaderboard = entries.OrderBy(e => e.rank).Take(15).ToList()
            };
        }
    }
}
