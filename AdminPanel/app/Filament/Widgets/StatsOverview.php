<?php

namespace App\Filament\Widgets;

use App\Support\DashboardMetrics;
use Filament\Widgets\StatsOverviewWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;

class StatsOverview extends StatsOverviewWidget
{
    protected static bool $isDiscovered = false;

    protected static ?int $sort = 1;

    protected ?string $heading = 'Total Report';

    protected ?string $description = 'All-time totals across Match IQ';

    protected function getStats(): array
    {
        $m = DashboardMetrics::totals();

        return [
            Stat::make('Total Players', number_format($m['players']))
                ->description($m['registered_players'].' registered · '.$m['guest_players'].' guests')
                ->color('success'),
            Stat::make('Total Coins', number_format($m['total_coins']))
                ->description('Coins in all wallets')
                ->color('primary'),
            Stat::make('Play Store Purchases', number_format($m['iap_purchases']))
                ->description(number_format($m['iap_coins']).' coins purchased')
                ->color('info'),
            Stat::make('Tournament Matches', number_format($m['matches']))
                ->description(number_format($m['rooms']).' rooms created')
                ->color('warning'),
            Stat::make('Active Rooms', number_format($m['active_rooms']))
                ->description('Live now (waiting / starting / active)')
                ->color('danger'),
        ];
    }
}
