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
        waiting_seconds=300,
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


def _wxo_room(
    room_id: str,
    label: str,
    players: int,
    entry: int,
    *,
    icon: str = "🎯",
    live: bool = False,
) -> TournamentDefinition:
    """
    WXO shared-wallet room. Winner takes 2× entry, the rest of the pot is platform fee.
    Room ids mirror the web match lobby so one lobby drives web and app alike.
    """
    pool = entry * players
    winner_take = entry * 2
    return TournamentDefinition(
        id=room_id,
        icon="📺" if live else icon,
        display_name=label,
        max_players=players,
        entry_fee=entry,
        prize_pool=winner_take,
        platform_fee=max(0, pool - winner_take),
        reward_info="Winner takes 2× entry" if entry else "Free practice",
        waiting_seconds=90 if players > 2 else 300,
        status_label="OPEN",
    )


# Rooms offered by the WXO match lobby (web + app share these ids)
WXO_ROOM_CATALOG: list[TournamentDefinition] = [
    _wxo_room("wxo_free", "Free Practice", 2, 0, icon="🎮"),
    _wxo_room("wxo_duel2", "2 Players", 2, 25, icon="⚔️"),
    _wxo_room("wxo_squad5", "5 Players", 5, 50),
    _wxo_room("wxo_room10", "10 Players", 10, 100),
    _wxo_room("wxo_room20", "20 Players", 20, 200),
    _wxo_room("wxo_live5", "Live Stream • 5", 5, 75, live=True),
    _wxo_room("wxo_live10", "Live Stream • 10", 10, 150, live=True),
]

TOURNAMENT_CATALOG.extend(WXO_ROOM_CATALOG)


def get_tournament(tournament_id: str) -> TournamentDefinition | None:
    return next((t for t in TOURNAMENT_CATALOG if t.id == tournament_id), None)


def is_instant_duel(tournament_id: str) -> bool:
    """Head-to-head room: player enters game immediately; match syncs when opponent joins."""
    if tournament_id == "duel_1v1":
        return True
    tournament = next((t for t in TOURNAMENT_CATALOG if t.id == tournament_id), None)
    return bool(tournament and tournament.max_players == 2)
