<?php

namespace App\Filament\Widgets;

use App\Models\IapPurchase;
use App\Models\Player;
use App\Models\TournamentRoom;
use App\Models\Wallet;
use Filament\Widgets\StatsOverviewWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;

class StatsOverview extends StatsOverviewWidget
{
    protected function getStats(): array
    {
        return [
            Stat::make('Players', Player::query()->count())
                ->description('Registered game users')
                ->color('success'),
            Stat::make('Active Rooms', TournamentRoom::query()->whereIn('status', ['waiting', 'starting', 'active'])->count())
                ->description('Live tournament rooms')
                ->color('warning'),
            Stat::make('Total Coins', number_format((int) Wallet::query()->sum('balance')))
                ->description('Coins in all wallets')
                ->color('primary'),
            Stat::make('IAP Today', IapPurchase::query()->whereDate('created_at', today())->count())
                ->description('Play Store purchases today')
                ->color('info'),
        ];
    }
}
