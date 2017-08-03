﻿using System;
using System.Net;
using Conjur;

namespace Example
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
            if (args.Length < 6)
            {
                Console.WriteLine("Usage: Example <applianceHostName> <applianceCertificatePath> <accountName> <username> <password> <variableId>");
                return;
            }
            string applianceName = args[0];
            string certPath = args[1];
            string account = args[2];
            string username = args[3];
            string password = args[4];
            string variableId = args[5];

            // Instantiate a Conjur Client object.
            //  parameter: applianceUri - conjur appliance URI
            //  return: Client object - if URI is incorrect errors thrown when used
            string uri = String.Format("https://{0}", applianceName);
            var conjurClient = new Client(uri, account);

            // If the Conjur root certificate is not in the system trust store,
            // add it as trusted explicitly
            if (certPath.Length > 0)
                conjurClient.TrustedCertificates.ImportPem(certPath);

            // Login with Conjur userid and password,
            // or hostid and api_key, etc
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

                // to log in with an API key use it directly, ie.
                var apiKey = password;
                conjurClient.Credential = new NetworkCredential(username, apiKey);
            }
            // Check if this user has permission to get the value of variableId
            // That requires exectue permissions on the variable

            // Instantiate a Variable object
            //               name - the name of the variable
            var conjurVariable = conjurClient.Variable(variableId);

            // Check if the current user has "execute" privilege required to get
            // the value of the variable
            //   parameters: privilege - string name of the priv to check for
            try
            {
                bool isAllowed = conjurVariable.Check("execute");
                if (!isAllowed)
                {
                    Console.WriteLine("You do not have permissions to get the value of '{0}'", variableId);
                }
                else
                {
                    string value = conjurVariable.GetValue();
                    Console.WriteLine("'{0}' has the value: '{1}'", variableId, value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Permission check failed. An exception occurred '{0}'", e);
            }

        }
    }
}
