<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class IapPurchase extends Model
{
    public $timestamps = false;

    protected $fillable = [
        'user_id',
        'platform',
        'product_id',
        'order_id',
        'purchase_token',
        'coins_added',
    ];

    protected function casts(): array
    {
        return [
            'created_at' => 'datetime',
        ];
    }

    public function player(): BelongsTo
    {
        return $this->belongsTo(Player::class, 'user_id');
    }
}
