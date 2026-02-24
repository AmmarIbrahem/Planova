#!/usr/bin/env bash
# =============================================================================
# deploy.sh — Build, push, and deploy Planova to Azure
# =============================================================================
# Prerequisites:
#   az CLI logged in (az login)
#   Docker running
#   jq installed
# Usage:
#   ./deploy.sh [dev|staging|prod] [resource-group] [location]
# =============================================================================
set -euo pipefail

ENV="${1:-dev}"
RG="${2:-planova-rg-${ENV}}"
LOCATION="${3:-westeurope}"
APP_NAME="planova"

echo "==> Deploying Planova [$ENV] to [$RG] in [$LOCATION]"

# ---------------------------------------------------------------------------
# 1. Ensure resource group exists
# ---------------------------------------------------------------------------
echo "==> Creating resource group if not exists..."
az group create \
  --name "$RG" \
  --location "$LOCATION" \
  --tags environment="$ENV" app="$APP_NAME" \
  --output none

# ---------------------------------------------------------------------------
# 2. Deploy Bicep (pass secrets directly — or use --parameters file with KV refs)
# ---------------------------------------------------------------------------
echo "==> Running Bicep deployment (what-if first)..."

# Generate random secrets if not set in environment
SQL_PASSWORD="${SQL_ADMIN_PASSWORD:-$(openssl rand -base64 24)}"
JWT_KEY_VAL="${JWT_KEY:-$(openssl rand -hex 32)}"

DEPLOY_OUTPUT=$(az deployment group create \
  --resource-group "$RG" \
  --template-file main.bicep \
  --parameters \
      environment="$ENV" \
      appName="$APP_NAME" \
      sqlAdminLogin="sqladmin" \
      sqlAdminPassword="$SQL_PASSWORD" \
      jwtKey="$JWT_KEY_VAL" \
  --output json)

echo "$DEPLOY_OUTPUT" | jq '.properties.outputs'

# ---------------------------------------------------------------------------
# 3. Extract outputs
# ---------------------------------------------------------------------------
ACR_SERVER=$(echo "$DEPLOY_OUTPUT"  | jq -r '.properties.outputs.acrLoginServer.value')
WEBAPP_NAME=$(echo "$DEPLOY_OUTPUT" | jq -r '.properties.outputs.webAppName.value')

echo "==> ACR:     $ACR_SERVER"
echo "==> WebApp:  $WEBAPP_NAME"

# ---------------------------------------------------------------------------
# 4. Build & push Docker image to ACR
# ---------------------------------------------------------------------------
echo "==> Logging in to ACR..."
az acr login --name "${ACR_SERVER%%.*}"

IMAGE_TAG="${ACR_SERVER}/${APP_NAME}:latest"
GIT_SHA=$(git rev-parse --short HEAD 2>/dev/null || echo "local")
IMAGE_SHA_TAG="${ACR_SERVER}/${APP_NAME}:${GIT_SHA}"

echo "==> Building Docker image..."
docker build \
  -t "$IMAGE_TAG" \
  -t "$IMAGE_SHA_TAG" \
  -f Dockerfile \
  .

echo "==> Pushing image..."
docker push "$IMAGE_TAG"
docker push "$IMAGE_SHA_TAG"

# ---------------------------------------------------------------------------
# 5. Restart App Service to pick up new image
# ---------------------------------------------------------------------------
echo "==> Restarting App Service..."
az webapp restart \
  --name "$WEBAPP_NAME" \
  --resource-group "$RG"

# ---------------------------------------------------------------------------
# 6. Tail startup logs briefly
# ---------------------------------------------------------------------------
APP_URL=$(echo "$DEPLOY_OUTPUT" | jq -r '.properties.outputs.appUrl.value')
echo ""
echo "==> Deployment complete!"
echo "    App URL:  $APP_URL"
echo "    Swagger:  $APP_URL/swagger  (Development mode)"
echo "    Health:   $APP_URL/health"
echo ""
