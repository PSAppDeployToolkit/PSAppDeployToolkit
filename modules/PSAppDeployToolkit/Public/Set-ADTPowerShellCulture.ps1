#-----------------------------------------------------------------------------
#
# MARK: Set-ADTPowerShellCulture
#
#-----------------------------------------------------------------------------

function Set-ADTPowerShellCulture
{
    <#
    .SYNOPSIS
        Changes the current thread's Culture and UICulture to the specified culture.

    .DESCRIPTION
        The `Set-ADTPowerShellCulture` function changes the current thread's Culture and UICulture to the specified culture.

    .PARAMETER CultureInfo
        The culture to set the current thread's Culture and UICulture to. Can be a CultureInfo object, or any valid IETF BCP 47 language tag.

    .EXAMPLE
        Set-ADTPowerShellCulture -CultureInfo en-US

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTPowerShellCulture

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Set-ADTPowerShellCulture.ps1
    #>

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
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $smaResolverFlags = [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static
        $smaCultureFields = [System.Reflection.Assembly]::Load('System.Management.Automation').GetType('Microsoft.PowerShell.NativeCultureResolver').GetFields($smaResolverFlags).Where({ $_.Name -match '^m_(ui)?Culture$' })
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

                # Update the culture on the thread, which is what PowerShell's culture resolver defers to.
                [System.Threading.Thread]::CurrentThread.CurrentCulture = $CultureInfo
                [System.Threading.Thread]::CurrentThread.CurrentUICulture = $CultureInfo

                # Releases that cache the culture in the resolver's own fields need those set as well,
                # otherwise the thread's value is never consulted again for the life of the process.
                $smaCultureFields | & { process { $_.SetValue($null, $CultureInfo) } }
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
