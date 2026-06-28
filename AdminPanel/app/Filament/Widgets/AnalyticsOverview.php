<?php

namespace App\Filament\Widgets;

use App\Models\IapPurchase;
use App\Models\Player;
use App\Models\TournamentRoom;
use App\Models\Wallet;
use App\Models\WalletTransaction;
use Filament\Widgets\StatsOverviewWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;
use Illuminate\Support\Facades\DB;

class AnalyticsOverview extends StatsOverviewWidget
{
    protected function getStats(): array
    {
        $txToday = WalletTransaction::query()->whereDate('created_at', today())->count();
        $iapRevenue = IapPurchase::query()->whereDate('created_at', today())->sum('coins_added');

        return [
            Stat::make('DAU (approx)', Player::query()->whereDate('updated_at', today())->count())
                ->description('Players active today'),
            Stat::make('Wallet TX Today', $txToday)
                ->description('Coin transactions'),
            Stat::make('Coins Purchased Today', number_format($iapRevenue))
                ->description('Via Play Store'),
            Stat::make('Active Rooms', TournamentRoom::query()->whereIn('status', ['waiting', 'starting', 'active'])->count())
                ->description('Live tournaments'),
        ];
    }
}
