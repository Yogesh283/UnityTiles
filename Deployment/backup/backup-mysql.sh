#!/bin/bash
# Match IQ MySQL backup script
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/var/backups/matchiq}"
MYSQL_USER="${MYSQL_USER:-root}"
MYSQL_PASSWORD="${MYSQL_PASSWORD:-}"
MYSQL_DATABASE="${MYSQL_DATABASE:-game}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
FILENAME="${BACKUP_DIR}/${MYSQL_DATABASE}_${TIMESTAMP}.sql.gz"

mkdir -p "$BACKUP_DIR"

mysqldump -u"$MYSQL_USER" -p"$MYSQL_PASSWORD" "$MYSQL_DATABASE" | gzip > "$FILENAME"

# Retention: daily 7, weekly 4, monthly 6
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +7 -delete

echo "Backup saved: $FILENAME"
