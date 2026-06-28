@extends('layouts.admin')

@section('content')
<h1>Dashboard</h1>
<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:1rem;">
    <div class="card"><strong>Users</strong><div>{{ $stats['users'] }}</div></div>
    <div class="card"><strong>Active Rooms</strong><div>{{ $stats['active_rooms'] }}</div></div>
    <div class="card"><strong>Matches Today</strong><div>{{ $stats['tournaments_today'] }}</div></div>
    <div class="card"><strong>Total Coins</strong><div>{{ $stats['total_coins'] }}</div></div>
</div>
@endsection
