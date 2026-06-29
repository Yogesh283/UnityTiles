<?php

namespace App\Filament\Widgets;

use App\Support\DashboardMetrics;
use Filament\Widgets\StatsOverviewWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;

class AnalyticsOverview extends StatsOverviewWidget
{
    protected static bool $isDiscovered = false;

    protected static ?int $sort = 2;

    protected ?string $heading = 'Daily Report';

    protected function getDescription(): ?string
    {
        return 'Today\'s activity ('.now()->toFormattedDateString().')';
    }

    protected function getStats(): array
    {
        $m = DashboardMetrics::daily();

        return [
            Stat::make('New Players', number_format($m['new_players']))
                ->description(number_format($m['active_players']).' active today')
                ->color('success'),
            Stat::make('Matches Today', number_format($m['matches']))
                ->description(number_format($m['rooms_created']).' new rooms')
                ->color('warning'),
            Stat::make('IAP Today', number_format($m['iap_purchases']))
                ->description(number_format($m['iap_coins']).' coins · '.number_format($m['wallet_tx']).' wallet TX')
                ->color('info'),
            Stat::make('Entry Fees Today', number_format($m['entry_fees']))
                ->description(number_format($m['prizes_paid']).' coins paid as prizes')
                ->color('primary'),
            Stat::make('Active Rooms', number_format($m['active_rooms']))
                ->description('Live tournament rooms right now')
                ->color('danger'),
        ];
    }
}
