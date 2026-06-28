using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mkey.Network;
using UnityEngine;

namespace Mkey.Tournament
{
    public static class TournamentHistoryService
    {
        private const string SaveKey = "mk_tournament_history";
        private const int MaxEntries = 50;

        private static List<TournamentHistoryEntry> apiCache;

        public static IReadOnlyList<TournamentHistoryEntry> GetAll()
        {
            if (apiCache != null && apiCache.Count > 0)
                return apiCache;

            if (!PlayerPrefs.HasKey(SaveKey)) return Array.Empty<TournamentHistoryEntry>();

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return Array.Empty<TournamentHistoryEntry>();

            TournamentHistorySaveData data = JsonUtility.FromJson<TournamentHistorySaveData>(json);
            return data?.entries ?? (IReadOnlyList<TournamentHistoryEntry>)Array.Empty<TournamentHistoryEntry>();
        }

        public static void ApplyApiHistory(List<TournamentHistoryDto> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                apiCache = null;
                return;
            }

            apiCache = rows.Select(MapApiEntry).ToList();
        }

        public static void SaveResult(TournamentMatchResult result)
        {
            if (result == null) return;

            if (ApiConfig.Current.UseLocalSimulation)
                SaveLocal(result);
        }

        private static void SaveLocal(TournamentMatchResult result)
        {
            TournamentHistorySaveData data = LoadData();
            data.entries.Insert(0, new TournamentHistoryEntry
            {
                tournamentId = result.tournamentId,
                tournamentName = result.tournamentName,
                rank = result.playerRank,
                maxPlayers = result.maxPlayers,
                score = result.playerScore,
                timeSeconds = result.playerTimeSeconds,
                moves = result.playerMoves,
                prizeWon = result.prizeWon,
                entryFee = result.entryFee,
                completedUtcTicks = DateTime.UtcNow.Ticks,
                levelIndex = result.levelIndex
            });

            if (data.entries.Count > MaxEntries)
                data.entries = data.entries.Take(MaxEntries).ToList();

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private static TournamentHistoryEntry MapApiEntry(TournamentHistoryDto dto)
        {
            long ticks = 0;
            if (!string.IsNullOrEmpty(dto.createdAt) &&
                DateTime.TryParse(dto.createdAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed))
            {
                ticks = parsed.ToUniversalTime().Ticks;
            }

            return new TournamentHistoryEntry
            {
                tournamentId = dto.tournamentId,
                tournamentName = dto.tournamentId,
                rank = dto.rank,
                score = dto.score,
                prizeWon = dto.prize,
                completedUtcTicks = ticks
            };
        }

        private static TournamentHistorySaveData LoadData()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return new TournamentHistorySaveData();

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new TournamentHistorySaveData();

            TournamentHistorySaveData data = JsonUtility.FromJson<TournamentHistorySaveData>(json);
            return data ?? new TournamentHistorySaveData();
        }
    }
}
