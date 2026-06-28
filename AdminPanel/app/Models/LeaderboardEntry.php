<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class LeaderboardEntry extends Model
{
    protected $table = 'leaderboard';

    public $timestamps = false;

    protected $fillable = ['user_id', 'total_wins', 'total_prize', 'tournaments_played', 'best_rank'];
}
