using System.Collections.Generic;

namespace Mkey.Tournament
{
    public static class TournamentCatalog
    {
        private static List<TournamentDefinition> apiCatalog;

        public static IReadOnlyList<TournamentDefinition> All =>
            apiCatalog != null && apiCatalog.Count > 0 ? apiCatalog : GetDefaultList();

        public static void ApplyApiCatalog(List<TournamentDefinition> items)
        {
            apiCatalog = items != null && items.Count > 0 ? new List<TournamentDefinition>(items) : null;
        }

        public static void ResetToDefaults()
        {
            apiCatalog = null;
        }

        public static List<TournamentDefinition> GetDefaultList()
        {
            return new List<TournamentDefinition>
            {
                new TournamentDefinition
                {
                    id = "duel_1v1",
                    icon = "⚔️",
                    displayName = "1 vs 1 Duel",
                    maxPlayers = 2,
                    entryFee = 100,
                    prizePool = 160,
                    platformFee = 40,
                    rewardInfo = "",
                    waitingSeconds = 12,
                    statusLabel = "OPEN"
                },
                new TournamentDefinition
                {
                    id = "quick_cup",
                    icon = "⚡",
                    displayName = "Quick Cup",
                    maxPlayers = 10,
                    entryFee = 100,
                    prizePool = 800,
                    platformFee = 0,
                    rewardInfo = "Top 3 Win",
                    waitingSeconds = 20,
                    statusLabel = "OPEN"
                },
                new TournamentDefinition
                {
                    id = "mega_clash",
                    icon = "🔥",
                    displayName = "Mega Clash",
                    maxPlayers = 50,
                    entryFee = 200,
                    prizePool = 8000,
                    platformFee = 0,
                    rewardInfo = "Top 10 Win",
                    waitingSeconds = 30,
                    statusLabel = "FILLING"
                },
                new TournamentDefinition
                {
                    id = "grand_clash",
                    icon = "👑",
                    displayName = "Grand Clash",
                    maxPlayers = 100,
                    entryFee = 500,
                    prizePool = 40000,
                    platformFee = 0,
                    rewardInfo = "Top 20 Win",
                    waitingSeconds = 45,
                    statusLabel = "FILLING"
                },
                new TournamentDefinition
                {
                    id = "championship",
                    icon = "💎",
                    displayName = "Championship",
                    maxPlayers = 500,
                    entryFee = 1000,
                    prizePool = 400000,
                    platformFee = 0,
                    rewardInfo = "Top 100 Win",
                    waitingSeconds = 60,
                    statusLabel = "STARTING SOON"
                },
                new TournamentDefinition
                {
                    id = "world_cup",
                    icon = "🌍",
                    displayName = "World Cup",
                    maxPlayers = 1000,
                    entryFee = 2000,
                    prizePool = 1600000,
                    platformFee = 0,
                    rewardInfo = "Top 200 Win",
                    waitingSeconds = 90,
                    statusLabel = "FULL"
                }
            };
        }

        public static readonly (string icon, string label)[] TournamentTypes =
        {
            ("📅", "Daily Tournament"),
            ("⏱", "Hourly Tournament"),
            ("🏆", "Weekly Championship"),
            ("👑", "VIP Tournament"),
            ("🎁", "Free Tournament"),
            ("⚔️", "Knockout Event"),
            ("📊", "Leaderboard Rewards"),
            ("🎫", "Tournament Tickets")
        };

        public static readonly (string icon, string title, string body)[] InfoCards =
        {
            ("🛡", "Fair Play", "Every player gets the same Mahjong layout."),
            ("🏆", "Play • Compete • Win", "Compete with players worldwide and earn rewards."),
            ("🔒", "Secure Tournament", "Real-time ranking and secure reward distribution.")
        };
    }
}
