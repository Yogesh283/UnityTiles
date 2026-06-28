<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Routing\Controller;
use Illuminate\Support\Facades\DB;

class TournamentController extends Controller
{
    public function index()
    {
        $tournaments = DB::table('tournaments')->orderBy('entry_fee')->get();
        return view('admin.tournaments.index', compact('tournaments'));
    }

    public function show(string $id)
    {
        $tournament = DB::table('tournaments')->where('id', $id)->first();
        abort_unless($tournament, 404);
        $rooms = DB::table('tournament_rooms')->where('tournament_id', $id)->orderByDesc('created_at')->limit(50)->get();
        return view('admin.tournaments.show', compact('tournament', 'rooms'));
    }
}
