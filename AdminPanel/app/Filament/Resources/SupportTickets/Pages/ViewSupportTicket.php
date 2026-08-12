<?php

namespace App\Filament\Resources\SupportTickets\Pages;

use App\Filament\Resources\SupportTickets\SupportTicketResource;
use Filament\Actions\Action;
use Filament\Forms\Components\Textarea;
use Filament\Resources\Pages\ViewRecord;

class ViewSupportTicket extends ViewRecord
{
    protected static string $resource = SupportTicketResource::class;

    protected function getHeaderActions(): array
    {
        return [
            Action::make('reply')
                ->label('Reply')
                ->icon('heroicon-o-chat-bubble-left-right')
                ->color('success')
                ->visible(fn () => in_array($this->record->status, ['open', 'answered'], true))
                ->form([
                    Textarea::make('admin_reply')
                        ->label('Reply to player')
                        ->required()
                        ->rows(5)
                        ->maxLength(4000)
                        ->default(fn () => $this->record->admin_reply),
                ])
                ->action(function (array $data): void {
                    $this->record->update([
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
                ->visible(fn () => $this->record->status !== 'closed')
                ->requiresConfirmation()
                ->action(function (): void {
                    $this->record->update([
                        'status' => 'closed',
                        'reviewed_by' => auth()->user()?->email ?? (string) auth()->id(),
                        'reviewed_at' => now(),
                    ]);
                }),
        ];
    }
}
