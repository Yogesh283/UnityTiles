<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Routing\Controller;
use Illuminate\Support\Facades\DB;

class LogController extends Controller
{
    public function index()
    {
        $logs = DB::table('logs')->orderByDesc('created_at')->paginate(50);
        return view('admin.logs.index', compact('logs'));
    }
}
