<?php

namespace App\Filament\Resources\TournamentRooms\Pages;

use App\Filament\Resources\TournamentRooms\TournamentRoomResource;
use Filament\Actions\CreateAction;
use Filament\Resources\Pages\ListRecords;

class ListTournamentRooms extends ListRecords
{
    protected static string $resource = TournamentRoomResource::class;

    protected function getHeaderActions(): array
    {
        return [
            CreateAction::make(),
        ];
    }
}
