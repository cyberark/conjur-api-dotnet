# Conjur API for .NET

Programmatic .NET access to [Conjur](https://conjur.org) (for both Conjur Open Source and Enterprise).
This .NET SDK allows developers to build new apps in .NET that communicate with Conjur by
invoking our Conjur API to perform operations on stored data (add, retrieve, etc)

## Table of Contents

- [Using this Project With Conjur Open Source](#using-conjur-api-dotnet-with-conjur-open-source)
- [Requirements](#requirements)
- [Building](#building)
- [Methods](#methods)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## Using conjur-api-dotnet with Conjur Open Source

Are you using this project with [Conjur Open Source](https://github.com/cyberark/conjur)? Then we
**strongly** recommend choosing the version of this project to use from the latest [Conjur OSS
suite release](https://docs.conjur.org/Latest/en/Content/Overview/Conjur-OSS-Suite-Overview.html).
Conjur maintainers perform additional testing on the suite release versions to ensure
compatibility. When possible, upgrade your Conjur version to match the
[latest suite release](https://docs.conjur.org/Latest/en/Content/ReleaseNotes/ConjurOSS-suite-RN.htm);
when using integrations, choose the latest suite release that matches your Conjur version. For any
questions, please contact us on [Discourse](https://discuss.cyberarkcommons.org/c/conjur/5).

## Requirements

- Conjur Enterprise v10+ or Conjur Open Source v1+
- .NET 6.0 or later
- When using the **AWS Authenticator**, Conjur Enterprise v13+ or Conjur Cloud (Conjur OSS was not tested)

## Building

### Visual Studio

To load in Visual Studio, from the Visual Studio File menu select Open > Project/Solution > api-dotnet.sln
and build the solution. This will create:

- conjur-api.dll: the .NET version of the Conjur API.
- ConjurTest.dll: test DLL used for automated testing of the Conjur .NET API
- example.exe: sample application that uses the Conjur API.

### Docker

To build in a Docker container, run the following commands:

```bash
make -C docker
./build.sh
```

## Methods

### `Client`

#### `Client Client(uri, account)`

- Create new Conjur instance
  - `uri` - URI of the Conjur server. Example: `https://myconjur.org.com/api`
  - `account` - Name of the Conjur account

#### `void client.LogIn(string userName, string password)`

- Login to a Conjur user
  - `userName` - Username of Conjur user to login as
  - `password` - Password of user

#### `void client.TrustedCertificates.ImportPem (string certPath)`

- Add Conjur root certificate to system trust store
  - `certPath` = Path to cert

#### `void client.DisableCertCheck()`

- Disable SSL Cert check -- used when Conjur is configured with self-signed cert. Do not use in production.

#### `void client.EnableCertCheck()`

- Enable SSL Cert check -- Default is to perform cert check; this method is used if there is a need to disable and enable the cert check.

#### `client.Credential = new NetworkCredential(string userName, string apiKey)`

- To login with an API key, use it directly
  - `userName` - Username of user to login as
  - `apiKey` - API key of user/host/etc

#### `IEnumerable<Variable> client.ListVariables(string query = null)`

- Returns a list of variable objects
  - `query` - Additional query parameters (not required)

#### `uint client.CountVariables(string query = null)`

- Return count of Conjur variables conforming to the `query` parameter
  - `query` - Additional query parameters (not required)

#### `Host client.CreateHost(string name, string hostFactoryToken)`

- Creates a host using a host factory token
  - `name` - Name of the host to create
  - `hostFactoryToken` - Host factory token

#### `client.Authenticator = new Conjur.AWSIAMAuthenticator(Conjur.Client client, string Identity, string Authenticator, string roleArn = "", string ConjurAWSRegion = "us-east-1")`

- **REQUIREMENTS**: Conjur Enterprise v13+ or Conjur Cloud (Conjur OSS was not tested)
- Configure the client to use the AWS IAM Authenticator
  - Client must be instantiated with these attributes before instantiating the AWS authenticator:
    - `ApplianceUri`
    - `Account`
    - Example: `var client = new Conjur.Client(conjurApiUri, conjurAccount);`

### `Policy`

#### `Policy client.Policy(string policyName)`

- Create a Conjur policy object
  - `policyName` - Name of policy

#### `policy.LoadPolicy(Stream policyContent)`

- Load policy into Conjur
  - `policyContent` - The policy

### `Variable`

#### `Variable client.Variable(string name)`

- Instantiate a Variable object
  - `name` - Name of the variable

#### `Boolean variable.Check(string privilege)`

- Check if the current entity has the specified privilege on this variable
  - `privilege` - string name of the privilege to check for
    - Privileges: read, create, update, delete, execute

#### `void variable.AddSecret(bytes val)`

- Change current variable to val
  - `val` - Value in bytes to update current variable to

#### `String variable.GetValue()`

- Return the value of the current Variable

## Examples

### Example Code

```csharp
// Instantiate a Conjur Client object.
//  parameter: URI - conjur appliance URI
//  parameter: ACCOUNT - conjur account name
//  return: Client object - if URI is incorrect errors thrown when used
Client conjurClient = new Client("https://myorg.com", account);

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
if (!isAllowed)
{
    Console.WriteLine("You do not have permissions to get the value of {0}", variableId);
}
else
{
    Console.WriteLine("{0} has the value: {1}", variableId, conjurVariable.GetValue());
}
```

### Example App

This example app shows how to:

- Authenticate
- Load Policy
- Check permissions to get the value of a variable
- Get the value of a variable
- Use a Host Factory token to create a new Host and get an apiKey to use with Conjur

To run the sample in Visual Studio, set the `example` project as the Startup
 Project.  To do so, in
the Solution Explorer right click over `example` and select `Set as Startup Project`.

```txt
Usage: Example  <applianceURL>
                <applianceCertificatePath>
                <accountName>
                <username>
                <password>
                <variableId>
                <hostFactoryToken>
```

`applianceURL`: the applianceURL e.g. `https://conjur.myorg.com/`

`applianceCertificatePath`: the path and name of the Conjur appliance
 certificate. The easiest way to get the certifiate is to use the Conjur
CLI command `conjur init -u conjur.myorg.com -f .conjurrc`. The certificate can be taken from any system you have run the Conjur CLI from.

`accountName`: The name of the account in Conjur.

`username`: Username of a user in Conjur. Alternatively can be a hostname.

`password`: Password of a user in Conjur. Alternatively can be a host apiKey.

`variableId`: The name of an existing variable in Conjur that has a value set and for which the `username` has execute permissions.

`hostFactoryToken`: A host factory token. The easiest way to get a host
 factory token for testing is to add a hostfactory to a layer using
the Conjur CLI command `conjur hostfactory create` and
 `conjur hostfactory token create`. Take the token returned from that call
and pass it as the hostFactoryToken parameter to this example.

#### Example Code with AWS Authenticator

This example code shows how to configure and use the AWS authenticator.

Note:  The IAM role may need to have the `AssumeRole` permissions

```csharp
// Assuming the conjur api dotnet code is `git clone`d, then 
// add a reference to it in your project:
// dotnet add conjapp.csproj reference ../conjur-api-dotnet/conjur-api/
using Conjur;

namespace ConjurApp
{
    internal class Program
    {
        public static void Main()
        {
            string roleArnToAssume = "arn:aws:iam::12345:role/MyIAMRole";
            string variableId = "data/vault/myapplication/mypasswordtype/password";

            string conjurApiUri = "https://conjur.example/api";
            string conjurAccount = "conjur";
            string conjurIdentity = "host/data/myhost/12345/MyIAMRole";
            string conjurAuthenticator = "authn-iam/myauthenticatorname";

            Conjur.Client conjurClient = new Conjur.Client(conjurApiUri, conjurAccount);

            conjurClient.Authenticator = new Conjur.AWSIAMAuthenticator(
                conjurClient,
                conjurIdentity,
                conjurAuthenticator,
                roleArnToAssume);

            Conjur.Variable conjurVariable = conjurClient.Variable(variableId);
            var value = conjurVariable.GetValue();
            Console.WriteLine("Variable - {0} = {1}", variableId, value);
        }
    }
}
```

## Contributing

We welcome contributions of all kinds to this repository. For instructions on
 how to get started and descriptions
of our development workflows, please see our [contributing guide](https://github.com/cyberark/conjur-api-dotnet/blob/main/CONTRIBUTING.md).

## License

This repository is licensed under Apache License 2.0 - see [`LICENSE`](LICENSE) for more details.
