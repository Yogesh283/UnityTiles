<?php

namespace App\Filament\Resources\AppLogs\Pages;

use App\Filament\Resources\AppLogs\AppLogResource;
use Filament\Actions\DeleteAction;
use Filament\Actions\ViewAction;
use Filament\Resources\Pages\EditRecord;

class EditAppLog extends EditRecord
{
    protected static string $resource = AppLogResource::class;

    protected function getHeaderActions(): array
    {
        return [
            ViewAction::make(),
            DeleteAction::make(),
        ];
    }
}
