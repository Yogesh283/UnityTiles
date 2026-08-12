<?php

namespace App\Filament\Resources\SupportTickets\Tables;

use Filament\Actions\Action;
use Filament\Actions\ViewAction;
use Filament\Forms\Components\Textarea;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Filters\SelectFilter;
use Filament\Tables\Table;

class SupportTicketsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->defaultSort('created_at', 'desc')
            ->columns([
                TextColumn::make('ticket_code')->label('Code')->copyable()->searchable(),
                TextColumn::make('player.display_name')->label('Player')->searchable(),
                TextColumn::make('player.email')->label('Email')->searchable()->toggleable(),
                TextColumn::make('category')->badge()->sortable(),
                TextColumn::make('subject')->searchable()->limit(40)->wrap(),
                TextColumn::make('message')->label('Query')->limit(60)->wrap()->toggleable(),
                TextColumn::make('status')
                    ->badge()
                    ->color(fn (string $state): string => match ($state) {
                        'open' => 'warning',
                        'answered' => 'info',
                        'closed' => 'gray',
                        default => 'gray',
                    }),
                TextColumn::make('admin_reply')->label('Reply')->limit(40)->placeholder('—')->toggleable(),
                TextColumn::make('created_at')->dateTime()->sortable(),
            ])
            ->filters([
                SelectFilter::make('status')
                    ->options([
                        'open' => 'Open',
                        'answered' => 'Answered',
                        'closed' => 'Closed',
                    ])
                    ->default('open'),
                SelectFilter::make('category')
                    ->options([
                        'general' => 'General',
                        'payment' => 'Payment',
                        'game' => 'Game',
                        'account' => 'Account',
                        'other' => 'Other',
                    ]),
            ])
            ->recordActions([
                ViewAction::make(),
                Action::make('reply')
                    ->label('Reply')
                    ->icon('heroicon-o-chat-bubble-left-right')
                    ->color('success')
                    ->visible(fn ($record) => in_array($record->status, ['open', 'answered'], true))
                    ->form([
                        Textarea::make('admin_reply')
                            ->label('Reply to player')
                            ->required()
                            ->rows(5)
                            ->maxLength(4000)
                            ->default(fn ($record) => $record->admin_reply),
                    ])
                    ->modalHeading(fn ($record) => 'Reply · '.$record->ticket_code)
                    ->modalDescription(fn ($record) => ($record->player?->display_name ?: 'Player').': '.$record->subject)
                    ->action(function ($record, array $data): void {
                        $record->update([
                            'admin_reply' => $data['admin_reply'],
                            'status' => 'answered',
                            'reviewed_by' => auth()->user()?->email ?? (string) auth()->id(),
                            'reviewed_at' => now(),
                        ]);
                    }),
                Action::make('close')
                    ->label('Close')
                    ->icon('heroicon-o-check-circle')
                    ->color('gray')
                    ->visible(fn ($record) => $record->status !== 'closed')
                    ->requiresConfirmation()
                    ->modalHeading('Close this ticket?')
                    ->action(function ($record): void {
                        $record->update([
                            'status' => 'closed',
                            'reviewed_by' => auth()->user()?->email ?? (string) auth()->id(),
                            'reviewed_at' => now(),
                        ]);
                    }),
            ]);
    }
}
