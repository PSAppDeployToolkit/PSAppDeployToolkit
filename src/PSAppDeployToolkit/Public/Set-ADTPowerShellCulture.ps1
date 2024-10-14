#-----------------------------------------------------------------------------
#
# MARK: Set-ADTPowerShellCulture
#
#-----------------------------------------------------------------------------

function Set-ADTPowerShellCulture
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Globalization.CultureInfo]$CultureInfo
    )

    begin
    {
        # Initialize function.
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $smaCultureResolver = [System.Reflection.Assembly]::Load('System.Management.Automation').GetType('Microsoft.PowerShell.NativeCultureResolver')
        $smaResolverFlags = [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static
        [System.Globalization.CultureInfo[]]$validCultures = (Get-WinUserLanguageList).LanguageTag
    }

    process
    {
        try
        {
            try
            {
                # Test that the specified culture is installed or not.
                if (!$validCultures.Contains($CultureInfo))
                {
                    $naerParams = @{
                        Exception = [System.ArgumentException]::new("The language pack for [$CultureInfo] is not installed on this system.", $CultureInfo)
                        Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                        ErrorId = 'CultureNotInstalled'
                        TargetObject = $validCultures
                        RecommendedAction = "Please review the installed cultures within this error's TargetObject and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Reflectively update the culture to the specified value.
                # This will change PowerShell, but not its default variables like $PSCulture and $PSUICulture.
                $smaCultureResolver.GetField('m_Culture', $smaResolverFlags).SetValue($null, $CultureInfo)
                $smaCultureResolver.GetField('m_uiCulture', $smaResolverFlags).SetValue($null, $CultureInfo)
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Object ensures the correct PositionMessage is used.
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
