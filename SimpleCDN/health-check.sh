#!/bin/sh

# Get the application URL from the ASPNETCORE_URLS environment variable
APP_URL=${ASPNETCORE_URLS:-"http://localhost:5000"}

# replace + and * with localhost
APP_URL=$(echo $APP_URL | sed 's/\*/localhost/g' | sed 's/\+/localhost/g')

# Perform the health check using curl
curl -f $APP_URL/_cdn/server/health || exit 1

echo "Health check passed."
