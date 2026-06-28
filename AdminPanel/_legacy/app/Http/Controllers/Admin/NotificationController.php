<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Http\Request;
use Illuminate\Routing\Controller;
use Illuminate\Support\Facades\DB;

class NotificationController extends Controller
{
    public function index()
    {
        $notifications = DB::table('notifications')->orderByDesc('created_at')->paginate(25);
        return view('admin.notifications.index', compact('notifications'));
    }

    public function store(Request $request)
    {
        $data = $request->validate([
            'title' => 'required|string|max:255',
            'body' => 'required|string',
            'user_id' => 'nullable|integer',
        ]);
        $data['created_at'] = now();
        DB::table('notifications')->insert($data);
        return back()->with('success', 'Notification sent');
    }
}
