# Conjur API for .NET

This is a *Draft* implementation of the .NET API for [V5 Conjur](https://developer.conjur.net/).
This implementation includes an example that shows how to:

    - Authenticate
    - Load Policy
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

### `Policy`

#### `Policy client.Policy(string policyName)`
- Create a Conjur policy object 
   - `policyName` - Name of policy

#### `policy.LoadPolicy(Stream policyContent)`
- Load policy into Conjur
   -  `policyContent` - The policy

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

## Usage

To run the sample in Visual Studio, set the `example` project as the Startup Project.  To do so, in the Solution Explorer right click over `example` and select `Set as Startup Project`.

```sh
Usage: Example  <applianceURL>
                <applianceCertificatePath>
                <accountName>
                <username>
                <password>
                <variableId>
                <hostFactoryToken>
```

`applianceURL`: the applianceURL e.g. `https://conjurmaster.myorg.com/`

`applianceCertificatePath`: the path and name of the Conjur appliance certificate. The easiest way to get the certifiate is to use the Conjur CLI command `conjur init -u conjurmaster.myorg.com -f .conjurrc`. The certificate can be taken from any system you have run the Conjur CLI from.

`accountName`: The name of the account in Conjur.

`username`: Username of a user in Conjur. Alternatively can be a hostname.

`password`: Password of a user in Conjur. Alternatively can be a host apiKey.

`variableId`: The name of an existing variable in Conjur that has a value set and for which the `username` has execute permissions.

`hostFactoryToken`: A host factory token. The easiest way to get a host factory token for testing is to add a hostfactory to a layer using the Conjur CLI command `conjur hostfactory create` and `conjur hostfactory token create`. Take the token returned from that call and pass it as the hostFactoryToken parameter to this example.

## Example

```sh
    // Instantiate a Conjur Client object.
    //  parameter: URI - conjur appliance URI
    //  parameter: ACCOUNT - conjur account name
    //  return: Client object - if URI is incorrect errors thrown when used
    Uri uri = new Uri("https://myorg.com");
    Client conjurClient = new Client(uri, account);

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

## License

This repository is licensed under Apache License 2.0 - see [`LICENSE`](LICENSE) for more details.
