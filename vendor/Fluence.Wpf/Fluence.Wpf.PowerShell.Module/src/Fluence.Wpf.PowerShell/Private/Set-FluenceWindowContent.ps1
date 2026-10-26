<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


function Set-FluenceWindowContent
{
    <#
    .SYNOPSIS
        Builds the window for a specification and populates its content for the resolved mode.
    .DESCRIPTION
        For ContentBlock mode it builds a host window and invokes the user content block against it.
        For XamlString and XamlFile modes it parses or loads the XAML: a Window root is used directly
        (chrome and owner applied over it); any other root is hosted inside a new FluenceWindow. The
        optional Initialize block then runs against the window. The user block is resolved through
        Resolve-FluenceUserBlock so it runs live on a shared runspace or is recreated from text on the
        module-owned UI runspace. Any error the user block throws is captured on the shared state.
    .PARAMETER Spec
        The normalized window specification hashtable from Show-FluenceWindow.
    .PARAMETER State
        A shared @{ Error = $null } hashtable that carries any user-block error back to the caller.
    .OUTPUTS
        System.Windows.Window
    .NOTES
        Must run on a UI (STA) thread; call it through Show-FluenceWindowCore.
    #>
    [CmdletBinding()]
    [OutputType([System.Windows.Window])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds a WPF window and sets its content in memory; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec,

        [Parameter(Mandatory = $true)]
        [hashtable]$State
    )

    if ($Spec.Mode -eq 'ContentBlock')
    {
        $window = New-FluenceHostWindow -Spec $Spec
        $block = Resolve-FluenceUserBlock -LiveBlock $Spec.Content -BlockText $Spec.ContentText -CallerRunspaceId $Spec.CallerRunspaceId
        if ($null -ne $block)
        {
            try
            {
                $null = & $block $window $Spec.Data
            }
            catch
            {
                $State.Error = $_
            }
        }
        return $window
    }

    if ($Spec.Mode -eq 'XamlString')
    {
        $parsed = [System.Windows.Markup.XamlReader]::Parse($Spec.Xaml)
    }
    else
    {
        $stream = [System.IO.File]::OpenRead($Spec.XamlPath)
        try
        {
            $fileUri = [System.Uri]::new($Spec.XamlPath, [System.UriKind]::Absolute)
            $reader = [System.Xml.XmlReader]::Create(
                $stream,
                [System.Xml.XmlReaderSettings]::new(),
                $fileUri.AbsoluteUri)
            try
            {
                $parsed = [System.Windows.Markup.XamlReader]::Load($reader)
            }
            finally
            {
                $reader.Dispose()
            }
        }
        finally
        {
            $stream.Dispose()
        }
    }

    if ($parsed -is [System.Windows.Window])
    {
        $window = $parsed
        Set-FluenceWindowChrome -Window $window -ChromeBound $Spec.ChromeBound
        Set-FluenceWindowIcon -Window $window -IconSource $Spec.TitleBarIcon
        if ($null -ne $Spec.Owner)
        {
            # Same guard as New-FluenceHostWindow and New-FluenceDialogWindow: a WPF Owner must live
            # on the window's own thread, so on an MTA host a caller-thread Owner cannot parent a
            # window built on the module's UI runspace. Warn and show it unparented rather than
            # throwing a cross-thread InvalidOperationException from this one XAML-root path.
            if ($Spec.Owner.Dispatcher.CheckAccess())
            {
                $window.Owner = $Spec.Owner
                $window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::CenterOwner
            }
            else
            {
                Write-Warning "The -Owner window belongs to a different thread than the Fluence UI thread; modal owner parenting is not available here. Showing the window without an owner."
            }
        }
    }
    else
    {
        $window = New-FluenceHostWindow -Spec $Spec
        $window.Content = $parsed
    }

    $block = Resolve-FluenceUserBlock -LiveBlock $Spec.Initialize -BlockText $Spec.InitializeText -CallerRunspaceId $Spec.CallerRunspaceId
    if ($null -ne $block)
    {
        try
        {
            $null = & $block $window $Spec.Data
        }
        catch
        {
            $State.Error = $_
        }
    }
    return $window
}
