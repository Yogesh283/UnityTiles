<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Routing\Controller;

class SupportController extends Controller
{
    public function index()
    {
        return view('admin.support.index');
    }
}
