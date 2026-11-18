param location string = resourceGroup().location
param imageTag string

var suffix = uniqueString(resourceGroup().id)

module registry 'modules/registry.bicep' = {
  name: 'registry'
  params: { name: 'acrmaintainops${suffix}', location: location }
}

module plan 'modules/appServicePlan.bicep' = {
  name: 'plan'
  params: { name: 'asp-maintainops-${suffix}', location: location }
}

module api 'modules/appService.bicep' = {
  name: 'api'
  params: {
    name: 'app-maintainops-api-${suffix}'
    location: location
    planId: plan.outputs.id
    registryName: registry.outputs.name
    registryLoginServer: registry.outputs.loginServer
    imageName: 'maintainops-api'
    imageTag: imageTag
    port: 5286
  }
}

module web 'modules/appService.bicep' = {
  name: 'web'
  params: {
    name: 'app-maintainops-web-${suffix}'
    location: location
    planId: plan.outputs.id
    registryName: registry.outputs.name
    registryLoginServer: registry.outputs.loginServer
    imageName: 'maintainops-web'
    imageTag: imageTag
    port: 8080
  }
}

output apiUrl string = 'https://${api.outputs.hostName}'
output webUrl string = 'https://${web.outputs.hostName}'
