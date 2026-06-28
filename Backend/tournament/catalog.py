from dataclasses import dataclass


@dataclass(frozen=True)
class TournamentDefinition:
    id: str
    icon: str
    display_name: str
    max_players: int
    entry_fee: int
    prize_pool: int
    platform_fee: int
    reward_info: str
    waiting_seconds: int
    status_label: str


TOURNAMENT_CATALOG: list[TournamentDefinition] = [
    TournamentDefinition(
        id="duel_1v1",
        icon="⚔️",
        display_name="1 vs 1 Duel",
        max_players=2,
        entry_fee=100,
        prize_pool=160,
        platform_fee=40,
        reward_info="",
        waiting_seconds=12,
        status_label="OPEN",
    ),
    TournamentDefinition(
        id="quick_cup",
        icon="⚡",
        display_name="Quick Cup",
        max_players=10,
        entry_fee=100,
        prize_pool=800,
        platform_fee=0,
        reward_info="Top 3 Win",
        waiting_seconds=20,
        status_label="OPEN",
    ),
    TournamentDefinition(
        id="mega_clash",
        icon="🔥",
        display_name="Mega Clash",
        max_players=50,
        entry_fee=200,
        prize_pool=8000,
        platform_fee=0,
        reward_info="Top 10 Win",
        waiting_seconds=30,
        status_label="FILLING",
    ),
    TournamentDefinition(
        id="grand_clash",
        icon="👑",
        display_name="Grand Clash",
        max_players=100,
        entry_fee=500,
        prize_pool=40000,
        platform_fee=0,
        reward_info="Top 20 Win",
        waiting_seconds=45,
        status_label="FILLING",
    ),
    TournamentDefinition(
        id="championship",
        icon="💎",
        display_name="Championship",
        max_players=500,
        entry_fee=1000,
        prize_pool=400000,
        platform_fee=0,
        reward_info="Top 100 Win",
        waiting_seconds=60,
        status_label="STARTING SOON",
    ),
    TournamentDefinition(
        id="world_cup",
        icon="🌍",
        display_name="World Cup",
        max_players=1000,
        entry_fee=2000,
        prize_pool=1600000,
        platform_fee=0,
        reward_info="Top 200 Win",
        waiting_seconds=90,
        status_label="FULL",
    ),
]


def get_tournament(tournament_id: str) -> TournamentDefinition | None:
    return next((t for t in TOURNAMENT_CATALOG if t.id == tournament_id), None)
