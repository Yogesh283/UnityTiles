ALTER TABLE `level_rewards`
  ADD COLUMN IF NOT EXISTS `user_id` BIGINT NULL,
  ADD COLUMN IF NOT EXISTS `user_uuid` VARCHAR(36) NULL,
  ADD COLUMN IF NOT EXISTS `level_number` INT NULL,
  ADD COLUMN IF NOT EXISTS `reward_coins` INT NOT NULL DEFAULT 50,
  ADD COLUMN IF NOT EXISTS `rewarded_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP;

SET @has_index := (
  SELECT COUNT(1)
  FROM information_schema.statistics
  WHERE table_schema = DATABASE()
    AND table_name = 'level_rewards'
    AND index_name = 'uq_level_rewards_user_level'
);
SET @sql := IF(
  @has_index = 0,
  'CREATE UNIQUE INDEX `uq_level_rewards_user_level` ON `level_rewards` (`user_id`, `level_number`)',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_uuid_index := (
  SELECT COUNT(1)
  FROM information_schema.statistics
  WHERE table_schema = DATABASE()
    AND table_name = 'level_rewards'
    AND index_name = 'idx_level_rewards_user_uuid'
);
SET @sql2 := IF(
  @has_uuid_index = 0,
  'CREATE INDEX `idx_level_rewards_user_uuid` ON `level_rewards` (`user_uuid`)',
  'SELECT 1'
);
PREPARE stmt2 FROM @sql2;
EXECUTE stmt2;
DEALLOCATE PREPARE stmt2;
