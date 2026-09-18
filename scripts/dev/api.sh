#!/bin/zsh

set -u

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/common.sh"

DOTNET_BIN="$(command -v dotnet 2>/dev/null || true)"

if [[ -z "$DOTNET_BIN" && -x /usr/local/share/dotnet/dotnet ]]; then
    DOTNET_BIN=/usr/local/share/dotnet/dotnet
fi

if [[ -z "$DOTNET_BIN" ]]; then
    echo "Est.Api requires the .NET SDK, but dotnet could not be found."
    exit 1
fi

PORT=5026
HEALTH_URL="http://127.0.0.1:${PORT}/health"

api_is_healthy() {
    local response

    response="$(
        curl -fsS --max-time 1 "$HEALTH_URL" 2>/dev/null
    )" || return 1

    [[ "$response" == *'"service":"Est.Api"'* &&
       "$response" == *'"status":"healthy"'* ]]
}

api_health_status() {
    curl \
        -sS \
        -o /dev/null \
        -w '%{http_code}' \
        --max-time 1 \
        "$HEALTH_URL" \
        2>/dev/null || true
}

api_process_is_est_owned() {
    local pid="$1"
    local command
    local cwd

    command="$(process_command "$pid")"
    cwd="$(process_cwd "$pid")"

    [[ "$cwd" == "$EST_ROOT/src/Est.Api" ]] || return 1

    [[ "$command" == *"Est.Api"* ||
       "$command" == *"dotnet"* ]]
}

pid="$(listener_pid "$PORT")"

if [[ -n "$pid" ]]; then
    command="$(process_command "$pid")"
    cwd="$(process_cwd "$pid")"

    if ! api_process_is_est_owned "$pid"; then
        echo "Port $PORT is occupied by a process that Est does not own."
        echo "PID: $pid"
        echo "Command: ${command:-unknown}"
        echo "Working directory: ${cwd:-unknown}"
        echo "Refusing to touch it."
        exit 1
    fi

    if api_is_healthy; then
        echo "Est.Api is already healthy on port $PORT."
        exit 0
    fi

    health_status="$(api_health_status)"

    if [[ "$health_status" == "404" ]]; then
        echo "Est.Api is already running on port $PORT."
        echo "This process predates the /health endpoint, so it will be reused."
        echo "PID: $pid"
        exit 0
    fi

    echo "Est.Api owns port $PORT but did not pass its health check."
    echo "PID: $pid"
    echo "Command: ${command:-unknown}"
    echo "Working directory: ${cwd:-unknown}"
    echo "Refusing to restart it automatically."
    exit 1
fi

launch_command="cd ${(q)EST_ROOT}/src/Est.Api && ${(q)DOTNET_BIN} run --launch-profile http"

echo "Starting Est.Api in its own iTerm window..."
open_iterm_window "$launch_command"

if wait_for_url "$HEALTH_URL" 60 && api_is_healthy; then
    echo "Est.Api is healthy at $HEALTH_URL."
    exit 0
fi

echo "Est.Api did not become healthy in time."
echo "Its service window has been left open for inspection."
exit 1
