// =============================================================================
// Planova — Azure Infrastructure (Bicep)
// =============================================================================
// Deploys:
//   • Azure Container Registry (ACR)          — stores Docker image
//   • Azure SQL Server + Database             — SQL Server 2022 equivalent
//   • Azure App Service Plan (Linux)          — hosts the container
//   • Azure App Service (Web App for Containers) — runs Planova API
//   • Azure Key Vault                         — stores JWT key + SA password
//   • Log Analytics Workspace + App Insights  — observability
// =============================================================================

@description('Environment name (dev | staging | prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Base name for all resources — will be suffixed with env + unique token')
param appName string = 'planova'

@description('SQL Server administrator login')
param sqlAdminLogin string = 'sqladmin'

@description('SQL Server administrator password — store securely, never hard-code in VCS')
@secure()
param sqlAdminPassword string

@description('JWT signing key — must be a long random secret')
@secure()
param jwtKey string

@description('JWT Issuer value')
param jwtIssuer string = 'Planova'

@description('JWT Audience value')
param jwtAudience string = 'PlanovaUsers'

@description('JWT token lifetime in hours')
param jwtExpiresInHours int = 2

@description('ASP.NET Core environment (Development | Production)')
param aspNetCoreEnvironment string = environment == 'dev' ? 'Development' : 'Production'

@description('App Service SKU — use B1 for dev, P2v3 for prod')
param appServiceSku string = environment == 'prod' ? 'P2v3' : 'B1'

@description('SQL Database SKU')
param sqlSkuName string = environment == 'prod' ? 'S2' : 'Basic'

// ---------------------------------------------------------------------------
// Naming helpers
// ---------------------------------------------------------------------------
var uniqueSuffix = uniqueString(resourceGroup().id, appName)
var shortSuffix  = substring(uniqueSuffix, 0, 6)

var names = {
  logAnalytics:  '${appName}-logs-${environment}-${shortSuffix}'
  appInsights:   '${appName}-ai-${environment}-${shortSuffix}'
  keyVault:      '${appName}-kv-${environment}-${shortSuffix}'
  acr:           '${appName}acr${environment}${shortSuffix}'   // ACR only lowercase alphanumeric
  sqlServer:     '${appName}-sql-${environment}-${shortSuffix}'
  sqlDatabase:   '${appName}db'
  appServicePlan:'${appName}-asp-${environment}-${shortSuffix}'
  webApp:        '${appName}-app-${environment}-${shortSuffix}'
}

// Key Vault secret names
var secretNames = {
  sqlAdminPassword: 'sql-admin-password'
  jwtKey:           'jwt-key'
  sqlConnString:    'sql-connection-string'
}

// ---------------------------------------------------------------------------
// Log Analytics Workspace
// ---------------------------------------------------------------------------
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: names.logAnalytics
  location: location
  tags: { environment: environment, app: appName }
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: environment == 'prod' ? 90 : 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// ---------------------------------------------------------------------------
// Application Insights
// ---------------------------------------------------------------------------
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: names.appInsights
  location: location
  kind: 'web'
  tags: { environment: environment, app: appName }
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery:     'Enabled'
  }
}

// ---------------------------------------------------------------------------
// Azure Container Registry
// ---------------------------------------------------------------------------
resource acr 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: names.acr
  location: location
  tags: { environment: environment, app: appName }
  sku: {
    name: environment == 'prod' ? 'Standard' : 'Basic'
  }
  properties: {
    adminUserEnabled: true   // required for App Service pull without managed identity on Basic SKU
    publicNetworkAccess: 'Enabled'
  }
}

// ---------------------------------------------------------------------------
// Key Vault
// ---------------------------------------------------------------------------
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: names.keyVault
  location: location
  tags: { environment: environment, app: appName }
  properties: {
    sku: {
      family: 'A'
      name:   'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization:      true
    enableSoftDelete:             true
    softDeleteRetentionInDays:    environment == 'prod' ? 90 : 7
    enabledForTemplateDeployment: true
    publicNetworkAccess:          'Enabled'
  }
}

// Store SQL admin password
resource secretSqlPassword 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: secretNames.sqlAdminPassword
  properties: {
    value: sqlAdminPassword
    attributes: { enabled: true }
  }
}

// Store JWT key
resource secretJwtKey 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: secretNames.jwtKey
  properties: {
    value: jwtKey
    attributes: { enabled: true }
  }
}

// Store full connection string (set after SQL server is created)
resource secretConnString 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: secretNames.sqlConnString
  properties: {
    // Build connection string the same way docker-compose does:
    //   Server=<host>;Database=PlanovaDb;User=sa;Password=…;TrustServerCertificate=True
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${names.sqlDatabase};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
    attributes: { enabled: true }
  }
}

// ---------------------------------------------------------------------------
// Azure SQL Server
// ---------------------------------------------------------------------------
resource sqlServer 'Microsoft.Sql/servers@2022-11-01-preview' = {
  name: names.sqlServer
  location: location
  tags: { environment: environment, app: appName }
  properties: {
    administratorLogin:         sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version:                    '12.0'
    minimalTlsVersion:          '1.2'
    publicNetworkAccess:        'Enabled'  // allow App Service outbound; lock down further in prod with VNet
  }
}

