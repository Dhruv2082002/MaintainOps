param name string
param location string

resource plan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: name
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
  }
  properties: {
    reserved: true
  }
}

output id string = plan.id
