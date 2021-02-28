using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Linq;

namespace AzureApiExample
{
    public static class MachineOperation
    {
        const string AUTHORITY = "https://login.microsoftonline.com/";
        const string RESOURCE = "https://management.azure.com/";
        const string APIVERSION = "2020-12-01";

        static readonly string[] _possibleOperations = new[] { "start", "deallocate" }; 

        [FunctionName("MachineOperation")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger _)
        {
            //Setup environment variables
            var subscription  = GetVariable("SubscriptionId");
            var client        = GetVariable("ClientId");
            var tenant        = GetVariable("TenantId");
            var secret        = GetVariable("Secret");
            var resourceGroup = GetVariable("ResourceGroup");
            var machine       = GetVariable("Machine");

            //Parse and validate the request input
            string operation = req.Query["operation"].ToString().ToLower();

            if (!_possibleOperations.Contains(operation))
                return new NotFoundObjectResult($"No such operation {operation}");

            //Get the bearer token from azure
            var token = GetAzureToken(new ClientCredential(client, secret), tenant);

            //Start / deallocate the machine
            var result = await SetMachineOperation(subscription, resourceGroup, machine, token, operation);

            //Return the result
            if (!result.IsSuccessStatusCode)
                return new StatusCodeResult((int)result.StatusCode);

            return new OkResult();
        }

        /// <summary>
        /// Will throw if the variable is not defined
        /// </summary>
        static string GetVariable(string name)
        {
            string variable = Environment.GetEnvironmentVariable(name);

            if (variable == null)
                throw new InvalidOperationException($"Missing environment variable \"{name}\"");

            return variable;
        }

        /// <summary>
        /// Gets the azure bearer token
        /// </summary>
        static string GetAzureToken(ClientCredential clientCredentials, string tentant)
        {
            string authority = AUTHORITY + tentant;
            var authenticationContext = new AuthenticationContext(authority);
            var result = authenticationContext.AcquireTokenAsync(RESOURCE, clientCredentials).Result;

            if (result == null)
                throw new ArgumentException("Failed to obtain token");

            return result.AccessToken;
        }

        /// <summary>
        /// Either start or deallocate the target machine
        /// </summary>
        static async Task<HttpResponseMessage> SetMachineOperation(string subscription, string resourceGroup, string machine, string token, string operation)
        {
            var url = $"{RESOURCE}subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.Compute/virtualMachines/{machine}/{operation}?api-version={APIVERSION}";

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            return await client.PostAsync(url, null);
        }
    }
}