// Allow Azure services (App Service) to reach SQL Server
resource sqlFirewallAzureServices 'Microsoft.Sql/servers/firewallRules@2022-11-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress:   '0.0.0.0'
  }
}

// Azure SQL Database — GeneralPurpose Serverless is a good default for non-prod
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-11-01-preview' = {
  parent: sqlServer
  name: names.sqlDatabase
  location: location
  tags: { environment: environment, app: appName }
  sku: {
    name:     sqlSkuName
    tier:     environment == 'prod' ? 'Standard' : 'Basic'
    capacity: environment == 'prod' ? 50 : 5
  }
  properties: {
    collation:                          'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes:                       environment == 'prod' ? 32212254720 : 2147483648 // 30 GB / 2 GB
    zoneRedundant:                      false
    readScale:                          'Disabled'
    requestedBackupStorageRedundancy:   environment == 'prod' ? 'Geo' : 'Local'
  }
}

// ---------------------------------------------------------------------------
// App Service Plan (Linux)
// ---------------------------------------------------------------------------
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: names.appServicePlan
  location: location
  tags: { environment: environment, app: appName }
  kind: 'linux'
  sku: {
    name: appServiceSku
  }
  properties: {
    reserved: true   // required for Linux
  }
}

// ---------------------------------------------------------------------------
// App Service — Web App for Containers
// ---------------------------------------------------------------------------
resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: names.webApp
  location: location
  tags: { environment: environment, app: appName }
  kind: 'app,linux,container'
  identity: {
    type: 'SystemAssigned'   // used to pull from ACR and read Key Vault
  }
  properties: {
    serverFarmId:          appServicePlan.id
    httpsOnly:             true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion:  'DOCKER|${acr.properties.loginServer}/${appName}:latest'
      alwaysOn:        environment != 'dev'
      http20Enabled:   true
      minTlsVersion:   '1.2'
      ftpsState:       'Disabled'
      healthCheckPath: '/health/live'

      // App settings — mirrors docker-compose environment block
      appSettings: [
        {
          name:  'ASPNETCORE_ENVIRONMENT'
          value: aspNetCoreEnvironment
        }
        {
          name:  'ASPNETCORE_URLS'
          value: 'http://+:8080'
        }
        {
          name:  'WEBSITES_PORT'
          value: '8080'
        }
        // ACR credentials for container pull
        {
          name:  'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://${acr.properties.loginServer}'
        }
        {
          name:  'DOCKER_REGISTRY_SERVER_USERNAME'
          value: acr.listCredentials().username
        }
        {
          name:  'DOCKER_REGISTRY_SERVER_PASSWORD'
          value: acr.listCredentials().passwords[0].value
        }
        // Connection string via Key Vault reference
        {
          name:  'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${secretConnString.properties.secretUri})'
        }
        // JWT settings via Key Vault reference
        {
          name:  'Jwt__Key'
          value: '@Microsoft.KeyVault(SecretUri=${secretJwtKey.properties.secretUri})'
        }
        {
          name:  'Jwt__Issuer'
          value: jwtIssuer
        }
        {
          name:  'Jwt__Audience'
          value: jwtAudience
        }
        {
          name:  'Jwt__ExpiresInHours'
          value: string(jwtExpiresInHours)
        }
        // Application Insights
        {
          name:  'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name:  'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
      ]
    }
  }
}

// ---------------------------------------------------------------------------
// Key Vault RBAC — grant Web App's managed identity read access to secrets
// ---------------------------------------------------------------------------
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User (built-in)

resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webApp.id, kvSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId:      webApp.identity.principalId
    principalType:    'ServicePrincipal'
  }
}

// ---------------------------------------------------------------------------
// Diagnostic settings — stream App Service logs to Log Analytics
// ---------------------------------------------------------------------------
resource webAppDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'send-to-log-analytics'
  scope: webApp
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      { category: 'AppServiceHTTPLogs';           enabled: true }
      { category: 'AppServiceConsoleLogs';         enabled: true }
      { category: 'AppServiceAppLogs';             enabled: true }
      { category: 'AppServiceAuditLogs';           enabled: true }
      { category: 'AppServiceIPSecAuditLogs';      enabled: true }
      { category: 'AppServicePlatformLogs';        enabled: true }
    ]
    metrics: [
      { category: 'AllMetrics'; enabled: true }
    ]
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
@description('App Service URL')
output appUrl string = 'https://${webApp.properties.defaultHostName}'

@description('ACR login server')
output acrLoginServer string = acr.properties.loginServer

@description('SQL Server FQDN')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('SQL Database name')
output sqlDatabaseName string = sqlDatabase.name

@description('Key Vault URI')
output keyVaultUri string = keyVault.properties.vaultUri

@description('Application Insights connection string')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Web App name — used for az webapp config container set commands')
output webAppName string = webApp.name

@description('Resource group name')
output resourceGroupName string = resourceGroup().name
