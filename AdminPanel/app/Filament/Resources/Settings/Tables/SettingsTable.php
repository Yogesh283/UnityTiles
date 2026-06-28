<?php

namespace App\Filament\Resources\Settings\Tables;

use Filament\Actions\EditAction;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;

class SettingsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->columns([
                TextColumn::make('key')->searchable(),
                TextColumn::make('value')->limit(80),
                TextColumn::make('updated_at')->dateTime(),
            ])
            ->recordActions([
                EditAction::make(),
            ]);
    }
}
