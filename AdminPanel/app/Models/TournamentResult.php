<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class TournamentResult extends Model
{
    public $timestamps = false;

    protected $fillable = ['room_id', 'tournament_id', 'user_id', 'rank', 'score', 'prize'];

    protected function casts(): array
    {
        return ['created_at' => 'datetime'];
    }
}
