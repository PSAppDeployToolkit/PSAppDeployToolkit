function Initialize-FluenceStaRunspace
{
    <#
    .SYNOPSIS
        Returns the module-owned STA runspace, adopting a process-wide one or creating it, with the module imported.
    .DESCRIPTION
        The runspace persists for the process, not for one module instance. WPF allows exactly one
        Application per AppDomain for the life of the process, and the module creates that Application
        on this runspace's thread on an MTA host, so a later Import-Module -Force (a test run, a
        development loop) must reuse the same thread rather than start a second one. The open runspace
        is therefore published in AppDomain data; a new module instance adopts it and re-imports the
        module into it so the runspace runs the current code. A failed bootstrap import is torn down
        rather than left as an Opened-but-empty runspace that would fail every later call.
    .OUTPUTS
        System.Management.Automation.Runspaces.Runspace
    .NOTES
        Does not require a host application. Used on MTA hosts only.
    #>
    [CmdletBinding()]
    [OutputType([System.Management.Automation.Runspaces.Runspace])]
    param()

    if ($null -ne $script:StaRunspace -and $script:StaRunspace.RunspaceStateInfo.State -eq 'Opened')
    {
        return $script:StaRunspace
    }

    $shared = [System.AppDomain]::CurrentDomain.GetData($script:StaRunspaceSlot)
    $adopted = $false
    if ($shared -is [System.Management.Automation.Runspaces.Runspace] -and $shared.RunspaceStateInfo.State -eq 'Opened')
    {
        $script:StaRunspace = $shared
        $script:OwnsApplication = $true
        $adopted = $true
        Write-Verbose 'Adopted the process-wide Fluence UI runspace.'
    }
    else
    {
        $script:StaRunspace = [runspacefactory]::CreateRunspace()
        $script:StaRunspace.ApartmentState = 'STA'
        $script:StaRunspace.ThreadOptions = 'ReuseThread'
        $script:StaRunspace.Open()
    }

    if ($script:StaRunspace.RunspaceAvailability -ne 'Available')
    {
        $script:StaRunspace = $null
        throw 'The process-wide Fluence UI runspace is still busy. Close its dialog or allow the active callback to finish before reimporting the module.'
    }

    $bootstrap = [powershell]::Create()
    $bootstrap.Runspace = $script:StaRunspace
    $bootstrapFailed = $false
    $bootstrapError = $null
    try
    {
        $null = $bootstrap.AddCommand('Import-Module').AddParameter('Name', $script:ModuleManifestPath).AddParameter('Force').Invoke()
        $bootstrapFailed = $bootstrap.HadErrors
        if ($bootstrap.Streams.Error.Count -gt 0)
        {
            $bootstrapFailed = $true
            $bootstrapError = $bootstrap.Streams.Error[0]
        }
    }
    catch
    {
        $bootstrapFailed = $true
        $bootstrapError = $_
    }
    finally
    {
        $bootstrap.Dispose()
    }

    if ($bootstrapFailed)
    {
        if (-not $adopted)
        {
            $script:StaRunspace.Dispose()
        }
        $script:StaRunspace = $null
        if ($null -ne $bootstrapError)
        {
            throw $bootstrapError
        }
        throw 'Failed to import the Fluence.Wpf.PowerShell module into the STA runspace.'
    }

    [System.AppDomain]::CurrentDomain.SetData($script:StaRunspaceSlot, $script:StaRunspace)
    Register-FluenceRunspaceExit
    return $script:StaRunspace
}
