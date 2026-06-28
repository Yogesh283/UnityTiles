<?php

namespace App\Filament\Resources\Tournaments\Tables;

use Filament\Actions\EditAction;
use Filament\Actions\ViewAction;
use Filament\Tables\Columns\IconColumn;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;

class TournamentsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->columns([
                TextColumn::make('id')->searchable(),
                TextColumn::make('icon'),
                TextColumn::make('display_name')->searchable(),
                TextColumn::make('max_players')->numeric(),
                TextColumn::make('entry_fee')->numeric(),
                TextColumn::make('prize_pool')->numeric(),
                TextColumn::make('status_label')->badge(),
                IconColumn::make('is_active')->boolean(),
            ])
            ->recordActions([
                ViewAction::make(),
                EditAction::make(),
            ]);
    }
}
