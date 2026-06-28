-- Google Play in-app purchase records
USE `game`;

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
