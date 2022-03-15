﻿// <copyright file="AssemblyInfo.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
// Assembly info.
// </summary>
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Conjur.dll")]
[assembly: AssemblyDescription("Conjur server API library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("CyberArk Software Ltd.")]
[assembly: AssemblyProduct("Conjur .NET API")]
[assembly: AssemblyCopyright("(c) CyberArk Software Ltd.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

/// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
/// The form "{Major}.{Minor}.*" will automatically update the build and revision,
/// and "{Major}.{Minor}.{Build}.*" will update just the revision.
[assembly: AssemblyVersion("2.1.*")]

#if (!SIGNING)
[assembly: InternalsVisibleTo("ConjurTest")]
#endif
