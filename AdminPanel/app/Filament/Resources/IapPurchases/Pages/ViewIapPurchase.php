<?php

namespace App\Filament\Resources\IapPurchases\Pages;

use App\Filament\Resources\IapPurchases\IapPurchaseResource;
use Filament\Actions\EditAction;
use Filament\Resources\Pages\ViewRecord;

class ViewIapPurchase extends ViewRecord
{
    protected static string $resource = IapPurchaseResource::class;

    protected function getHeaderActions(): array
    {
        return [
            EditAction::make(),
        ];
    }
}
