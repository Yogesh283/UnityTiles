<?php

namespace App\Filament\Resources\Pools;

use App\Filament\Resources\Pools\Pages\CreatePool;
use App\Filament\Resources\Pools\Pages\EditPool;
use App\Filament\Resources\Pools\Pages\ListPools;
use App\Filament\Resources\Pools\Pages\ViewPool;
use App\Filament\Resources\Pools\Schemas\PoolForm;
use App\Filament\Resources\Pools\Tables\PoolsTable;
use App\Models\Pool;
use BackedEnum;
use Filament\Resources\Resource;
use Filament\Schemas\Schema;
use Filament\Support\Icons\Heroicon;
use Filament\Tables\Table;

class PoolResource extends Resource
{
    protected static ?string $model = Pool::class;

    protected static ?string $navigationLabel = 'Pool Play';

    protected static string|\UnitEnum|null $navigationGroup = 'Game';

    protected static string|BackedEnum|null $navigationIcon = Heroicon::OutlinedUserGroup;

    protected static ?int $navigationSort = 3;

    public static function form(Schema $schema): Schema
    {
        return PoolForm::configure($schema);
    }

    public static function table(Table $table): Table
    {
        return PoolsTable::configure($table);
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
            'index' => ListPools::route('/'),
            'create' => CreatePool::route('/create'),
            'view' => ViewPool::route('/{record}'),
            'edit' => EditPool::route('/{record}/edit'),
        ];
    }
}
