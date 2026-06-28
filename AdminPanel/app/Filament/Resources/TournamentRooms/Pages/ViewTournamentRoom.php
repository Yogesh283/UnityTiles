<?php

namespace App\Filament\Resources\TournamentRooms\Pages;

use App\Filament\Resources\TournamentRooms\TournamentRoomResource;
use Filament\Actions\EditAction;
use Filament\Resources\Pages\ViewRecord;

class ViewTournamentRoom extends ViewRecord
{
    protected static string $resource = TournamentRoomResource::class;

    protected function getHeaderActions(): array
    {
        return [
            EditAction::make(),
        ];
    }
}
