﻿using System.Reflection;
using System.Runtime.CompilerServices;

// Information about this assembly is defined by the following attributes.
// Change them to the values specific to your project.

[assembly: AssemblyTitle("FaunaDB.Client.Common")]
[assembly: AssemblyCompany("Fauna, Inc.")]
[assembly: AssemblyProduct("C# Driver for FaunaDB")]
[assembly: AssemblyDescription("C# Driver for FaunaDB")]
[assembly: AssemblyCopyright("© Fauna, Inc. 2017. Distributed under MPL 2.0 License")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: InternalsVisibleTo("FaunaDB.Client.Common.Test")]
[assembly: InternalsVisibleTo("FaunaDB.Client.Api.V2.Test")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: InternalsVisibleTo("FaunaDB.Client.Api.V2")]

// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
// The form "{Major}.{Minor}.*" will automatically update the build and revision,
// and "{Major}.{Minor}.{Build}.*" will update just the revision.

[assembly: AssemblyVersion(FaunaDBCommonAttribute.Version)]
[assembly: AssemblyFileVersion(FaunaDBCommonAttribute.Version)]
[assembly: AssemblyInformationalVersion(FaunaDBCommonAttribute.Version + "-alpha")]

// The following attributes are used to specify the signing key for the assembly,
// if desired. See the Mono documentation for more information about signing.

//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("")]

static class FaunaDBCommonAttribute
{
    public const string Version = "1.0.0";
}
