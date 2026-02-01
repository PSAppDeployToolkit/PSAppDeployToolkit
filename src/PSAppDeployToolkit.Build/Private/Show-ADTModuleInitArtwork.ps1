#-----------------------------------------------------------------------------
#
# MARK: Show-ADTModuleInitArtwork
#
#-----------------------------------------------------------------------------

function Show-ADTModuleInitArtwork
{
    # Only draw artwork if the caller's not requesting any logos be displayed.
    if ([System.Environment]::GetCommandLineArgs() -contains '-NoLogo')
    {
        return
    }

    # If we fail to set the console's cursor visiblity, we're not in a console.
    try
    {
        $cursorState = [System.Console]::CursorVisible
        [System.Console]::CursorVisible = $false
    }
    catch
    {
        return
    }

    # Set the output encoding to UTF8 so our characters can render right.
    $previousOutputEncoding = [System.Console]::OutputEncoding
    [System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8

    # Output ANSI art to console and restore console. This uses the .ForEach() method to avoid introduced pipeline latency.
    $null = if ($Script:ModuleConstants.InitializationArtwork.Style.Equals('Raster'))
    {
        # Get each line.
        $Script:ModuleConstants.InitializationArtwork.Banner.Split("`n").Replace("`r", [System.Management.Automation.Language.NullString]::Value).ForEach({
                # Print each line's character, then place a line feed before continuing.
                $_.GetEnumerator().ForEach({
                        # Log the start time, write the character and spin until enough ticks have elapsed. 10000 ticks is 1 millisecond.
                        # Note: Halting the thread via Start-Sleep or [System.Threading.Thread]::Sleep() is not precise enough here.
                        $start = [System.DateTime]::Now
                        [System.Console]::Write($_)
                        while (([System.DateTime]::Now - $start).Ticks -lt 20000) {}
                    })
                [System.Console]::WriteLine()
            })
    }
    else
    {
        # Get each line and draw one-by-one.
        $Script:ModuleConstants.InitializationArtwork.Banner.Split("`n").Replace("`r", [System.Management.Automation.Language.NullString]::Value).ForEach({
                [System.Console]::WriteLine($_)
                [System.Threading.Thread]::Sleep(125)
            })
    }
    [System.Console]::CursorVisible = $cursorState

    # Draw subtitle.
    [System.Console]::WriteLine($Script:ModuleConstants.InitializationArtwork.Subtitle)
    [System.Console]::OutputEncoding = $previousOutputEncoding
}
