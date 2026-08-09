-- MySQL 5.7+ compatible: IQFX Pro Tournament "Pool Play".
-- A pool is a time-boxed leaderboard tournament: players pay a fixed entry fee to join,
-- each plays the same level once, and when the pool fills (or is closed by an admin) the
-- top-N scores share the prize pool (= 70% of total collection) by a distribution table.

-- pools ----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `pools` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(160) NOT NULL,
  `icon` VARCHAR(16) NOT NULL DEFAULT '🏆',
  `entry_fee` INT NOT NULL,
  `max_players` INT NOT NULL,
  `winners_count` INT NOT NULL DEFAULT 1,
  `prize_share` DECIMAL(5,4) NOT NULL DEFAULT 0.7000,
  `prize_pool` INT NOT NULL DEFAULT 0,           -- advertised (max_players * entry * prize_share)
  `distribution` VARCHAR(255) NOT NULL DEFAULT '[100]',  -- JSON array of winner percentages
  `level_index` INT NOT NULL DEFAULT 0,
  `level_seed` INT NOT NULL DEFAULT 0,
  `status` VARCHAR(16) NOT NULL DEFAULT 'open',  -- open | running | finished | cancelled
  `starts_at` DATETIME NULL,
  `ends_at` DATETIME NULL,
  `finished_at` DATETIME NULL,
  `created_by` VARCHAR(64) NULL,                 -- admin id / 'system'
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_pools_status` (`status`, `entry_fee`),
  KEY `idx_pools_created` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- pool_entries ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `pool_entries` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `pool_id` BIGINT NOT NULL,
  `user_id` BIGINT NOT NULL,
  `entry_fee` INT NOT NULL,
  `score` INT NOT NULL DEFAULT 0,
  `moves` INT NOT NULL DEFAULT 0,
  `elapsed_seconds` INT NOT NULL DEFAULT 0,
  `rank` INT NULL,
  `prize` INT NOT NULL DEFAULT 0,
  `status` VARCHAR(16) NOT NULL DEFAULT 'joined', -- joined | played | won | lost | refunded
  `joined_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `submitted_at` DATETIME NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_pool_entry` (`pool_id`, `user_id`),
  KEY `idx_pool_entries_pool` (`pool_id`, `score`),
  KEY `idx_pool_entries_user` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Seed a few open preset pools so the app always has something to join --------
INSERT INTO `pools` (`name`, `icon`, `entry_fee`, `max_players`, `winners_count`, `prize_pool`, `distribution`, `status`, `created_by`)
SELECT * FROM (
  SELECT 'IQFX Pro · ₹10 · 10 Players'  AS name, '🎯' AS icon, 10  AS entry_fee, 10  AS max_players, 1 AS winners_count, 70   AS prize_pool, '[100]'        AS distribution, 'open' AS status, 'system' AS created_by
  UNION ALL SELECT 'IQFX Pro · ₹50 · 100 Players', '🏆', 50, 100, 2, 3500, '[70,30]', 'open', 'system'
  UNION ALL SELECT 'IQFX Pro · ₹100 · 500 Players', '👑', 100, 500, 3, 35000, '[60,25,15]', 'open', 'system'
) AS seed
WHERE NOT EXISTS (SELECT 1 FROM `pools` WHERE `created_by` = 'system');
