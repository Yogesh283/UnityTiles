<?php

namespace App\Filament\Resources\Pools\Pages;

use App\Filament\Resources\Pools\PoolResource;
use App\Models\Pool;
use Filament\Resources\Pages\CreateRecord;

class CreatePool extends CreateRecord
{
    protected static string $resource = PoolResource::class;

    protected function mutateFormDataBeforeCreate(array $data): array
    {
        return Pool::applyDefaults($data);
    }
}
