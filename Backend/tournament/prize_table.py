def _share_equal(pool: int, rank_from: int, rank_to: int, rank: int) -> int:
    if rank < rank_from or rank > rank_to:
        return 0
    slots = rank_to - rank_from + 1
    base_share = pool // slots
    remainder = pool - base_share * slots
    return base_share + remainder if rank == rank_from else base_share


def _championship_prize(rank: int) -> int:
    if rank == 1:
        return 80000
    if rank == 2:
        return 50000
    if rank == 3:
        return 30000
    if 4 <= rank <= 100:
        return _share_equal(240000, 4, 100, rank)
    return 0


def _world_cup_prize(rank: int) -> int:
    if rank == 1:
        return 200000
    if rank == 2:
        return 120000
    if rank == 3:
        return 80000
    if 4 <= rank <= 200:
        return _share_equal(1200000, 4, 200, rank)
    return 0


def get_prize(tournament_id: str, rank: int) -> int:
    if not tournament_id or rank < 1:
        return 0

    match tournament_id:
        case "duel_1v1":
            return 160 if rank == 1 else 0
        case "quick_cup":
            if rank == 1:
                return 500
            if rank == 2:
                return 200
            if rank == 3:
                return 100
            return 0
        case "mega_clash":
            if rank == 1:
                return 2500
            if rank == 2:
                return 1500
            if rank == 3:
                return 1000
            if 4 <= rank <= 10:
                return _share_equal(3000, 4, 10, rank)
            return 0
        case "grand_clash":
            if rank == 1:
                return 10000
            if rank == 2:
                return 6000
            if rank == 3:
                return 4000
            if 4 <= rank <= 20:
                return _share_equal(20000, 4, 20, rank)
            return 0
        case "championship":
            return _championship_prize(rank)
        case "world_cup":
            return _world_cup_prize(rank)
        case _:
            return 0


def get_paid_rank_count(tournament_id: str) -> int:
    return {
        "duel_1v1": 1,
        "quick_cup": 3,
        "mega_clash": 10,
        "grand_clash": 20,
        "championship": 100,
        "world_cup": 200,
    }.get(tournament_id, 0)


def is_winning_rank(tournament_id: str, rank: int) -> bool:
    return get_prize(tournament_id, rank) > 0
