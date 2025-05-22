@{
    BalloonTip = @{
        Start = @{
            Install = 'Installation startet.'
            Repair = 'Reparation startet.'
            Uninstall = 'Afinstallation startet.'
        }
        Complete = @{
            Install = 'Installation fuldført.'
            Repair = 'Reparation fuldført.'
            Uninstall = 'Afinstallation fuldført.'
        }
        RestartRequired = @{
            Install = 'Installationen er fuldført. En genstart er påkrævet.'
            Repair = 'Reparation fuldført. En genstart er påkrævet.'
            Uninstall = 'Afinstallation fuldført. En genstart er påkrævet.'
        }
        FastRetry = @{
            Install = 'Installation ikke fuldført.'
            Repair = 'Reparation ikke fuldført.'
            Uninstall = 'Afinstallation ikke fuldført.'
        }
        Error = @{
            Install = 'Installation mislykkedes.'
            Repair = 'Reparation mislykkedes.'
            Uninstall = 'Afinstallation mislykkedes.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'Start af denne applikation er midlertidigt blokeret, så en installation kan gennemføres.'
            Repair = 'Start af dette program er midlertidigt blokeret, så en reparation kan gennemføres.'
            Uninstall = 'Start af dette program er midlertidigt blokeret, så en afinstallationsoperation kan gennemføres.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation af App'
            Repair = '{Toolkit\CompanyName} - Reparation af App'
            Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Du har ikke nok diskplads til at fuldføre installationen af:`n{0}`n`nPladsbehov: {1}MB`nPlads til rådighed: {2}MB`n`nFrigør venligst nok diskplads til at fortsætte med installationen."
            Repair = "Du har ikke nok diskplads til at fuldføre reparationen af:`n{0}`n`nKrævet plads: {1}MB`nPlads til rådighed: {2}MB`n`nFrigør venligst nok diskplads til at kunne fortsætte med reparationen."
            Uninstall = "Du har ikke nok diskplads til at fuldføre afinstallationen af:`n{0}`n`nPladsbehov: {1}MB`nPlads til rådighed: {2}MB`n`nFrigør venligst nok diskplads til at fortsætte med afinstallationen."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation af App'
            Repair = '{Toolkit\CompanyName} - Reparation af App'
            Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Installation i gang. Vent venligst...'
            Repair = 'Reparation i gang. Vent venligst...'
            Uninstall = 'Afinstallation i gang. Vent venligst...'
        }
        MessageDetail = @{
            Install = 'Dette vindue lukkes automatisk, når installationen er færdig.'
            Repair = 'Dette vindue lukkes automatisk, når reparationen er færdig.'
            Uninstall = 'Dette vindue lukkes automatisk, når afinstallationen er færdig.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation af App'
            Repair = '{Toolkit\CompanyName} - Reparation af App'
            Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimér'
        ButtonRestartNow = 'Genstart nu'
        Message = @{
            Install = 'For at installationen kan gennemføres, skal du genstarte din computer.'
            Repair = 'For at reparationen kan gennemføres, skal du genstarte din computer.'
            Uninstall = 'For at afinstallationen kan gennemføres, skal du genstarte computeren.'
        }
        CustomMessage = ''
        MessageRestart = 'Din computer genstartes automatisk, når nedtællingen er slut.'
        MessageTime = 'Gem venligst dit arbejde, og genstart inden for den tildelte tid.'
        TimeRemaining = 'Resterende tid:'
        Title = 'Genstart påkrævet'
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation af App'
            Repair = '{Toolkit\CompanyName} - Reparation af App'
            Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'Følgende program er ved at blive installeret:'
                Repair = 'Følgende applikation er ved at blive repareret:'
                Uninstall = 'Følgende applikation er ved at blive afinstalleret:'
            }
            CloseAppsMessage = @{
                Install = 'Følgende programmer skal lukkes, før installationen kan fortsætte.« Gem venligst dit arbejde, luk programmerne, og fortsæt derefter. Alternativt kan du gemme dit arbejde og klikke på »Luk programmer«.'
                Reparation = 'Følgende programmer skal lukkes, før reparationen kan fortsætte.« Gem venligst dit arbejde, luk programmerne, og fortsæt derefter. Alternativt kan du gemme dit arbejde og klikke på »Luk programmer«.'
                Uninstall = 'Følgende programmer skal lukkes, før afinstallationen kan fortsætte.« Gem venligst dit arbejde, luk programmerne, og fortsæt derefter. Alternativt kan du gemme dit arbejde og klikke på »Luk programmer«.'
            }
            ExpiryMessage = @{
                Install = 'Du kan vælge at udskyde installationen, indtil udskydelsen udløber:'
                Repair = 'Du kan vælge at udskyde reparationen, indtil udskydelsen udløber:'
                Uninstall = 'Du kan vælge at udskyde afinstallationen, indtil udsættelsen udløber:'
            }
            DeferralsRemaining = 'Resterende udsættelser:'
            DeferralDeadline = 'Tidsfrist:'
            WarningMessage = 'Når udsættelsen er udløbet, har du ikke længere mulighed for at udskyde.'
            CountdownDefer = @{
                Install = 'Installationen fortsætter automatisk om:'
                Repair = 'Reparationen fortsætter automatisk i:'
                Uninstall = 'Afinstallationen fortsætter automatisk om:'
            }
            CountdownClose = 'BEMÆRK: Programmet/programmerne lukkes automatisk i:'
            ButtonClose = 'Luk &Programmer'
            ButtonDefer = '&Udskyde'
            ButtonContinue = '&Fortsæt'
            ButtonContinueTooltip = 'Vælg kun »Fortsæt«, når du har lukket ovenstående program(mer).'
        }
        Fluent = @{
            DialogMessage = 'Gem venligst dit arbejde, før du fortsætter, da de følgende programmer lukkes automatisk.'
            DialogMessageNoProcesses = @{
                Install = 'Vælg Install for at fortsætte med installationen. Hvis du har udsættelser tilbage, kan du også vælge at udskyde installationen.'
                Repair = 'Vælg Repair for at fortsætte med reparationen. Hvis du har udsættelser tilbage, kan du også vælge at udskyde reparationen.'
                Uninstall = 'Vælg Afinstallation for at fortsætte med afinstallationen. Hvis du har udsættelser tilbage, kan du også vælge at udskyde afinstallationen.'
            }
            AutomaticStartCountdown = 'Automatisk startnedtælling'
            DeferralsRemaining = 'Resterende udsættelser'
            DeferralDeadline = 'Udsættelsesfrist'
            ButtonLeftText = @{
                Install = 'Luk Apps og Installer'
                Repair = 'Luk Apps og Reparer'
                Uninstall = 'Luk Apps og Afinstaller'
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Installer'
                Repair = 'Reparation'
                Uninstall = 'Afinstaller'
            }
            ButtonRightText = 'Udskyd'
            Subtitle = @{
                Install = '{Toolkit\CompanyName} - Installation af App'
                Repair = '{Toolkit\CompanyName} - Reparation af App'
                Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
            }
        }
        CustomMessage = ''
    }
}
