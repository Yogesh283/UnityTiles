<?php

namespace App\Filament\Resources\TournamentRooms\Pages;

use App\Filament\Resources\TournamentRooms\TournamentRoomResource;
use Filament\Actions\DeleteAction;
use Filament\Actions\ViewAction;
use Filament\Resources\Pages\EditRecord;

class EditTournamentRoom extends EditRecord
{
    protected static string $resource = TournamentRoomResource::class;

    protected function getHeaderActions(): array
    {
        return [
            ViewAction::make(),
            DeleteAction::make(),
        ];
    }
}
