<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;

class Tournament extends Model
{
    public $incrementing = false;

    protected $keyType = 'string';

    public $timestamps = false;

    protected $fillable = [
        'id',
        'display_name',
        'icon',
        'max_players',
        'entry_fee',
        'prize_pool',
        'platform_fee',
        'reward_info',
        'waiting_seconds',
        'status_label',
        'is_active',
    ];

    protected function casts(): array
    {
        return [
            'is_active' => 'boolean',
        ];
    }

    public function rooms(): HasMany
    {
        return $this->hasMany(TournamentRoom::class, 'tournament_id');
    }
}
