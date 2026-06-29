-- New users start with 0 coins (deposit / IAP to join tournaments)
UPDATE `settings` SET `value` = '0' WHERE `key` = 'default_coins';
INSERT INTO `settings` (`key`, `value`) VALUES ('default_coins', '0')
ON DUPLICATE KEY UPDATE `value` = '0';

ALTER TABLE `wallet` MODIFY `balance` INT NOT NULL DEFAULT 0;
