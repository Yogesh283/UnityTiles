<?php

namespace App\Support;

use App\Models\IapPurchase;
use App\Models\Player;
use App\Models\TournamentResult;
use App\Models\TournamentRoom;
use App\Models\Wallet;
use App\Models\WalletTransaction;
use Illuminate\Support\Carbon;

class DashboardMetrics
{
    public static function activeRoomCount(): int
    {
        return TournamentRoom::query()
            ->whereIn('status', ['waiting', 'starting', 'active'])
            ->count();
    }

    /** @return array<string, int|float> */
    public static function totals(): array
    {
        return [
            'players' => Player::query()->count(),
            'registered_players' => Player::query()->where('is_guest', false)->count(),
            'guest_players' => Player::query()->where('is_guest', true)->count(),
            'total_coins' => (int) Wallet::query()->sum('balance'),
            'iap_purchases' => IapPurchase::query()->count(),
            'iap_coins' => (int) IapPurchase::query()->sum('coins_added'),
            'rooms' => TournamentRoom::query()->count(),
            'matches' => TournamentResult::query()->count(),
            'active_rooms' => self::activeRoomCount(),
        ];
    }

    /** @return array<string, int|float> */
    public static function daily(?Carbon $date = null): array
    {
        $date ??= today();

        $entryFees = (int) WalletTransaction::query()
            ->whereDate('created_at', $date)
            ->where('type', 'tournament_entry')
            ->sum('amount');

        $prizesPaid = (int) WalletTransaction::query()
            ->whereDate('created_at', $date)
            ->where('type', 'tournament_prize')
            ->sum('amount');

        return [
            'new_players' => Player::query()->whereDate('created_at', $date)->count(),
            'active_players' => Player::query()->whereDate('updated_at', $date)->count(),
            'matches' => TournamentResult::query()->whereDate('created_at', $date)->count(),
            'rooms_created' => TournamentRoom::query()->whereDate('created_at', $date)->count(),
            'iap_purchases' => IapPurchase::query()->whereDate('created_at', $date)->count(),
            'iap_coins' => (int) IapPurchase::query()->whereDate('created_at', $date)->sum('coins_added'),
            'wallet_tx' => WalletTransaction::query()->whereDate('created_at', $date)->count(),
            'entry_fees' => abs($entryFees),
            'prizes_paid' => $prizesPaid,
            'active_rooms' => self::activeRoomCount(),
        ];
    }
}
