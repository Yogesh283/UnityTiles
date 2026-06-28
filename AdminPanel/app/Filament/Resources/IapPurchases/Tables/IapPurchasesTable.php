<?php

namespace App\Filament\Resources\IapPurchases\Tables;

use Filament\Actions\ViewAction;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;

class IapPurchasesTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->defaultSort('created_at', 'desc')
            ->columns([
                TextColumn::make('id')->sortable(),
                TextColumn::make('player.display_name')->label('Player')->searchable(),
                TextColumn::make('product_id')->badge(),
                TextColumn::make('coins_added')->numeric(),
                TextColumn::make('order_id')->copyable()->limit(24),
                TextColumn::make('platform')->badge(),
                TextColumn::make('created_at')->dateTime()->sortable(),
            ])
            ->recordActions([
                ViewAction::make(),
            ]);
    }
}
