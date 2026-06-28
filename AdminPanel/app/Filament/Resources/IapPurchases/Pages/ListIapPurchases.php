<?php

namespace App\Filament\Resources\IapPurchases\Pages;

use App\Filament\Resources\IapPurchases\IapPurchaseResource;
use Filament\Actions\CreateAction;
use Filament\Resources\Pages\ListRecords;

class ListIapPurchases extends ListRecords
{
    protected static string $resource = IapPurchaseResource::class;

    protected function getHeaderActions(): array
    {
        return [
            CreateAction::make(),
        ];
    }
}
