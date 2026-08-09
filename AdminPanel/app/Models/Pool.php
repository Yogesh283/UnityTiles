<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;

class Pool extends Model
{
    protected $table = 'pools';

    protected $fillable = [
        'name',
        'icon',
        'entry_fee',
        'max_players',
        'winners_count',
        'prize_share',
        'prize_pool',
        'distribution',
        'level_index',
        'level_seed',
        'status',
        'starts_at',
        'ends_at',
        'finished_at',
        'created_by',
    ];

    protected function casts(): array
    {
        return [
            'entry_fee' => 'integer',
            'max_players' => 'integer',
            'winners_count' => 'integer',
            'prize_pool' => 'integer',
            'prize_share' => 'decimal:4',
            'starts_at' => 'datetime',
            'ends_at' => 'datetime',
            'finished_at' => 'datetime',
        ];
    }

    public function entries(): HasMany
    {
        return $this->hasMany(PoolEntry::class, 'pool_id');
    }

    /** Winners paid for a pool of the given size (mirrors backend pool/rules.py). */
    public static function winnersFor(int $players): int
    {
        if ($players <= 50) {
            return 1;
        }
        if ($players <= 100) {
            return 2;
        }
        return 3;
    }

    /** Default winner share percentages. */
    public static function distributionPercents(int $winners): array
    {
        return match (true) {
            $winners <= 1 => [100],
            $winners === 2 => [70, 30],
            $winners === 3 => [60, 25, 15],
            default => [100],
        };
    }

    /** Fill computed fields (prize pool, winners, distribution, seed) before saving. */
    public static function applyDefaults(array $data): array
    {
        $entry = (int) ($data['entry_fee'] ?? 0);
        $players = (int) ($data['max_players'] ?? 0);
        $share = (float) ($data['prize_share'] ?? 0.70);

        $winners = (int) ($data['winners_count'] ?? 0);
        if ($winners < 1) {
            $winners = self::winnersFor($players);
        }
        $winners = max(1, min($winners, max(1, $players)));
        $data['winners_count'] = $winners;

        if (empty($data['distribution'])) {
            $data['distribution'] = json_encode(self::distributionPercents($winners));
        }

        $data['prize_pool'] = (int) floor($players * $entry * $share);

        if (empty($data['level_seed'])) {
            $data['level_seed'] = random_int(1, 2000000000);
        }

        return $data;
    }
}
