<?php

namespace App\Filament\Resources\Notifications\Schemas;

use Filament\Forms\Components\Textarea;
use Filament\Forms\Components\TextInput;
use Filament\Forms\Components\Toggle;
use Filament\Schemas\Schema;

class NotificationForm
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->components([
                TextInput::make('user_id')->numeric()->label('Player ID (blank = broadcast)'),
                TextInput::make('title')->required()->maxLength(255),
                Textarea::make('body')->required()->rows(4),
                Toggle::make('is_read')->default(false),
            ]);
    }
}
