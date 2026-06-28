<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class IpBan extends Model
{
    public $timestamps = false;

    protected $fillable = ['ip_address', 'reason', 'banned_by', 'is_active'];

    protected function casts(): array
    {
        return ['is_active' => 'boolean', 'created_at' => 'datetime'];
    }
}
