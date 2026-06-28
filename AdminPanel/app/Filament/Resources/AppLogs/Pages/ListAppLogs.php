<?php

namespace App\Filament\Resources\AppLogs\Pages;

use App\Filament\Resources\AppLogs\AppLogResource;
use Filament\Actions\CreateAction;
use Filament\Resources\Pages\ListRecords;

class ListAppLogs extends ListRecords
{
    protected static string $resource = AppLogResource::class;

    protected function getHeaderActions(): array
    {
        return [
            CreateAction::make(),
        ];
    }
}
