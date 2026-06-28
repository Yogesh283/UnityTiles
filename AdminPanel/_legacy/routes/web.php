<?php

use App\Http\Controllers\Admin\BannerController;
use App\Http\Controllers\Admin\DashboardController;
use App\Http\Controllers\Admin\LeaderboardController;
use App\Http\Controllers\Admin\LogController;
use App\Http\Controllers\Admin\NotificationController;
use App\Http\Controllers\Admin\ReportController;
use App\Http\Controllers\Admin\RoomController;
use App\Http\Controllers\Admin\SettingController;
use App\Http\Controllers\Admin\SupportController;
use App\Http\Controllers\Admin\TournamentController;
use App\Http\Controllers\Admin\UserController;
use App\Http\Controllers\Admin\WalletController;
use Illuminate\Support\Facades\Route;

Route::get('/', [DashboardController::class, 'index'])->name('dashboard');

Route::prefix('users')->group(function () {
    Route::get('/', [UserController::class, 'index'])->name('users.index');
    Route::get('/{id}', [UserController::class, 'show'])->name('users.show');
});

Route::prefix('wallet')->group(function () {
    Route::get('/', [WalletController::class, 'index'])->name('wallet.index');
    Route::post('/adjust', [WalletController::class, 'adjust'])->name('wallet.adjust');
});

Route::prefix('tournaments')->group(function () {
    Route::get('/', [TournamentController::class, 'index'])->name('tournaments.index');
    Route::get('/{id}', [TournamentController::class, 'show'])->name('tournaments.show');
});

Route::prefix('rooms')->group(function () {
    Route::get('/', [RoomController::class, 'index'])->name('rooms.index');
    Route::get('/{id}', [RoomController::class, 'show'])->name('rooms.show');
});

Route::get('/leaderboard', [LeaderboardController::class, 'index'])->name('leaderboard.index');
Route::get('/reports', [ReportController::class, 'index'])->name('reports.index');
Route::get('/analytics', [ReportController::class, 'analytics'])->name('analytics.index');

Route::prefix('settings')->group(function () {
    Route::get('/', [SettingController::class, 'index'])->name('settings.index');
    Route::post('/', [SettingController::class, 'update'])->name('settings.update');
});

Route::prefix('banners')->group(function () {
    Route::get('/', [BannerController::class, 'index'])->name('banners.index');
    Route::post('/', [BannerController::class, 'store'])->name('banners.store');
});

Route::prefix('notifications')->group(function () {
    Route::get('/', [NotificationController::class, 'index'])->name('notifications.index');
    Route::post('/', [NotificationController::class, 'store'])->name('notifications.store');
});

Route::get('/support', [SupportController::class, 'index'])->name('support.index');
Route::get('/logs', [LogController::class, 'index'])->name('logs.index');
