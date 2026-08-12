<?php

namespace App\Filament\Resources\WithdrawalRequests;

use App\Filament\Resources\WithdrawalRequests\Pages\CreateWithdrawalRequest;
use App\Filament\Resources\WithdrawalRequests\Pages\EditWithdrawalRequest;
use App\Filament\Resources\WithdrawalRequests\Pages\ListWithdrawalRequests;
use App\Filament\Resources\WithdrawalRequests\Pages\ViewWithdrawalRequest;
use App\Filament\Resources\WithdrawalRequests\Schemas\WithdrawalRequestForm;
use App\Filament\Resources\WithdrawalRequests\Schemas\WithdrawalRequestInfolist;
use App\Filament\Resources\WithdrawalRequests\Tables\WithdrawalRequestsTable;
use App\Models\WithdrawalRequest;
use BackedEnum;
use Filament\Resources\Resource;
use Filament\Schemas\Schema;
use Filament\Support\Icons\Heroicon;
use Filament\Tables\Table;

class WithdrawalRequestResource extends Resource
{
    protected static ?string $model = WithdrawalRequest::class;

    protected static ?string $navigationLabel = 'Withdrawals';

    protected static string|\UnitEnum|null $navigationGroup = 'Finance';

    protected static string|BackedEnum|null $navigationIcon = Heroicon::OutlinedArrowUpTray;

    protected static ?int $navigationSort = 4;

    public static function canCreate(): bool
    {
        return false;
    }

    public static function canEdit($record): bool
    {
        return false;
    }

    public static function getNavigationBadge(): ?string
    {
        $count = static::getModel()::query()->where('status', 'pending')->count();

        return $count ? (string) $count : null;
    }

    public static function getNavigationBadgeColor(): ?string
    {
        return 'warning';
    }

    public static function form(Schema $schema): Schema
    {
        return WithdrawalRequestForm::configure($schema);
    }

    public static function infolist(Schema $schema): Schema
    {
        return WithdrawalRequestInfolist::configure($schema);
    }

    public static function table(Table $table): Table
    {
        return WithdrawalRequestsTable::configure($table);
    }

    public static function getRelations(): array
    {
        return [
            //
        ];
    }

    public static function getPages(): array
    {
        return [
            'index' => ListWithdrawalRequests::route('/'),
            'create' => CreateWithdrawalRequest::route('/create'),
            'view' => ViewWithdrawalRequest::route('/{record}'),
            'edit' => EditWithdrawalRequest::route('/{record}/edit'),
        ];
    }
}
