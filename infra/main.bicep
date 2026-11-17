param location string = resourceGroup().location
var suffix = uniqueString(resourceGroup().id)

module registry 'modules/registry.bicep' = {
  name: 'registry'
  params: { name: 'acrmaintainops${suffix}', location: location }
}
