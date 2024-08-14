function Get-ADTRunAsActiveUser
{
    # Determine the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
    # If a console user exists, then that will be the active user session.
    # If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that is either 'Active' or 'Connected' is the active user.
    $SessionInfoMember = if (Test-ADTIsMultiSessionOS) {'IsCurrentSession'} else {'IsActiveUserSession'}
    return [PSADT.QueryUser]::GetUserSessionInfo().Where({$_.NTAccount -and $_.$SessionInfoMember}, 'First', 1)
}
