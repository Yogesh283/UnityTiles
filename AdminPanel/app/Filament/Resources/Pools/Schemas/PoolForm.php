<?php

namespace App\Filament\Resources\Pools\Schemas;

use Filament\Forms\Components\Select;
use Filament\Forms\Components\TextInput;
use Filament\Schemas\Schema;

class PoolForm
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->components([
                TextInput::make('name')
                    ->maxLength(160)
                    ->placeholder('IQFX Pro · ₹50 · 100 Players')
                    ->helperText('Leave blank to auto-name from entry fee and size.'),
                TextInput::make('icon')->maxLength(16)->default('🏆'),
                Select::make('entry_fee')
                    ->options([10 => '₹10', 50 => '₹50', 100 => '₹100'])
                    ->required()
                    ->native(false),
                Select::make('max_players')
                    ->label('Players')
                    ->options([10 => 10, 50 => 50, 100 => 100, 500 => 500, 1000 => 1000])
                    ->required()
                    ->native(false),
                TextInput::make('winners_count')
                    ->numeric()
                    ->minValue(1)
                    ->helperText('Blank = auto (≤50 → 1, ≤100 → 2, else 3).'),
                TextInput::make('distribution')
                    ->label('Distribution (%)')
                    ->placeholder('[60,25,15]')
                    ->helperText('JSON array of winner percentages. Blank = auto.'),
                TextInput::make('prize_share')
                    ->numeric()
                    ->default(0.70)
                    ->step(0.01)
                    ->helperText('Prize pool share of collection (0.70 = 70%).'),
                TextInput::make('prize_pool')
                    ->numeric()
                    ->disabled()
                    ->dehydrated(false)
                    ->helperText('Auto = players × entry × share.'),
                TextInput::make('level_index')->numeric()->default(0),
                Select::make('status')
                    ->options([
                        'open' => 'Open',
                        'running' => 'Running',
                        'finished' => 'Finished',
                        'cancelled' => 'Cancelled',
                    ])
                    ->default('open')
                    ->required()
                    ->native(false),
            ]);
    }
}
