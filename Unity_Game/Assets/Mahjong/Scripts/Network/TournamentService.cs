using System.Collections.Generic;
using System.Threading.Tasks;
using Mkey.Tournament;

namespace Mkey.Network
{
    public static class TournamentService
    {
        public static RoomResponseDto ActiveRoom { get; private set; }

        public static async Task<ApiResult<List<TournamentDefinition>>> FetchTournamentListAsync()
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<List<TournamentDefinition>>.Ok(TournamentCatalog.GetDefaultList());

            var result = await NetworkManager.Instance.GetAsync<List<TournamentDto>>("tournaments");
            if (!result.Success || result.Data == null)
                return ApiResult<List<TournamentDefinition>>.Fail(
                    result.ErrorMessage, result.StatusCode, result.IsServerUnavailable);

            var mapped = new List<TournamentDefinition>();
            foreach (TournamentDto dto in result.Data)
                mapped.Add(MapTournament(dto));

            TournamentCatalog.ApplyApiCatalog(mapped);
            return ApiResult<List<TournamentDefinition>>.Ok(mapped);
        }

        public static async Task<ApiResult<RoomResponseDto>> JoinTournamentAsync(string tournamentId)
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<RoomResponseDto>.Fail("Development mode enabled.");

            var body = new JoinTournamentRequestDto { tournamentId = tournamentId };
            var result = await NetworkManager.Instance.PostAsync<JoinTournamentRequestDto, RoomResponseDto>(
                "tournaments/join", body);

            if (result.Success)
                ActiveRoom = result.Data;

            return result;
        }

        public static async Task<ApiResult<RoomSnapshotDto>> FetchRoomSnapshotAsync(string roomId)
        {
            if (ApiConfig.Current.UseLocalSimulation || string.IsNullOrEmpty(roomId))
                return ApiResult<RoomSnapshotDto>.Fail("No API room.");

            return await NetworkManager.Instance.GetAsync<RoomSnapshotDto>(
                "tournaments/rooms/" + roomId, requireAuth: false);
        }

        public static async Task<ApiResult<SubmitScoreResponseDto>> SubmitScoreAsync(
            string roomId,
            int score,
            int moves,
            int elapsedSeconds)
        {
            if (ApiConfig.Current.UseLocalSimulation || string.IsNullOrEmpty(roomId))
            {
                return ApiResult<SubmitScoreResponseDto>.Ok(new SubmitScoreResponseDto
                {
                    ok = true,
                    finalized = true,
                    rank = 1,
                    prize = 0,
                    roomStatus = "finished"
                });
            }

            var body = new SubmitScoreRequestDto
            {
                roomId = roomId,
                score = score,
                moves = moves,
                elapsedSeconds = elapsedSeconds
            };

            return await NetworkManager.Instance.PostAsync<SubmitScoreRequestDto, SubmitScoreResponseDto>(
                "tournaments/submit-score", body);
        }

        public static async Task<ApiResult<List<TournamentHistoryDto>>> FetchHistoryAsync()
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<List<TournamentHistoryDto>>.Ok(new List<TournamentHistoryDto>());

            return await NetworkManager.Instance.GetAsync<List<TournamentHistoryDto>>("tournaments/history");
        }

        private static TournamentDefinition MapTournament(TournamentDto dto)
        {
            return new TournamentDefinition
            {
                id = dto.id,
                icon = dto.icon,
                displayName = dto.displayName,
                maxPlayers = dto.maxPlayers,
                entryFee = dto.entryFee,
                prizePool = dto.prizePool,
                platformFee = dto.platformFee,
                rewardInfo = dto.rewardInfo ?? string.Empty,
                waitingSeconds = dto.waitingSeconds,
                statusLabel = dto.statusLabel
            };
        }
    }
}
