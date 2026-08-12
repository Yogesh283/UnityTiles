-- USDT BEP20 withdrawals (admin-approved)

CREATE TABLE IF NOT EXISTS `withdrawal_requests` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `request_code` VARCHAR(64) NOT NULL,
  `user_id` BIGINT UNSIGNED NOT NULL,
  `coins` INT NOT NULL,
  `usdt_amount` DECIMAL(18,8) NOT NULL,
  `bep20_address` VARCHAR(64) NOT NULL,
  `status` VARCHAR(32) NOT NULL DEFAULT 'pending',
  `admin_note` VARCHAR(255) NULL,
  `payout_tx` VARCHAR(128) NULL,
  `reviewed_by` VARCHAR(64) NULL,
  `reviewed_at` DATETIME NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_withdraw_code` (`request_code`),
  KEY `idx_withdraw_user` (`user_id`),
  KEY `idx_withdraw_status` (`status`)
) ENGINE=InnoDB;
