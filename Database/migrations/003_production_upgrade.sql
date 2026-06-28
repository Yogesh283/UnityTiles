-- Match IQ production upgrade (run once)
USE `game`;

ALTER TABLE `users`
  ADD COLUMN `user_uuid` CHAR(36) NULL UNIQUE AFTER `id`,
  ADD COLUMN `last_ip` VARCHAR(45) NULL AFTER `is_active`,
  ADD COLUMN `last_device_id` VARCHAR(128) NULL AFTER `last_ip`,
  ADD COLUMN `is_banned` TINYINT(1) NOT NULL DEFAULT 0 AFTER `last_device_id`,
  ADD COLUMN `ban_reason` VARCHAR(255) NULL AFTER `is_banned`;

UPDATE `users` SET `user_uuid` = UUID() WHERE `user_uuid` IS NULL;

ALTER TABLE `wallet_transactions`
  ADD COLUMN `transaction_id` CHAR(36) NULL UNIQUE AFTER `id`,
  ADD COLUMN `balance_before` INT NULL AFTER `amount`,
  ADD COLUMN `reason` VARCHAR(255) NULL AFTER `note`;

UPDATE `wallet_transactions`
SET `transaction_id` = UUID(),
    `balance_before` = GREATEST(0, `balance_after` - `amount`),
    `reason` = COALESCE(`note`, `type`)
WHERE `transaction_id` IS NULL;

ALTER TABLE `room_players`
  ADD COLUMN `submitted_at` DATETIME NULL AFTER `finished_at`;

CREATE TABLE IF NOT EXISTS `device_bans` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `device_id` VARCHAR(128) NOT NULL UNIQUE,
  `reason` VARCHAR(255) NOT NULL,
  `banned_by` VARCHAR(128) NULL,
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `ip_bans` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `ip_address` VARCHAR(45) NOT NULL UNIQUE,
  `reason` VARCHAR(255) NOT NULL,
  `banned_by` VARCHAR(128) NULL,
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `security_events` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `user_id` BIGINT UNSIGNED NULL,
  `event_type` VARCHAR(64) NOT NULL,
  `severity` VARCHAR(16) NOT NULL DEFAULT 'warning',
  `message` TEXT NOT NULL,
  `context` TEXT NULL,
  `ip_address` VARCHAR(45) NULL,
  `device_id` VARCHAR(128) NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_security_user` (`user_id`),
  KEY `idx_security_type` (`event_type`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `audit_logs` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `actor_type` VARCHAR(32) NOT NULL DEFAULT 'system',
  `actor_id` VARCHAR(128) NULL,
  `action` VARCHAR(64) NOT NULL,
  `target_type` VARCHAR(64) NULL,
  `target_id` VARCHAR(128) NULL,
  `message` TEXT NOT NULL,
  `context` TEXT NULL,
  `ip_address` VARCHAR(45) NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_audit_action` (`action`),
  KEY `idx_audit_created` (`created_at`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `player_reports` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `reporter_user_id` BIGINT UNSIGNED NULL,
  `reported_user_id` BIGINT UNSIGNED NOT NULL,
  `reason` VARCHAR(255) NOT NULL,
  `details` TEXT NULL,
  `status` VARCHAR(32) NOT NULL DEFAULT 'open',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_reports_reported` (`reported_user_id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `fcm_tokens` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `user_id` BIGINT UNSIGNED NOT NULL,
  `token` VARCHAR(512) NOT NULL,
  `platform` VARCHAR(32) NOT NULL DEFAULT 'android',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_fcm_token` (`token`(191)),
  KEY `idx_fcm_user` (`user_id`)
) ENGINE=InnoDB;

CREATE INDEX `idx_users_uuid` ON `users` (`user_uuid`);
CREATE INDEX `idx_wallet_tx_type` ON `wallet_transactions` (`type`);
CREATE INDEX `idx_wallet_tx_created` ON `wallet_transactions` (`created_at`);
CREATE INDEX `idx_room_status` ON `tournament_rooms` (`status`);
CREATE INDEX `idx_room_created` ON `tournament_rooms` (`created_at`);
CREATE INDEX `idx_results_created` ON `tournament_results` (`created_at`);
