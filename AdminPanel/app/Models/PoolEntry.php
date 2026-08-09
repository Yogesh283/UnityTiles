<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class PoolEntry extends Model
{
    protected $table = 'pool_entries';

    public $timestamps = false;

    protected $fillable = [
        'pool_id',
        'user_id',
        'entry_fee',
        'score',
        'moves',
        'elapsed_seconds',
        'rank',
        'prize',
        'status',
        'joined_at',
        'submitted_at',
    ];

    protected function casts(): array
    {
        return [
            'joined_at' => 'datetime',
            'submitted_at' => 'datetime',
        ];
    }

    public function pool(): BelongsTo
    {
        return $this->belongsTo(Pool::class, 'pool_id');
    }

    public function user(): BelongsTo
    {
        return $this->belongsTo(Player::class, 'user_id');
    }
}
