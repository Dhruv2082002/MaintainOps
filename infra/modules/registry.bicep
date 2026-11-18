param name string
param location string

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2025-11-01' = {
  name: name
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

output loginServer string = containerRegistry.properties.loginServer
output name string = containerRegistry.name


