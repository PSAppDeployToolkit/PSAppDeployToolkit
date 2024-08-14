function Get-ADTGuidRegexPattern
{
    <#

    .NOTES
    This function can be called without an active ADT session.

    #>

	return '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$'
}
