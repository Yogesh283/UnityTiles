<?php

namespace App\Filament\Resources\Players\Tables;

use Filament\Actions\EditAction;
use Filament\Actions\ViewAction;
use Filament\Tables\Columns\IconColumn;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;

class PlayersTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->defaultSort('created_at', 'desc')
            ->columns([
                TextColumn::make('id')->sortable(),
                TextColumn::make('display_name')->searchable()->sortable(),
                TextColumn::make('email')->searchable(),
                TextColumn::make('wallet.balance')->label('Coins')->numeric()->sortable(),
                IconColumn::make('is_guest')->boolean()->label('Guest'),
                IconColumn::make('is_active')->boolean()->label('Active'),
                TextColumn::make('created_at')->dateTime()->sortable(),
            ])
            ->filters([])
            ->recordActions([
                ViewAction::make(),
                EditAction::make(),
            ]);
    }
}
