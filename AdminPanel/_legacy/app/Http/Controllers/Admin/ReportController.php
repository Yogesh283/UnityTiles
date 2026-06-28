<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Routing\Controller;

class ReportController extends Controller
{
    public function index()
    {
        return view('admin.reports.index');
    }

    public function analytics()
    {
        return view('admin.analytics.index');
    }
}
