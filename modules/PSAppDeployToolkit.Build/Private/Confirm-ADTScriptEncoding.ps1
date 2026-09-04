#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTScriptEncoding
#
#-----------------------------------------------------------------------------

function Confirm-ADTScriptEncoding
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    $bom = [System.Byte[]]::new(4)
    try
    {
        Write-ADTBuildLogEntry -Message "Confirming all PowerShell files are encoded using UTF8-BOM."
        foreach ($scriptFile in (Get-ChildItem -Path "$($Script:ModuleConstants.Paths.SourceRoot)\*.ps*1" -Recurse -File))
        {
            # Open the file, read out the first 4 bytes, then close it out.
            Write-ADTBuildLogEntry -Message "Testing file [$($scriptFile.FullName)]."
            $stream = [System.IO.FileStream]::new($scriptFile.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
            $null = $stream.Read($bom, 0, $bom.Count)
            $stream.Flush(); $stream.Close()

            # Throw if the byte order mark doesn't match utf8-bom.
            if (!(($bom[0] -eq 0xEF) -and ($bom[1] -eq 0xBB) -and ($bom[2] -eq 0xBF)))
            {
                throw "The file encoding for [$($scriptFile.FullName)] is not UTF-8 with BOM."
            }
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
