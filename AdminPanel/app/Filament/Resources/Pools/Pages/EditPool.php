<?php

namespace App\Filament\Resources\Pools\Pages;

use App\Filament\Resources\Pools\PoolResource;
use App\Models\Pool;
use Filament\Actions\DeleteAction;
use Filament\Actions\ViewAction;
use Filament\Resources\Pages\EditRecord;

class EditPool extends EditRecord
{
    protected static string $resource = PoolResource::class;

    protected function mutateFormDataBeforeSave(array $data): array
    {
        return Pool::applyDefaults($data);
    }

    protected function getHeaderActions(): array
    {
        return [
            ViewAction::make(),
            DeleteAction::make(),
        ];
    }
}
