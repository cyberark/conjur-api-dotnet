// <copyright file="Constants.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Aggregation of API constants segregated into internal classes.
// </summary>

namespace Conjur;

public static class Constants
{
    public const string KIND_USER = "user";
    public const string KIND_HOST = "host";
    public const string KIND_LAYER = "layer";
    public const string KIND_GROUP = "group";
    public const string KIND_POLICY = "policy";
    public const string KIND_VARIABLE = "variable";
    public const string KIND_WEBSERVICE = "webservice";
    
    public const string K8S_JWT_PATH = "/var/run/secrets/kubernetes.io/serviceaccount/token";
}
