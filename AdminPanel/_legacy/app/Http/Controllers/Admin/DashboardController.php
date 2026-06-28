<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Routing\Controller;
use Illuminate\Support\Facades\DB;

class DashboardController extends Controller
{
    public function index()
    {
        $stats = [
            'users' => DB::table('users')->count(),
            'active_rooms' => DB::table('tournament_rooms')->whereIn('status', ['waiting', 'starting', 'active'])->count(),
            'tournaments_today' => DB::table('tournament_results')->whereDate('created_at', today())->count(),
            'total_coins' => DB::table('wallet')->sum('balance'),
        ];

        return view('admin.dashboard', compact('stats'));
    }
}
