-- Support tickets: player query → admin reply

CREATE TABLE IF NOT EXISTS `support_tickets` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `ticket_code` VARCHAR(32) NOT NULL,
  `user_id` BIGINT UNSIGNED NOT NULL,
  `category` VARCHAR(64) NOT NULL DEFAULT 'general',
  `subject` VARCHAR(160) NOT NULL,
  `message` TEXT NOT NULL,
  `status` VARCHAR(32) NOT NULL DEFAULT 'open',
  `admin_reply` TEXT NULL,
  `reviewed_by` VARCHAR(128) NULL,
  `reviewed_at` DATETIME NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_ticket_code` (`ticket_code`),
  KEY `idx_ticket_user` (`user_id`),
  KEY `idx_ticket_status` (`status`),
  KEY `idx_ticket_created` (`created_at`)
) ENGINE=InnoDB;
