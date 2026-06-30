-- Run once on production MySQL before deploying matchmaking update:
CREATE TABLE IF NOT EXISTS matchmaking_queue (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    tournament_id VARCHAR(64) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX ix_matchmaking_queue_user_id (user_id),
    INDEX ix_matchmaking_queue_tournament_id (tournament_id)
);
