<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Relations\HasOne;

class Player extends Model
{
    protected $table = 'users';

    protected $fillable = [
        'username',
        'email',
        'display_name',
        'avatar_url',
        'is_guest',
        'is_active',
    ];

    protected function casts(): array
    {
        return [
            'is_guest' => 'boolean',
            'is_active' => 'boolean',
            'created_at' => 'datetime',
            'updated_at' => 'datetime',
        ];
    }

    public function wallet(): HasOne
    {
        return $this->hasOne(Wallet::class, 'user_id');
    }

    public function walletTransactions(): HasMany
    {
        return $this->hasMany(WalletTransaction::class, 'user_id');
    }

    public function iapPurchases(): HasMany
    {
        return $this->hasMany(IapPurchase::class, 'user_id');
    }
}
