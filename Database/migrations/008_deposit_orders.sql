-- UPI (Razorpay) + USDT BEP20 (NowPayments) deposit orders

CREATE TABLE IF NOT EXISTS `deposit_orders` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `order_code` VARCHAR(64) NOT NULL,
  `user_id` BIGINT UNSIGNED NOT NULL,
  `method` VARCHAR(16) NOT NULL,
  `provider` VARCHAR(32) NOT NULL,
  `pay_amount` DECIMAL(18,8) NOT NULL,
  `pay_currency` VARCHAR(8) NOT NULL,
  `coins` INT NOT NULL,
  `status` VARCHAR(32) NOT NULL DEFAULT 'pending',
  `provider_order_id` VARCHAR(128) NULL,
  `provider_payment_id` VARCHAR(128) NULL,
  `checkout_url` VARCHAR(1024) NULL,
  `extra` TEXT NULL,
  `paid_at` DATETIME NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_deposit_order_code` (`order_code`),
  KEY `idx_deposit_user` (`user_id`),
  KEY `idx_deposit_status` (`status`),
  KEY `idx_deposit_provider_order` (`provider_order_id`)
) ENGINE=InnoDB;
