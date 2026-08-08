-- MySQL 5.7+ compatible: WXO 6-level referral reward program.
-- Adds referral linkage on users and a per-level earning ledger.

SET @db := DATABASE();

-- users.referral_code -------------------------------------------------------
SET @col := (
  SELECT COUNT(1) FROM information_schema.COLUMNS
  WHERE table_schema = @db AND table_name = 'users' AND column_name = 'referral_code'
);
SET @sql := IF(
  @col = 0,
  'ALTER TABLE `users` ADD COLUMN `referral_code` VARCHAR(16) NULL',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- users.referred_by (upline user id) ---------------------------------------
SET @col := (
  SELECT COUNT(1) FROM information_schema.COLUMNS
  WHERE table_schema = @db AND table_name = 'users' AND column_name = 'referred_by'
);
SET @sql := IF(
  @col = 0,
  'ALTER TABLE `users` ADD COLUMN `referred_by` BIGINT NULL',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_index := (
  SELECT COUNT(1) FROM information_schema.statistics
  WHERE table_schema = @db AND table_name = 'users' AND index_name = 'uq_users_referral_code'
);
SET @sql := IF(
  @has_index = 0,
  'CREATE UNIQUE INDEX `uq_users_referral_code` ON `users` (`referral_code`)',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_index := (
  SELECT COUNT(1) FROM information_schema.statistics
  WHERE table_schema = @db AND table_name = 'users' AND index_name = 'idx_users_referred_by'
);
SET @sql := IF(
  @has_index = 0,
  'CREATE INDEX `idx_users_referred_by` ON `users` (`referred_by`)',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- referral_earnings ledger --------------------------------------------------
CREATE TABLE IF NOT EXISTS `referral_earnings` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `user_id` BIGINT NOT NULL,
  `from_user_id` BIGINT NOT NULL,
  `level` TINYINT NOT NULL,
  `percent` DECIMAL(5,2) NOT NULL,
  `entry_fee` INT NOT NULL,
  `points` INT NOT NULL,
  `room_id` VARCHAR(128) NULL,
  `tournament_id` VARCHAR(64) NULL,
  `status` VARCHAR(32) NOT NULL DEFAULT 'credited',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_referral_earning` (`user_id`, `from_user_id`, `room_id`, `level`),
  KEY `idx_referral_earnings_user` (`user_id`, `created_at`),
  KEY `idx_referral_earnings_from` (`from_user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Backfill referral codes for existing users --------------------------------
UPDATE `users`
SET `referral_code` = UPPER(CONCAT('WXO', LPAD(HEX(`id`), 6, '0')))
WHERE `referral_code` IS NULL OR `referral_code` = '';
