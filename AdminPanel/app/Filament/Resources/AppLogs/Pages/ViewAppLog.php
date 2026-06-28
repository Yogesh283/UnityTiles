<?php

namespace App\Filament\Resources\AppLogs\Pages;

use App\Filament\Resources\AppLogs\AppLogResource;
use Filament\Actions\EditAction;
use Filament\Resources\Pages\ViewRecord;

class ViewAppLog extends ViewRecord
{
    protected static string $resource = AppLogResource::class;

    protected function getHeaderActions(): array
    {
        return [
            EditAction::make(),
        ];
    }
}
