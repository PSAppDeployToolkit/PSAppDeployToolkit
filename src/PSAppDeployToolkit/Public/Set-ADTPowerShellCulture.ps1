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
        Set-ADTPowerShellCulture -Culture en-US

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

                # Update the culture to the specified value. Windows PowerShell exposes the internal
                # NativeCultureResolver fields used for culture resolution, and reflectively setting them
                # changes PowerShell without touching its default variables ($PSCulture/$PSUICulture).
                # PowerShell 7+ no longer exposes those fields, so fall back to setting the current
                # thread's culture directly (which on that runtime is what PowerShell resolves against).
                $mCultureField = $smaCultureResolver.GetField('m_Culture', $smaResolverFlags)
                $mUiCultureField = $smaCultureResolver.GetField('m_uiCulture', $smaResolverFlags)
                if ($mCultureField -and $mUiCultureField)
                {
                    $mCultureField.SetValue($null, $CultureInfo)
                    $mUiCultureField.SetValue($null, $CultureInfo)
                }
                else
                {
                    [System.Threading.Thread]::CurrentThread.CurrentCulture = $CultureInfo
                    [System.Threading.Thread]::CurrentThread.CurrentUICulture = $CultureInfo
                }
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
