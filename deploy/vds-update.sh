#!/usr/bin/env bash
set -eu

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT/backend"

docker compose -f docker-compose.yml -f docker-compose.vds.yml up --build -d
docker image prune -f
