<?php

namespace App\Filament\Resources\TournamentRooms\Tables;

use Filament\Actions\ViewAction;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;

class TournamentRoomsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->defaultSort('created_at', 'desc')
            ->columns([
                TextColumn::make('id')->searchable(),
                TextColumn::make('tournament.display_name')->label('Tournament'),
                TextColumn::make('status')->badge(),
                TextColumn::make('level_index')->numeric(),
                TextColumn::make('max_players')->numeric(),
                TextColumn::make('started_at')->dateTime(),
                TextColumn::make('created_at')->dateTime()->sortable(),
            ])
            ->recordActions([
                ViewAction::make(),
            ]);
    }
}
