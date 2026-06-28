<?php

namespace App\Filament\Resources\AppLogs\Pages;

use App\Filament\Resources\AppLogs\AppLogResource;
use Filament\Resources\Pages\CreateRecord;

class CreateAppLog extends CreateRecord
{
    protected static string $resource = AppLogResource::class;
}
