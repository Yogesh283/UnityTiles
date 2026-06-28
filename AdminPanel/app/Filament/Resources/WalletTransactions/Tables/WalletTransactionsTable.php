<?php

namespace App\Filament\Resources\WalletTransactions\Tables;

use Filament\Actions\ViewAction;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;

class WalletTransactionsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->defaultSort('created_at', 'desc')
            ->columns([
                TextColumn::make('id')->sortable(),
                TextColumn::make('player.display_name')->label('Player')->searchable(),
                TextColumn::make('amount')->numeric()->color(fn (int $state): string => $state >= 0 ? 'success' : 'danger'),
                TextColumn::make('balance_after')->numeric(),
                TextColumn::make('type')->badge(),
                TextColumn::make('reference_id')->toggleable(),
                TextColumn::make('note')->limit(40)->toggleable(),
                TextColumn::make('created_at')->dateTime()->sortable(),
            ])
            ->recordActions([
                ViewAction::make(),
            ]);
    }
}
