#!/bin/sh
# Runs automatically on container start (nginx's official image executes
# every executable script in /docker-entrypoint.d/ before starting nginx).
# Bakes the runtime API_BASE_URL into the static wwwroot/appsettings.json
# so the same built image works against any API URL without a rebuild.
set -eu

envsubst '${API_BASE_URL}' \
  < /usr/share/nginx/html/appsettings.template.json \
  > /usr/share/nginx/html/appsettings.json
