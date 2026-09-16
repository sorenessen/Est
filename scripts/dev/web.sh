#!/bin/zsh

set -u

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/common.sh"

PORT=5173
PAGE_URL="http://localhost:${PORT}/"

web_is_healthy() {
    local response

    response="$(
        curl -fsS --max-time 1 "$PAGE_URL" 2>/dev/null
    )" || return 1

    [[ "$response" == *'<!doctype html>'* ||
       "$response" == *'<html'* ]]
}

web_process_is_est_owned() {
    local pid="$1"
    local command
    local cwd

    command="$(process_command "$pid")"
    cwd="$(process_cwd "$pid")"

    [[ "$cwd" == "$EST_ROOT/src/Est.Web" ]] || return 1

    [[ "$command" == *"vite"* ||
       "$command" == *"node"* ||
       "$command" == *"npm"* ]]
}

pid="$(listener_pid "$PORT")"

if [[ -n "$pid" ]]; then
    command="$(process_command "$pid")"
    cwd="$(process_cwd "$pid")"

    if ! web_process_is_est_owned "$pid"; then
        echo "Port $PORT is occupied by a process that Est does not own."
        echo "PID: $pid"
        echo "Command: ${command:-unknown}"
        echo "Working directory: ${cwd:-unknown}"
        echo "Refusing to touch it."
        exit 1
    fi

    if web_is_healthy; then
        echo "Est.Web is already healthy on port $PORT."
        exit 0
    fi

    echo "Est.Web owns port $PORT but did not pass its health check."
    echo "PID: $pid"
    echo "Command: ${command:-unknown}"
    echo "Working directory: ${cwd:-unknown}"
    echo "Refusing to restart it automatically."
    exit 1
fi

launch_command="cd ${(q)EST_ROOT}/src/Est.Web && npm run dev"

echo "Starting Est.Web in its own iTerm window..."
open_iterm_window "$launch_command"

if wait_for_url "$PAGE_URL" 60 && web_is_healthy; then
    echo "Est.Web is healthy at $PAGE_URL."
    exit 0
fi

echo "Est.Web did not become healthy in time."
echo "Its service window has been left open for inspection."
exit 1
