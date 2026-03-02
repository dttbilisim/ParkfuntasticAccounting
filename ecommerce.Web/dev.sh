#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🧹 Cleaning up old processes...${NC}"
pkill -f "dotnet.*watch" 2>/dev/null
pkill -f "dotnet.*run" 2>/dev/null
pkill -f "ecommerce.Web" 2>/dev/null
sleep 1

PORT_PID=$(lsof -ti:5100 2>/dev/null)
if [ ! -z "$PORT_PID" ]; then
    echo -e "${RED}⚠️  Killing process on port 5100...${NC}"
    kill -9 $PORT_PID 2>/dev/null
    sleep 1
fi

echo -e "${GREEN}✅ Cleanup complete.${NC}"
echo ""
echo -e "${GREEN}⚡ DEV MODE - Blazor SSR Hot Reload${NC}"
echo -e "${YELLOW}💡 Hot reload enabled for .razor, .cs, .css files${NC}"
echo ""

# Blazor SSR için optimize edilmiş hot reload ayarları
export DOTNET_USE_POLLING_FILE_WATCHER=false
export DOTNET_WATCH_SUPPRESS_MSBUILD_INCREMENTALISM=false
export DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=true
export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true

cd "$(dirname "$0")"

# .NET 9.0 Blazor SSR için optimize edilmiş ayarlar
dotnet watch run \
  --launch-profile dev \
  --non-interactive \
  /p:UseSharedCompilation=true \
  /p:BuildInParallel=true
