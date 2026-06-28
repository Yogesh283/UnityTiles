<?php

namespace App\Filament\Resources\IapPurchases\Pages;

use App\Filament\Resources\IapPurchases\IapPurchaseResource;
use Filament\Actions\DeleteAction;
use Filament\Actions\ViewAction;
use Filament\Resources\Pages\EditRecord;

class EditIapPurchase extends EditRecord
{
    protected static string $resource = IapPurchaseResource::class;

    protected function getHeaderActions(): array
    {
        return [
            ViewAction::make(),
            DeleteAction::make(),
        ];
    }
}
