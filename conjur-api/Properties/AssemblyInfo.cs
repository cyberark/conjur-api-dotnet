// <copyright file="AssemblyInfo.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
// Assembly info.
// </summary>
using System.Reflection;
using System.Runtime.CompilerServices;
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if (!SIGNING)
[assembly: InternalsVisibleTo("ConjurTest")]
#endif
