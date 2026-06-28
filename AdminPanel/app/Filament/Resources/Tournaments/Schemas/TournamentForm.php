<?php

namespace App\Filament\Resources\Tournaments\Schemas;

use Filament\Forms\Components\TextInput;
use Filament\Forms\Components\Toggle;
use Filament\Schemas\Schema;

class TournamentForm
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->components([
                TextInput::make('id')->required()->maxLength(64)->disabledOn('edit'),
                TextInput::make('display_name')->required()->maxLength(128),
                TextInput::make('icon')->maxLength(16),
                TextInput::make('max_players')->numeric()->required(),
                TextInput::make('entry_fee')->numeric()->required(),
                TextInput::make('prize_pool')->numeric()->required(),
                TextInput::make('platform_fee')->numeric()->default(0),
                TextInput::make('reward_info')->maxLength(255),
                TextInput::make('waiting_seconds')->numeric()->default(30),
                TextInput::make('status_label')->maxLength(64),
                Toggle::make('is_active')->default(true),
            ]);
    }
}
