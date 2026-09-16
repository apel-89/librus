#!/usr/bin/env bash

set -euo pipefail

cd "$(dirname "$0")/.."

API_DIR="backend/src/Librus.Api"
WEB_DIR="web"


require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Saknar $1. $2" >&2
    exit 1
  fi
}

require docker "Installera Docker Desktop: https://docs.docker.com/get-docker/"
require dotnet "Installera .NET SDK 10: https://dotnet.microsoft.com/download"
require node   "Installera Node 20 eller senare: https://nodejs.org"

check_port() {
  local port=$1 name=$2
  local pid
  pid=$(lsof -t -iTCP:"$port" -sTCP:LISTEN 2>/dev/null || true)

  if [ -n "$pid" ]; then
    echo "Port $port ($name) används redan av process $pid." >&2
    echo "Avsluta den med: kill $pid" >&2
    exit 1
  fi
}

if ! docker info >/dev/null 2>&1; then
  echo "Docker är installerat men körs inte. Starta Docker och försök igen." >&2
  exit 1
fi


echo "==> Startar Postgres"
docker compose up -d --wait


echo "==> Hämtar NuGet-paket"
dotnet restore backend/Librus.slnx

if [ ! -d "$WEB_DIR/node_modules" ]; then
  echo "==> Installerar npm-paket (första gången, tar en stund)"
  npm --prefix "$WEB_DIR" install
else
  echo "==> npm-paket finns redan"
fi

if [ ! -f "$WEB_DIR/.env.local" ]; then
  echo "==> Skapar web/.env.local"
  echo "NEXT_PUBLIC_API_URL=http://localhost:5092" > "$WEB_DIR/.env.local"
fi


pids=()

kill_tree() {
  local pid=$1
  # Döda barnen först, annars blir de föräldralösa och behåller portarna.
  for child in $(pgrep -P "$pid" 2>/dev/null); do
    kill_tree "$child"
  done
  kill "$pid" 2>/dev/null || true
}

free_port() {
  local pid
  pid=$(lsof -t -iTCP:"$1" -sTCP:LISTEN 2>/dev/null || true)
  [ -n "$pid" ] && kill $pid 2>/dev/null || true
}

shutdown() {
  echo ""
  echo "==> Stänger ner"

  for pid in "${pids[@]}"; do
    kill_tree "$pid"
  done

  sleep 0.5

  # Skyddsnät om något ändå överlevde.
  free_port 5092
  free_port 3000

  exit 0
}


trap shutdown INT TERM

check_port 5092 "API"
check_port 3000 "frontend"

echo "==> Startar API (migrerar och seedar vid behov)"
dotnet run --project "$API_DIR" &
pids+=($!)

echo "==> Startar frontend"
npm --prefix "$WEB_DIR" run dev &
pids+=($!)

echo ""
echo "  Frontend   http://localhost:3000"
echo "  API        http://localhost:5092"
echo "  API-docs   http://localhost:5092/scalar/v1"
echo ""
echo "  Ctrl+C avslutar båda."
echo ""

wait
shutdown