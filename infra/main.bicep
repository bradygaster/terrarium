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

// ---------- Azure SignalR Service ----------

resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: 'signalr-${environmentName}'
  location: location
  sku: {
    name: 'Standard_S1'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Default'
      }
      {
        flag: 'EnableConnectivityLogs'
        value: 'true'
      }
    ]
    cors: {
      allowedOrigins: ['*']
    }
    serverless: {
      connectionTimeoutInSeconds: 60
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
        stickySessions: {
          affinity: 'sticky'
        }
      }
      secrets: [
        {
          name: 'sql-connection'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=Terrarium;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=true;TrustServerCertificate=false;'
        }
        {
          name: 'signalr-connection'
          value: listKeys(signalR.id, signalR.apiVersion).primaryConnectionString
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
            {
              name: 'ConnectionStrings__signalr'
              secretRef: 'signalr-connection'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/alive'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 30
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 5
              periodSeconds: 10
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              type: 'Startup'
              httpGet: {
                path: '/alive'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 0
              periodSeconds: 5
              timeoutSeconds: 5
              failureThreshold: 30
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'cpu-scaling'
            custom: {
              type: 'cpu'
              metadata: {
                type: 'Utilization'
                value: '70'
              }
            }
          }
          {
            name: 'signalr-connection-scaling'
            custom: {
              type: 'azure-monitor'
              metadata: {
                metricName: 'ConnectionCount'
                metricResourceUri: signalR.id
                targetValue: '100'
              }
              auth: [
                {
                  secretRef: 'signalr-connection'
                  triggerParameter: 'connectionFromEnv'
                }
              ]
            }
          }
        ]
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
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/alive'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 30
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 5
              periodSeconds: 10
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              type: 'Startup'
              httpGet: {
                path: '/alive'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 0
              periodSeconds: 5
              timeoutSeconds: 5
              failureThreshold: 30
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

output webUrl string = 'https://${webApp.properties.configuration.ingress.fqdn}'
output serverUrl string = 'https://${serverApp.properties.configuration.ingress.fqdn}'
output signalREndpoint string = signalR.properties.hostName
output signalRResourceId string = signalR.id
