using System.Net;
using Conjur;

namespace Example;

internal class Program
{
    // this example shows how to use the Conjur .NET api
    // to login, get a secret value, & check a permission
    // the credentials are passed as arguments.
    // Credentials are typically a hostId and api_key or
    // userId and password
    public static void Main(string[] args)
    {
        if (args.Length < 7)
        {
            Console.WriteLine("Usage: Example <applianceHostName> <applianceCertificatePath> <accountName> <username> <password> <variableId> <hostFactoryToken>");
            return;
        }

        var applianceName = args[0];
        var certPath = args[1];
        var account = args[2];
        var username = args[3];
        var password = args[4];
        var variableId = args[5];
        var token = args[6];

        // Instantiate a Conjur Client object.
        //  parameter: applianceUri - conjur appliance URI
        //  return: Client object - if URI is incorrect errors thrown when used
        var uri = $"https://{applianceName}";
        var conjurClient = new Client(uri, account);

        // If the Conjur root certificate is not in the system trust store,
        // add it as trusted explicitly
        if (certPath.Length > 0) 
        {
            conjurClient.TrustedCertificates.ImportPem (certPath);
        }

        // Login with Conjur userid and password,
        // or host_id and api_key, etc.
        //  parameters: username - conjur user or host id for example
        //              password - conjur user password
        try
        {
            conjurClient.LogIn(username, password);
            Console.WriteLine("Logged in as '{0}' to '{1}'", username, applianceName);
        }
        catch (Exception e)
        {
            Console.WriteLine("Authentication failed. An exception occurred '{0}'", e);

            // to log in with an API key use it directly, i.e.
            var apiKey = password;
            conjurClient.Credential = new NetworkCredential(username, apiKey);
        }

        // Load policy to root with request variable id
        var policy = conjurClient.Policy("root");
        using (var ms = new MemoryStream())
        {
            using var sw = new StreamWriter(ms);
            sw.WriteLine("- !variable");
            sw.WriteLine($"  id: {variableId}");
            sw.Flush();
            var policyOutputStream = policy.LoadPolicy(ms);
            using var reader = new StreamReader(policyOutputStream);
            var policyLoadOutput = reader.ReadToEnd();
            Console.WriteLine("Policy load successfully output: '{0}'", policyLoadOutput);
        }

        // Check if this user has permission to get the value of variableId
        // That requires execute permissions on the variable

        // Instantiate a Variable object
        //               name - the name of the variable
        var conjurVariable = conjurClient.Variable(variableId);

        // Check if the current user has "execute" privilege required to get
        // the value of the variable
        //   parameters: privilege - string name of the privilege to check for
        try
        {
            var isAllowed = conjurVariable.Check("execute");
            if (!isAllowed)
            {
                Console.WriteLine("You do not have permissions to get the value of '{0}'", variableId);
            }
            else
            {
                conjurVariable.AddSecret("ExampleValue"u8.ToArray());

                var val = conjurVariable.GetValue();
                Console.WriteLine("'{0}' has the value: '{1}'", variableId, val);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Permission check failed. An exception occurred '{0}'", e);
        }

        // Create a host and get the apiKey 
        //   parameters: hostName - the name of the new Conjur host identity
        try
        {
            // Use a host factory token to create a host
            // This example assumes the host factory token was created through
            // the UI or CLI and passed to this application. Read more
            // about HostFactory on developer.conjur.net
            var hostname = $"exampleHost{DateTime.Now:yyyMMddHHmmss}"; 
            var host = conjurClient.CreateHost(hostname, token);
            Console.WriteLine("Created host: {0}, apiKey: {1}", host.Id, host.ApiKey);

            // now you can log in as the host
            conjurClient.Credential = host.Credential;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to create a host. An exception occurred '{0}'", e);
        }

    }

}
