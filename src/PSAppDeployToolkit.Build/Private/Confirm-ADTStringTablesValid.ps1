#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTStringTablesValid
#
#-----------------------------------------------------------------------------

function Confirm-ADTStringTablesValid
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Verify the formatting of all PowerShell script files within the repository.
        Write-ADTBuildLogEntry -Message "Confirming string translation files have the same keys as English, this may take awhile."
        $reference = Import-LocalizedData -BaseDirectory $Script:ModuleConstants.Paths.ModuleStrings -FileName strings.psd1
        foreach ($stringFile in (Get-ChildItem -LiteralPath $Script:ModuleConstants.Paths.ModuleStrings -Directory | Get-ChildItem -File))
        {
            Write-ADTBuildLogEntry -Message "Testing file [$($stringFile.FullName)]..."
            Confirm-ADTHashtableKeyEquality -Reference $reference -Comparison (Import-LocalizedData -BaseDirectory $stringFile.Directory.FullName -FileName $stringFile.Name)
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
