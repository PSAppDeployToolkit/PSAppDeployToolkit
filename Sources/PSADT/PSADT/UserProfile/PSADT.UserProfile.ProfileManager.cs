using System;
using System.IO;
using System.Linq;
using PSADT.Account;
using System.Security;
using System.Collections.Generic;

namespace PSADT.UserProfile
{
    public class ProfileManager
    {
        private const string ProfileListSubKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";
        private const string LocalSystemSid = "S-1-5-18";
        private const string LocalServiceSid = "S-1-5-19";
        private const string NetworkServiceSid = "S-1-5-20";

        /// <summary>
        /// Retrieves a list of user profiles from the registry based on the provided filtering criteria.
        /// </summary>
        /// <param name="includeNtAccount">A list of NT accounts to include. If null, all accounts are included.</param>
        /// <param name="excludeNtDomain">A list of NT domains to exclude. If null, no domains are excluded.</param>
        /// <param name="excludeUserName">A list of usernames to exclude. If null, no usernames are excluded.</param>
        /// <param name="isExcludeSystemProfiles">Specifies whether to exclude system profiles (LocalSystem, LocalService, NetworkService).</param>
        /// <returns>A list of <see cref="Profile"/> objects representing the discovered user profiles.</returns>
        /// <exception cref="SecurityException">Thrown when there is a security error while accessing the registry.</exception>
        /// <exception cref="IOException">Thrown when there is an IO error while accessing the registry.</exception>
        /// <exception cref="Exception">Thrown when the registry key cannot be accessed or other unexpected errors occur.</exception>
        public static List<Profile> GetUserProfile(
            List<string>? includeNtAccount = null,
            List<string>? excludeNtDomain = null,
            List<string>? excludeUserName = null,
            bool isExcludeSystemProfiles = true)
        {
            var userProfileInfos = new List<Profile>();
            var profileSidList = new List<string>();

            try
            {
                using var profileListKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(ProfileListSubKey)
                    ?? throw new InvalidOperationException($"Unable to get user profile information from the registry because {ProfileListSubKey} does not exist.");
                profileSidList.AddRange(profileListKey.GetSubKeyNames());

                if (isExcludeSystemProfiles)
                {
                    profileSidList.Remove(LocalSystemSid);
                    profileSidList.Remove(LocalServiceSid);
                    profileSidList.Remove(NetworkServiceSid);
                }

                foreach (var profileSid in profileSidList)
                {
                    var profileInfo = new Profile();
                    string? ntAccount = null;
                    string? ntDomain = null;
                    string? userName = null;

                    try
                    {
                        ntAccount = AccountUtilities.GetNTAccountFromSidString(profileSid).ToString();
                        ntDomain = AccountUtilities.ParseNTDomainFromNTAccountString(ntAccount);
                        userName = AccountUtilities.ParseUserNameFromNTAccountString(ntAccount);

                        if (includeNtAccount != null && !includeNtAccount.Contains(ntAccount, StringComparer.OrdinalIgnoreCase) ||
                            excludeUserName != null && excludeUserName.Contains(userName, StringComparer.OrdinalIgnoreCase) ||
                            excludeNtDomain != null && excludeNtDomain.Contains(ntDomain, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        using var profileSidKey = profileListKey.OpenSubKey(profileSid);
                        if (profileSidKey != null && profileSidKey.GetValue("ProfileImagePath") is string profilePath)
                        {
                            profileInfo.Path = profilePath;
                            profileInfo.NTAccount = ntAccount ?? string.Empty;
                            profileInfo.NTDomain = ntDomain ?? string.Empty;
                            profileInfo.UserName = userName ?? string.Empty;
                            profileInfo.Sid = profileSid;
                            // Invalid to assume these paths, e.g. they may be underneath OneDrive or redirected to a network share
                            // profileInfo.DocumentsPath = Path.Combine(profilePath, "Documents");
                            // profileInfo.DesktopPath = Path.Combine(profilePath, "Desktop");
                            // profileInfo.PicturesPath = Path.Combine(profilePath, "Pictures");

                            userProfileInfos.Add(profileInfo);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Profile SID key {profileSid} does not contain a valid 'ProfileImagePath'.");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error processing profile SID {profileSid}.", ex);
                    }
                }
            }
            catch (SecurityException ex)
            {
                throw new SecurityException("Security error while accessing the registry.", ex);
            }
            catch (IOException ex)
            {
                throw new IOException("IO error while accessing the registry.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to discover user profile information.", ex);
            }

            return userProfileInfos;
        }
    }
}
