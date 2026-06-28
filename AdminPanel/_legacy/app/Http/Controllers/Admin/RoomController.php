<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Routing\Controller;
use Illuminate\Support\Facades\DB;

class RoomController extends Controller
{
    public function index()
    {
        $rooms = DB::table('tournament_rooms')->orderByDesc('created_at')->paginate(25);
        return view('admin.rooms.index', compact('rooms'));
    }

    public function show(string $id)
    {
        $room = DB::table('tournament_rooms')->where('id', $id)->first();
        abort_unless($room, 404);
        $players = DB::table('room_players')->where('room_id', $id)->get();
        return view('admin.rooms.show', compact('room', 'players'));
    }
}
