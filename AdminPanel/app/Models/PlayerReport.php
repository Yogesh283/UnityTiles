<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class PlayerReport extends Model
{
    public $timestamps = false;

    protected $fillable = ['reporter_user_id', 'reported_user_id', 'reason', 'details', 'status'];
}
