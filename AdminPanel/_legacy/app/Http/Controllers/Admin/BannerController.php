<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Http\Request;
use Illuminate\Routing\Controller;
use Illuminate\Support\Facades\DB;

class BannerController extends Controller
{
    public function index()
    {
        $banners = DB::table('banners')->orderBy('sort_order')->get();
        return view('admin.banners.index', compact('banners'));
    }

    public function store(Request $request)
    {
        $data = $request->validate([
            'title' => 'required|string|max:255',
            'image_url' => 'required|url',
            'link_url' => 'nullable|url',
            'sort_order' => 'integer',
        ]);
        $data['created_at'] = now();
        DB::table('banners')->insert($data);
        return back()->with('success', 'Banner created');
    }
}
