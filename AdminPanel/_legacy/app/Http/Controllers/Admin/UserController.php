<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Http\Request;
use Illuminate\Routing\Controller;
use Illuminate\Support\Facades\DB;

class UserController extends Controller
{
    public function index()
    {
        $users = DB::table('users')->orderByDesc('created_at')->paginate(25);
        return view('admin.users.index', compact('users'));
    }

    public function show(int $id)
    {
        $user = DB::table('users')->where('id', $id)->first();
        abort_unless($user, 404);
        $wallet = DB::table('wallet')->where('user_id', $id)->first();
        return view('admin.users.show', compact('user', 'wallet'));
    }
}
