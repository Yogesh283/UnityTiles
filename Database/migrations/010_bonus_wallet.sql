-- Signup bonus wallet: tournament-only, not withdrawable.
-- Tournament entry uses 20% bonus + 80% main while bonus > 0.

SET @db := DATABASE();

SET @col := (
  SELECT COUNT(1) FROM information_schema.COLUMNS
  WHERE table_schema = @db AND table_name = 'wallet' AND column_name = 'bonus_balance'
);
SET @sql := IF(
  @col = 0,
  'ALTER TABLE `wallet` ADD COLUMN `bonus_balance` INT NOT NULL DEFAULT 0 AFTER `balance`',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT INTO `settings` (`key`, `value`) VALUES ('signup_bonus_coins', '100')
ON DUPLICATE KEY UPDATE `value` = `value`;

INSERT INTO `settings` (`key`, `value`) VALUES ('bonus_entry_percent', '20')
ON DUPLICATE KEY UPDATE `value` = `value`;
