<?php

namespace App\Http\Controllers\Admin;

use Illuminate\Http\Request;
use Illuminate\Routing\Controller;
use Illuminate\Support\Facades\DB;

class WalletController extends Controller
{
    public function index()
    {
        $wallets = DB::table('wallet')
            ->join('users', 'users.id', '=', 'wallet.user_id')
            ->select('wallet.*', 'users.display_name', 'users.email')
            ->orderByDesc('wallet.balance')
            ->paginate(25);

        return view('admin.wallet.index', compact('wallets'));
    }

    public function adjust(Request $request)
    {
        $data = $request->validate([
            'user_id' => 'required|integer',
            'amount' => 'required|integer',
            'note' => 'nullable|string|max:255',
        ]);

        DB::transaction(function () use ($data) {
            $wallet = DB::table('wallet')->where('user_id', $data['user_id'])->lockForUpdate()->first();
            abort_unless($wallet, 404);

            $newBalance = $wallet->balance + $data['amount'];
            abort_if($newBalance < 0, 422, 'Insufficient balance');

            DB::table('wallet')->where('user_id', $data['user_id'])->update(['balance' => $newBalance]);
            DB::table('wallet_transactions')->insert([
                'user_id' => $data['user_id'],
                'amount' => $data['amount'],
                'balance_after' => $newBalance,
                'type' => 'admin_adjust',
                'note' => $data['note'] ?? 'Admin adjustment',
                'created_at' => now(),
            ]);
        });

        return back()->with('success', 'Wallet updated');
    }
}
