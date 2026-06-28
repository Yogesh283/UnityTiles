-- TilesClash shared database (XAMPP MySQL)
-- Import via phpMyAdmin or: mysql -u root < tilesclash.sql

CREATE DATABASE IF NOT EXISTS `game` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `game`;

CREATE TABLE IF NOT EXISTS `users` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `username` VARCHAR(64) NULL UNIQUE,
  `email` VARCHAR(255) NULL UNIQUE,
  `password_hash` VARCHAR(255) NULL,
  `google_id` VARCHAR(128) NULL UNIQUE,
  `guest_id` VARCHAR(128) NULL UNIQUE,
  `display_name` VARCHAR(128) NOT NULL DEFAULT 'Player',
  `avatar_url` VARCHAR(512) NULL,
  `is_guest` TINYINT(1) NOT NULL DEFAULT 0,
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `wallet` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `user_id` BIGINT UNSIGNED NOT NULL UNIQUE,
  `balance` INT NOT NULL DEFAULT 500,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_wallet_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `wallet_transactions` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `user_id` BIGINT UNSIGNED NOT NULL,
  `amount` INT NOT NULL,
  `balance_after` INT NOT NULL,
  `type` VARCHAR(64) NOT NULL,
  `reference_id` VARCHAR(128) NULL,
  `note` VARCHAR(255) NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_wallet_tx_user` (`user_id`),
  CONSTRAINT `fk_wallet_tx_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `iap_purchases` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `user_id` BIGINT UNSIGNED NOT NULL,
  `platform` VARCHAR(32) NOT NULL DEFAULT 'google_play',
  `product_id` VARCHAR(128) NOT NULL,
  `order_id` VARCHAR(128) NOT NULL UNIQUE,
  `purchase_token` VARCHAR(512) NOT NULL UNIQUE,
  `coins_added` INT NOT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_iap_user` (`user_id`),
  CONSTRAINT `fk_iap_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `levels` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `level_index` INT NOT NULL UNIQUE,
  `name` VARCHAR(128) NULL,
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `level_rewards` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `level_index` INT NOT NULL UNIQUE,
  `coin_reward` INT NOT NULL DEFAULT 50,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `tournaments` (
  `id` VARCHAR(64) NOT NULL,
  `display_name` VARCHAR(128) NOT NULL,
  `icon` VARCHAR(16) NOT NULL DEFAULT '🏆',
  `max_players` INT NOT NULL,
  `entry_fee` INT NOT NULL,
  `prize_pool` INT NOT NULL,
  `platform_fee` INT NOT NULL DEFAULT 0,
  `reward_info` VARCHAR(255) NOT NULL DEFAULT '',
  `waiting_seconds` INT NOT NULL DEFAULT 30,
  `status_label` VARCHAR(64) NOT NULL DEFAULT 'OPEN',
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `tournament_rooms` (
  `id` VARCHAR(64) NOT NULL,
  `tournament_id` VARCHAR(64) NOT NULL,
  `level_index` INT NOT NULL,
  `level_seed` INT NOT NULL,
  `status` VARCHAR(32) NOT NULL DEFAULT 'waiting',
  `max_players` INT NOT NULL,
  `started_at` DATETIME NULL,
  `ended_at` DATETIME NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_room_tournament` (`tournament_id`),
  CONSTRAINT `fk_room_tournament` FOREIGN KEY (`tournament_id`) REFERENCES `tournaments` (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `room_players` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `room_id` VARCHAR(64) NOT NULL,
  `user_id` BIGINT UNSIGNED NOT NULL,
  `score` INT NOT NULL DEFAULT 0,
  `moves` INT NOT NULL DEFAULT 0,
  `elapsed_seconds` INT NOT NULL DEFAULT 0,
  `rank` INT NULL,
  `prize` INT NOT NULL DEFAULT 0,
  `is_connected` TINYINT(1) NOT NULL DEFAULT 1,
  `joined_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `finished_at` DATETIME NULL,
  PRIMARY KEY (`id`),
  KEY `idx_room_players_room` (`room_id`),
  KEY `idx_room_players_user` (`user_id`),
  CONSTRAINT `fk_room_players_room` FOREIGN KEY (`room_id`) REFERENCES `tournament_rooms` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_room_players_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `tournament_results` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `room_id` VARCHAR(64) NOT NULL,
  `tournament_id` VARCHAR(64) NOT NULL,
  `user_id` BIGINT UNSIGNED NOT NULL,
  `rank` INT NOT NULL,
  `score` INT NOT NULL DEFAULT 0,
  `prize` INT NOT NULL DEFAULT 0,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_results_user` (`user_id`),
  KEY `idx_results_room` (`room_id`),
  CONSTRAINT `fk_results_room` FOREIGN KEY (`room_id`) REFERENCES `tournament_rooms` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_results_tournament` FOREIGN KEY (`tournament_id`) REFERENCES `tournaments` (`id`),
  CONSTRAINT `fk_results_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `leaderboard` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `user_id` BIGINT UNSIGNED NOT NULL UNIQUE,
  `total_wins` INT NOT NULL DEFAULT 0,
  `total_prize` INT NOT NULL DEFAULT 0,
  `tournaments_played` INT NOT NULL DEFAULT 0,
  `best_rank` INT NOT NULL DEFAULT 9999,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_leaderboard_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `notifications` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `user_id` BIGINT UNSIGNED NULL,
  `title` VARCHAR(255) NOT NULL,
  `body` TEXT NOT NULL,
  `is_read` TINYINT(1) NOT NULL DEFAULT 0,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_notifications_user` (`user_id`),
  CONSTRAINT `fk_notifications_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `settings` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `key` VARCHAR(128) NOT NULL UNIQUE,
  `value` TEXT NOT NULL,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `banners` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `title` VARCHAR(255) NOT NULL,
  `image_url` VARCHAR(512) NOT NULL,
  `link_url` VARCHAR(512) NULL,
  `sort_order` INT NOT NULL DEFAULT 0,
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `admins` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `email` VARCHAR(255) NOT NULL UNIQUE,
  `password_hash` VARCHAR(255) NOT NULL,
  `name` VARCHAR(128) NOT NULL,
  `role` VARCHAR(64) NOT NULL DEFAULT 'admin',
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `logs` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `level` VARCHAR(16) NOT NULL DEFAULT 'info',
  `source` VARCHAR(64) NOT NULL DEFAULT 'api',
  `message` TEXT NOT NULL,
  `context` TEXT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

-- Seed tournaments (matches Unity TournamentCatalog)
INSERT INTO `tournaments` (`id`, `display_name`, `icon`, `max_players`, `entry_fee`, `prize_pool`, `platform_fee`, `reward_info`, `waiting_seconds`, `status_label`) VALUES
('duel_1v1', '1 vs 1 Duel', '⚔️', 2, 100, 160, 40, '', 12, 'OPEN'),
('quick_cup', 'Quick Cup', '⚡', 10, 100, 800, 0, 'Top 3 Win', 20, 'OPEN'),
('mega_clash', 'Mega Clash', '🔥', 50, 200, 8000, 0, 'Top 10 Win', 30, 'FILLING'),
('grand_clash', 'Grand Clash', '👑', 100, 500, 40000, 0, 'Top 20 Win', 45, 'FILLING'),
('championship', 'Championship', '💎', 500, 1000, 400000, 0, 'Top 100 Win', 60, 'STARTING SOON'),
('world_cup', 'World Cup', '🌍', 1000, 2000, 1600000, 0, 'Top 200 Win', 90, 'FULL')
ON DUPLICATE KEY UPDATE display_name = VALUES(display_name);

-- Default admin (password: admin123) - change in production
INSERT INTO `admins` (`email`, `password_hash`, `name`, `role`) VALUES
('admin@tilesclash.com', '$2y$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Super Admin', 'admin')
ON DUPLICATE KEY UPDATE email = VALUES(email);

INSERT INTO `settings` (`key`, `value`) VALUES
('app_name', 'TilesClash'),
('maintenance_mode', '0'),
('default_coins', '500')
ON DUPLICATE KEY UPDATE value = VALUES(value);
