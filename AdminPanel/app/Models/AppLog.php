<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class AppLog extends Model
{
    protected $table = 'logs';

    public $timestamps = false;

    protected $fillable = [
        'level',
        'source',
        'message',
        'context',
    ];

    protected function casts(): array
    {
        return [
            'created_at' => 'datetime',
        ];
    }
}
