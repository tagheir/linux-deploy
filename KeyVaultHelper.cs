using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;

namespace azureFileWatch
{
    public class KeyVaultHelper
    {
        public static string TenantId { get; set; }
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
        public static string KeyVaultUrl { get; set; }
        public static string GetSecret(string key)
        {
            var credentials = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
            var client = new SecretClient(new Uri(KeyVaultUrl), credentials);

            var secret = client.GetSecret(key);

            string secretValue = secret?.Value?.Value;
            return secretValue;
        }
    }
}
