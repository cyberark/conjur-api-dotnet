# Conjur API for .NET

This is a *Draft* implementation of the .NET API for [Conjur](https://developer.conjur.net/).
This implementation includes an example that shows how to:

    - Authenticate
    - Check permissions to get the value of a variable
    - Get the value of a variable
    - Use a Host Factory token to create a new Host and get an apiKey to use with Conjur

## Building

This sample was built and tested with Visual Studio 2015. 

To load in Visual Studio, from the Visual Studio File menu select Open > Project/Solution > api-dotnet.sln and build the solution. This will create:

    - conjur-api.dll: the .NET version of the Conjur API.
    - ConjurTest.dll: test DLL used for automated testing of the Conjur .NET API
    - example.exe: sample application that uses the Conjur API.

Optionally, to build in a Docker container, it is recommended to use Mono and xbuild.

## Usage

To run the sample in Visual Studio, set the `example` project as the Startup Project.  To do so, in the Solution Explorer right click over `example` and select `Set as Startup Project`. 

```sh
Usage: Example  <applianceURL>
                <applianceCertificatePath>
                <username> 
                <password> 
                <variableId>
                <hostFactoryToken>
                
    applianceURL: the applianceURL including /api e.g. https://conjurmaster.myorg.com/api
    applianceCertificatePath: the path and name of the Conjur appliance certificate. The easiest way to get the certifiate is to use the Conjur CLI command `conjur init -h conjurmaster.myorg.com -f .conjurrc`. The certificate can be taken from any system you have run the Conjur CLI from.
    username: username of a user in Conjur
    password: password of a user in Conjur
    variableId: the name of an existing variable in Conjur that has a value set and the user has execute permissions for
    hostFactoryToken: a hostfactory token. The easiest way to test is to add a hostfactory to a layer using the Conjur CLI command `conjur hostfactory create` and `conjur hostfactory token create`.
```

## Example

```sh
using System;
using System.Net;
using System.Text;
using Conjur;

namespace Conjur.doc.DotNetExample
{
    class Program
    {
        // this example shows how to use the Conjur .NET api to
        // login, get a secret value, & check a permission
        // the credentials are passed as arguments.
        // Credentials are typically a hostId and api_key or
        // userId and password
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: Example <username> <password> <variableId>");
                return 1;
            }
            string username = args[0];
            string password = args[1];
            string variableId = args[2];

            // Instantiate a Conjur Client object.
            //  parameter: URI - conjur appliance URI (including /api)
            //  return: Client object - if URI is incorrect errors thrown when used
            Uri uri = new Uri("https://myorg.com/api");
            Client conjurClient = new Client(uri);

            // Login with Conjur credentials like userid and password,
            // or hostid and api_key, etc
            //  parameters: username - conjur user or host id for example
            //              password - conjur user password or host api key for example
            string conjurAuthToken = conjurClient.Login(username, password);

            // Check if this user has permission to get the value of variableId
            // That requires execute permissions on the variable

            // Instantiate a Variable object
            //   parameters: client - contains authentication token and conjur URI
            //               name - the name of the variable
            Variable conjurVariable = new Variable(conjurClient, variableId);

            // Check if the current user has "execute" privilege required to get
            // the value of the variable
            //   parameters: privilege - string name of the priv to check for
            bool isAllowed = conjurVariable.Check("execute");
            if (isAllowed)
            {
                Console.WriteLine("You do not have permissions to get the value of {0}", variableId);
            }
            else
            {
                Console.WriteLine("{0} has the value: {1}", variableId, conjurVariable.GetValue());
            }

            // Create a host and get the apiKey - this is needed to Conjurize a host
            // First create a host object
            //   parameters: client - contains the authentication token and conjur URI
            Host conjurHost = new Host(conjurClient);

            // Create the new host passing the name and group to own the host
            //               hostId - string name of the host
            //               asGroup - the owner of the host - do not leave yourself as the owner
            string hostData = conjurHost.Create(hostId, "security_admin");
            string apiKey = hostData.apiKey;

            Console.WriteLine("Created host: {0}, apiKey: {1}", hostId, apiKey);
        }
    }
}

```
