<?php

namespace App\Filament\Resources\Pools\Tables;

use Filament\Actions\EditAction;
use Filament\Actions\ViewAction;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;

class PoolsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->columns([
                TextColumn::make('id')->sortable(),
                TextColumn::make('icon'),
                TextColumn::make('name')->searchable()->limit(40),
                TextColumn::make('entry_fee')->money('INR')->sortable(),
                TextColumn::make('max_players')->label('Players')->numeric()->sortable(),
                TextColumn::make('entries_count')
                    ->label('Joined')
                    ->counts('entries'),
                TextColumn::make('winners_count')->label('Winners')->numeric(),
                TextColumn::make('prize_pool')->money('INR')->sortable(),
                TextColumn::make('status')
                    ->badge()
                    ->color(fn (string $state): string => match ($state) {
                        'open' => 'success',
                        'running' => 'warning',
                        'finished' => 'gray',
                        'cancelled' => 'danger',
                        default => 'gray',
                    }),
                TextColumn::make('created_at')->dateTime()->sortable()->toggleable(),
            ])
            ->defaultSort('id', 'desc')
            ->recordActions([
                ViewAction::make(),
                EditAction::make(),
            ]);
    }
}
