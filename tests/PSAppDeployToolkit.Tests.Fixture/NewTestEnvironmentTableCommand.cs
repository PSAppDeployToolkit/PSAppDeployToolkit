using System;
using System.Collections;
using System.Globalization;
using System.Management.Automation;
using PSAppDeployToolkit.Foundation;

namespace PSAppDeployToolkit.Tests.Fixture
{
    /// <summary>
    /// Builds an <see cref="EnvironmentTable"/> and writes it to the pipeline.
    /// </summary>
    /// <remarks>
    /// This exists because <see cref="EnvironmentTable"/> takes a <see cref="PSCmdlet"/> and reads
    /// <c>MyInvocation.MyCommand.Module</c> from it, which is populated only for a cmdlet running in a pipeline
    /// as part of a module. There is no way to hand it one from outside: <see cref="PSCmdlet.MyInvocation"/>
    /// comes from the engine's own invocation state, not from anything a caller can set. So the fixture asks
    /// PowerShell to run this, and PowerShell supplies the cmdlet.
    /// <para>
    /// It is the only part of the fixture that has to run inside a pipeline. Everything else - seating the
    /// module database, constructing a deployment session, writing log entries - happens on the test's own
    /// thread against a runspace it has adopted, which is why this writes the table out rather than doing any
    /// work with it.
    /// </para>
    /// </remarks>
    [Cmdlet(VerbsCommon.New, "TestEnvironmentTable")]
    [OutputType(typeof(EnvironmentTable))]
    public sealed class NewTestEnvironmentTableCommand : PSCmdlet
    {
        /// <summary>
        /// Builds the table and writes it out.
        /// </summary>
        protected override void EndProcessing()
        {
            // $PSVersionTable rather than a hand-built hashtable, because the table is what the module passes
            // and one of the values read out of it - PSVersion - is a SemanticVersion on PowerShell 7 rather
            // than a Version, which is a difference worth carrying into the test rather than papering over.
            Hashtable psVersionTable = (Hashtable)SessionState.PSVariable.GetValue("PSVersionTable");
            WriteObject(new EnvironmentTable(this, psVersionTable, GetPSVersion(psVersionTable)));
        }

        /// <summary>
        /// Reads the engine version out of the version table as a <see cref="Version"/>.
        /// </summary>
        /// <remarks>
        /// Windows PowerShell records a <see cref="Version"/> here. PowerShell 7 records a
        /// <c>SemanticVersion</c>, which is not a <see cref="Version"/> and does not convert to one, so its
        /// prerelease suffix is dropped and the numeric part is reparsed.
        /// </remarks>
        /// <param name="psVersionTable">The engine's version table.</param>
        /// <returns>The engine version.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the table carries no readable version.</exception>
        private static Version GetPSVersion(Hashtable psVersionTable)
        {
            object? value = psVersionTable["PSVersion"];
            while (value is PSObject psObject)
            {
                value = psObject.BaseObject;
            }
            if (value is Version version)
            {
                return version;
            }
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return Version.TryParse(text.Split('-')[0], out Version? parsed)
                ? parsed
                : throw new InvalidOperationException($"The engine's version table carries a PSVersion of [{text}], which is not a version.");
        }
    }
}
