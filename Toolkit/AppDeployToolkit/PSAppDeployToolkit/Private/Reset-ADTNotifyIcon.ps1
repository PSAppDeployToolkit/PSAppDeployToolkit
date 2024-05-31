function Reset-ADTNotifyIcon
{
    $null = if ($Script:FormData.NotifyIcon)
    {
        try
        {
            $Script:FormData.NotifyIcon.Dispose()
            $Script:FormData.NotifyIcon = $null
        }
        catch
        {
            $null
        }
    }
}
