@description('The Azure region for all resources.')
param location string = resourceGroup().location

@description('Unique suffix for resource names.')
param environmentName string

@secure()
@description('SQL Server administrator password.')
param sqlAdminPassword string

@description('SQL Server administrator username.')
param sqlAdminLogin string = 'terrariumadmin'

@description('Container image for Terrarium.Server.')
param serverImage string

@description('Container image for Terrarium.Web.')
param webImage string

// ---------- Log Analytics + Container Apps Environment ----------

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${environmentName}'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${environmentName}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ---------- Azure SQL ----------

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'sql-${environmentName}'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'Terrarium'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

resource sqlFirewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---------- Terrarium.Server Container App ----------

resource serverApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'server-${environmentName}'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
      secrets: [
        {
          name: 'sql-connection'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=Terrarium;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=true;TrustServerCertificate=false;'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'server'
          image: serverImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__Terrarium'
              secretRef: 'sql-connection'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

// ---------- Terrarium.Web Container App ----------

resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'web-${environmentName}'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'services__server__https__0'
              value: 'https://${serverApp.properties.configuration.ingress.fqdn}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

output webUrl string = 'https://${webApp.properties.configuration.ingress.fqdn}'
output serverUrl string = 'https://${serverApp.properties.configuration.ingress.fqdn}'
