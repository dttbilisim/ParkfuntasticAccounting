#!/bin/bash

echo "🧹 Cleaning up old processes..."
pkill -f "dotnet.*watch" 2>/dev/null
pkill -f "dotnet.*run" 2>/dev/null
pkill -f "ecommerce.Web" 2>/dev/null
sleep 1

PORT_PID=$(lsof -ti:5100 2>/dev/null)
if [ ! -z "$PORT_PID" ]; then
    kill -9 $PORT_PID 2>/dev/null
    sleep 1
fi

echo "✅ Cleanup complete."
echo ""
echo "🚀 TURBO MODE: Skip all dependency builds"
echo "💡 Using pre-built DLLs (fastest startup)"
echo ""

cd "$(dirname "$0")"

# Use already built DLLs, don't rebuild anything
dotnet run --no-build --no-restore
