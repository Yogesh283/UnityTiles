<?php

namespace App\Filament\Resources\Pools\Pages;

use App\Filament\Resources\Pools\PoolResource;
use Filament\Actions\EditAction;
use Filament\Resources\Pages\ViewRecord;

class ViewPool extends ViewRecord
{
    protected static string $resource = PoolResource::class;

    protected function getHeaderActions(): array
    {
        return [
            EditAction::make(),
        ];
    }
}
