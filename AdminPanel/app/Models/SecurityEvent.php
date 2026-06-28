<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class SecurityEvent extends Model
{
    public $timestamps = false;

    protected $fillable = ['user_id', 'event_type', 'severity', 'message', 'context', 'ip_address', 'device_id'];
}
