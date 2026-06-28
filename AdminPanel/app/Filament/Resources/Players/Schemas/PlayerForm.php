<?php

namespace App\Filament\Resources\Players\Schemas;

use Filament\Forms\Components\TextInput;
use Filament\Forms\Components\Toggle;
use Filament\Schemas\Schema;

class PlayerForm
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->components([
                TextInput::make('display_name')->required()->maxLength(128),
                TextInput::make('email')->email()->maxLength(255),
                TextInput::make('username')->maxLength(64),
                TextInput::make('avatar_url')->url()->maxLength(512),
                Toggle::make('is_active')->default(true),
                Toggle::make('is_guest')->disabled(),
            ]);
    }
}
