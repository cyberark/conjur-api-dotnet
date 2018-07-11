// <copyright file="AssemblyInfo.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
// Assembly info.
// </summary>
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Conjur.dll")]
[assembly: AssemblyDescription("Conjur server API library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Conjur Inc.")]
[assembly: AssemblyProduct("Conjur .NET API")]
[assembly: AssemblyCopyright("(c) Conjur Inc.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

/// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
/// The form "{Major}.{Minor}.*" will automatically update the build and revision,
/// and "{Major}.{Minor}.{Build}.*" will update just the revision.
[assembly: AssemblyVersion("1.2.*")]

#if (!SIGNING)
[assembly: InternalsVisibleTo("ConjurTest")]
#endif
