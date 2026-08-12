<?php

namespace App\Filament\Resources\WithdrawalRequests\Tables;

use App\Models\Wallet;
use App\Models\WalletTransaction;
use Filament\Actions\Action;
use Filament\Actions\ViewAction;
use Filament\Forms\Components\TextInput;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Filters\SelectFilter;
use Filament\Tables\Table;
use Illuminate\Support\Facades\DB;

class WithdrawalRequestsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->defaultSort('created_at', 'desc')
            ->columns([
                TextColumn::make('request_code')->label('Code')->copyable()->searchable(),
                TextColumn::make('player.display_name')->label('Player')->searchable(),
                TextColumn::make('player.email')->label('Email')->searchable()->toggleable(),
                TextColumn::make('coins')->numeric()->sortable(),
                TextColumn::make('usdt_amount')->label('USDT')->numeric(decimalPlaces: 4)->sortable(),
                TextColumn::make('bep20_address')->label('BEP20')->copyable()->limit(18)->searchable(),
                TextColumn::make('status')
                    ->badge()
                    ->color(fn (string $state): string => match ($state) {
                        'pending' => 'warning',
                        'approved' => 'success',
                        'rejected' => 'danger',
                        default => 'gray',
                    }),
                TextColumn::make('payout_tx')->label('TX')->copyable()->limit(16)->toggleable(),
                TextColumn::make('created_at')->dateTime()->sortable(),
            ])
            ->filters([
                SelectFilter::make('status')
                    ->options([
                        'pending' => 'Pending',
                        'approved' => 'Approved',
                        'rejected' => 'Rejected',
                    ])
                    ->default('pending'),
            ])
            ->recordActions([
                ViewAction::make(),
                Action::make('approve')
                    ->label('Approve')
                    ->icon('heroicon-o-check-circle')
                    ->color('success')
                    ->visible(fn ($record) => $record->status === 'pending')
                    ->form([
                        TextInput::make('payout_tx')
                            ->label('BSC TX hash (optional)')
                            ->maxLength(128),
                        TextInput::make('admin_note')->maxLength(255),
                    ])
                    ->requiresConfirmation()
                    ->modalHeading('Approve USDT BEP20 payout?')
                    ->modalDescription('Send USDT on BNB Smart Chain first, then approve. Coins are already deducted.')
                    ->action(function ($record, array $data): void {
                        if ($record->status !== 'pending') {
                            throw new \RuntimeException('Already processed.');
                        }
                        $record->update([
                            'status' => 'approved',
                            'payout_tx' => $data['payout_tx'] ?: null,
                            'admin_note' => $data['admin_note'] ?: null,
                            'reviewed_by' => auth()->user()?->email ?? (string) auth()->id(),
                            'reviewed_at' => now(),
                        ]);
                    }),
                Action::make('reject')
                    ->label('Reject')
                    ->icon('heroicon-o-x-circle')
                    ->color('danger')
                    ->visible(fn ($record) => $record->status === 'pending')
                    ->form([
                        TextInput::make('admin_note')
                            ->label('Reason')
                            ->required()
                            ->maxLength(255),
                    ])
                    ->requiresConfirmation()
                    ->modalHeading('Reject and refund coins?')
                    ->action(function ($record, array $data): void {
                        DB::transaction(function () use ($record, $data): void {
                            $fresh = $record->newQuery()->whereKey($record->getKey())->lockForUpdate()->first();
                            if (! $fresh || $fresh->status !== 'pending') {
                                throw new \RuntimeException('Already processed.');
                            }

                            $wallet = Wallet::query()
                                ->where('user_id', $fresh->user_id)
                                ->lockForUpdate()
                                ->first();
                            if (! $wallet) {
                                throw new \RuntimeException('Wallet not found.');
                            }

                            $refund = (int) $fresh->coins;
                            $newBalance = $wallet->balance + $refund;
                            $wallet->update(['balance' => $newBalance, 'updated_at' => now()]);

                            WalletTransaction::query()->create([
                                'user_id' => $fresh->user_id,
                                'amount' => $refund,
                                'balance_after' => $newBalance,
                                'type' => 'withdraw_usdt_refund',
                                'reference_id' => $fresh->request_code,
                                'note' => 'Withdraw rejected: '.($data['admin_note'] ?? ''),
                                'created_at' => now(),
                            ]);

                            $fresh->update([
                                'status' => 'rejected',
                                'admin_note' => $data['admin_note'] ?? null,
                                'reviewed_by' => auth()->user()?->email ?? (string) auth()->id(),
                                'reviewed_at' => now(),
                            ]);
                        });
                    }),
            ]);
    }
}
