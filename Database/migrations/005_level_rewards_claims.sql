-- MySQL 5.7+ compatible: add per-user level reward claim columns.

SET @db := DATABASE();

SET @col := (
  SELECT COUNT(1) FROM information_schema.COLUMNS
  WHERE table_schema = @db AND table_name = 'level_rewards' AND column_name = 'user_id'
);
SET @sql := IF(
  @col = 0,
  'ALTER TABLE `level_rewards` ADD COLUMN `user_id` BIGINT NULL',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col := (
  SELECT COUNT(1) FROM information_schema.COLUMNS
  WHERE table_schema = @db AND table_name = 'level_rewards' AND column_name = 'user_uuid'
);
SET @sql := IF(
  @col = 0,
  'ALTER TABLE `level_rewards` ADD COLUMN `user_uuid` VARCHAR(36) NULL',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col := (
  SELECT COUNT(1) FROM information_schema.COLUMNS
  WHERE table_schema = @db AND table_name = 'level_rewards' AND column_name = 'level_number'
);
SET @sql := IF(
  @col = 0,
  'ALTER TABLE `level_rewards` ADD COLUMN `level_number` INT NULL',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col := (
  SELECT COUNT(1) FROM information_schema.COLUMNS
  WHERE table_schema = @db AND table_name = 'level_rewards' AND column_name = 'reward_coins'
);
SET @sql := IF(
  @col = 0,
  'ALTER TABLE `level_rewards` ADD COLUMN `reward_coins` INT NOT NULL DEFAULT 50',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col := (
  SELECT COUNT(1) FROM information_schema.COLUMNS
  WHERE table_schema = @db AND table_name = 'level_rewards' AND column_name = 'rewarded_at'
);
SET @sql := IF(
  @col = 0,
  'ALTER TABLE `level_rewards` ADD COLUMN `rewarded_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_index := (
  SELECT COUNT(1)
  FROM information_schema.statistics
  WHERE table_schema = @db
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
  WHERE table_schema = @db
    AND table_name = 'level_rewards'
    AND index_name = 'idx_level_rewards_user_uuid'
);
SET @sql := IF(
  @has_uuid_index = 0,
  'CREATE INDEX `idx_level_rewards_user_uuid` ON `level_rewards` (`user_uuid`)',
  'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
