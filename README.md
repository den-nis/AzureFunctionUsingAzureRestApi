# AzureFunctionUsingAzureRestApi
Example of an Azure function using the azure API to turn on and off a virtual machine

# Setup 
To make this example work you will need to provide the function with the following environment variables
- ClientId
- TenantId
- SubscriptionId
- ResourceGroup
- Machine
- Secret

# Usage

Deallocate the target machine
```http://{{function}}/api/MachineOperation?operation=deallocate```

Start the target machine
```http://{{function}}/api/MachineOperation?operation=start```
