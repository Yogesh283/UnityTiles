<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Routing\Controller;
use Illuminate\Support\Facades\DB;

class LeaderboardController extends Controller
{
    public function index()
    {
        $entries = DB::table('leaderboard')
            ->join('users', 'users.id', '=', 'leaderboard.user_id')
            ->select('leaderboard.*', 'users.display_name')
            ->orderByDesc('total_prize')
            ->paginate(50);

        return view('admin.leaderboard.index', compact('entries'));
    }
}
