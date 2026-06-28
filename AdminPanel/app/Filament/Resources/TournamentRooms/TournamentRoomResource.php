<?php

namespace App\Filament\Resources\TournamentRooms;

use App\Filament\Resources\TournamentRooms\Pages\CreateTournamentRoom;
use App\Filament\Resources\TournamentRooms\Pages\EditTournamentRoom;
use App\Filament\Resources\TournamentRooms\Pages\ListTournamentRooms;
use App\Filament\Resources\TournamentRooms\Pages\ViewTournamentRoom;
use App\Filament\Resources\TournamentRooms\Schemas\TournamentRoomForm;
use App\Filament\Resources\TournamentRooms\Schemas\TournamentRoomInfolist;
use App\Filament\Resources\TournamentRooms\Tables\TournamentRoomsTable;
use App\Models\TournamentRoom;
use BackedEnum;
use Filament\Resources\Resource;
use Filament\Schemas\Schema;
use Filament\Support\Icons\Heroicon;
use Filament\Tables\Table;

class TournamentRoomResource extends Resource
{
    protected static ?string $model = TournamentRoom::class;

    protected static ?string $navigationLabel = 'Rooms';

    protected static string|\UnitEnum|null $navigationGroup = 'Game';

    protected static string|BackedEnum|null $navigationIcon = Heroicon::OutlinedHomeModern;

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
        return TournamentRoomForm::configure($schema);
    }

    public static function infolist(Schema $schema): Schema
    {
        return TournamentRoomInfolist::configure($schema);
    }

    public static function table(Table $table): Table
    {
        return TournamentRoomsTable::configure($table);
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
            'index' => ListTournamentRooms::route('/'),
            'create' => CreateTournamentRoom::route('/create'),
            'view' => ViewTournamentRoom::route('/{record}'),
            'edit' => EditTournamentRoom::route('/{record}/edit'),
        ];
    }
}
