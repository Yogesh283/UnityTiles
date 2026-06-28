<?php

namespace App\Filament\Resources\IapPurchases;

use App\Filament\Resources\IapPurchases\Pages\CreateIapPurchase;
use App\Filament\Resources\IapPurchases\Pages\EditIapPurchase;
use App\Filament\Resources\IapPurchases\Pages\ListIapPurchases;
use App\Filament\Resources\IapPurchases\Pages\ViewIapPurchase;
use App\Filament\Resources\IapPurchases\Schemas\IapPurchaseForm;
use App\Filament\Resources\IapPurchases\Schemas\IapPurchaseInfolist;
use App\Filament\Resources\IapPurchases\Tables\IapPurchasesTable;
use App\Models\IapPurchase;
use BackedEnum;
use Filament\Resources\Resource;
use Filament\Schemas\Schema;
use Filament\Support\Icons\Heroicon;
use Filament\Tables\Table;

class IapPurchaseResource extends Resource
{
    protected static ?string $model = IapPurchase::class;

    protected static ?string $navigationLabel = 'Play Store Purchases';

    protected static string|\UnitEnum|null $navigationGroup = 'Finance';

    protected static string|BackedEnum|null $navigationIcon = Heroicon::OutlinedCurrencyRupee;

    protected static ?int $navigationSort = 3;

    public static function canCreate(): bool
    {
        return false;
    }

    public static function canEdit($record): bool
    {
        return false;
    }

    public static function form(Schema $schema): Schema
    {
        return IapPurchaseForm::configure($schema);
    }

    public static function infolist(Schema $schema): Schema
    {
        return IapPurchaseInfolist::configure($schema);
    }

    public static function table(Table $table): Table
    {
        return IapPurchasesTable::configure($table);
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
            'index' => ListIapPurchases::route('/'),
            'create' => CreateIapPurchase::route('/create'),
            'view' => ViewIapPurchase::route('/{record}'),
            'edit' => EditIapPurchase::route('/{record}/edit'),
        ];
    }
}
