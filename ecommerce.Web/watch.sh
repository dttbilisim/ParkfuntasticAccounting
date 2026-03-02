#!/bin/bash

echo "🧹 Cleaning up old processes..."
pkill -f "dotnet.*watch" 2>/dev/null
pkill -f "dotnet.*run" 2>/dev/null
pkill -f "ecommerce.Web" 2>/dev/null
sleep 1

PORT_PID=$(lsof -ti:5100 2>/dev/null)
if [ ! -z "$PORT_PID" ]; then
    echo "⚠️  Killing process on port 5100..."
    kill -9 $PORT_PID 2>/dev/null
    sleep 1
fi

echo "✅ Cleanup complete."
echo ""
echo "🚀 Starting dotnet watch (Only Web project)..."
echo ""

cd "$(dirname "$0")"

# Only build ecommerce.Web, skip referenced projects
dotnet watch run \
  --no-build-deps \
  /p:BuildProjectReferences=false
