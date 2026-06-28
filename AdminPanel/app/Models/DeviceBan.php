<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class DeviceBan extends Model
{
    public $timestamps = false;

    protected $fillable = ['device_id', 'reason', 'banned_by', 'is_active'];

    protected function casts(): array
    {
        return ['is_active' => 'boolean', 'created_at' => 'datetime'];
    }
}
