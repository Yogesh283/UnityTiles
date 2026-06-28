<?php

namespace App\Filament\Resources\Wallets\Tables;

use App\Models\WalletTransaction;
use Filament\Actions\Action;
use Filament\Actions\ViewAction;
use Filament\Forms\Components\TextInput;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;
use Illuminate\Support\Facades\DB;

class WalletsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->defaultSort('balance', 'desc')
            ->columns([
                TextColumn::make('user_id')->label('Player ID')->sortable(),
                TextColumn::make('player.display_name')->label('Player')->searchable(),
                TextColumn::make('player.email')->label('Email')->searchable(),
                TextColumn::make('balance')->numeric()->sortable(),
                TextColumn::make('updated_at')->dateTime()->sortable(),
            ])
            ->recordActions([
                ViewAction::make(),
                Action::make('adjust')
                    ->label('Adjust')
                    ->icon('heroicon-o-banknotes')
                    ->form([
                        TextInput::make('amount')
                            ->numeric()
                            ->required()
                            ->helperText('Negative amount deducts coins.'),
                        TextInput::make('note')->maxLength(255),
                    ])
                    ->action(function ($record, array $data): void {
                        DB::transaction(function () use ($record, $data): void {
                            $amount = (int) $data['amount'];
                            $newBalance = $record->balance + $amount;

                            if ($newBalance < 0) {
                                throw new \RuntimeException('Insufficient balance.');
                            }

                            $record->update(['balance' => $newBalance, 'updated_at' => now()]);

                            WalletTransaction::query()->create([
                                'user_id' => $record->user_id,
                                'amount' => $amount,
                                'balance_after' => $newBalance,
                                'type' => 'admin_adjust',
                                'note' => $data['note'] ?? 'Admin adjustment',
                                'created_at' => now(),
                            ]);
                        });
                    }),
            ]);
    }
}
