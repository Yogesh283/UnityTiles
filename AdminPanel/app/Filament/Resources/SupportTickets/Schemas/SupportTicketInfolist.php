<?php

namespace App\Filament\Resources\SupportTickets\Schemas;

use Filament\Infolists\Components\TextEntry;
use Filament\Schemas\Schema;

class SupportTicketInfolist
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->components([
                TextEntry::make('ticket_code')->label('Code')->copyable(),
                TextEntry::make('status')->badge()->color(fn (string $state): string => match ($state) {
                    'open' => 'warning',
                    'answered' => 'info',
                    'closed' => 'gray',
                    default => 'gray',
                }),
                TextEntry::make('player.display_name')->label('Player'),
                TextEntry::make('player.email')->label('Email')->placeholder('—'),
                TextEntry::make('category')->badge(),
                TextEntry::make('created_at')->dateTime(),
                TextEntry::make('subject')->columnSpanFull(),
                TextEntry::make('message')->columnSpanFull(),
                TextEntry::make('admin_reply')->label('Admin reply')->placeholder('No reply yet')->columnSpanFull(),
                TextEntry::make('reviewed_by')->placeholder('—'),
                TextEntry::make('reviewed_at')->dateTime()->placeholder('—'),
            ]);
    }
}
