#!/bin/bash

# Ultra-fast restart script
# Use this when app is already built and you just want to restart quickly

echo "🔄 Quick restart..."

# Kill existing processes
pkill -f "dotnet.*watch" 2>/dev/null
pkill -f "dotnet.*run" 2>/dev/null
pkill -f "ecommerce.Web" 2>/dev/null

# Kill port
lsof -ti:5100 | xargs kill -9 2>/dev/null

sleep 1

echo "⚡ Starting (no build)..."

cd "$(dirname "$0")"

# Fastest possible: no build, no restore, use existing DLLs
dotnet run --no-build --no-restore --no-launch-profile 2>&1

