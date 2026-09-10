#!/usr/bin/env bash
set -euo pipefail

psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set ON_ERROR_STOP=1 <<'SQL'
CREATE DATABASE taskmgr_mvc;
CREATE DATABASE taskmgr_cleanarch;
SQL
