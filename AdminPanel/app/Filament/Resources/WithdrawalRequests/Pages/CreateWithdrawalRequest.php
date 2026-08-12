<?php

namespace App\Filament\Resources\WithdrawalRequests\Pages;

use App\Filament\Resources\WithdrawalRequests\WithdrawalRequestResource;
use Filament\Resources\Pages\CreateRecord;

class CreateWithdrawalRequest extends CreateRecord
{
    protected static string $resource = WithdrawalRequestResource::class;
}
