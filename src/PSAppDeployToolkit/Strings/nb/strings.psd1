﻿@{
    BalloonTip = @{
        Start = @{
            Install = "Installasjon startet."
            Repair = "Reparasjon startet."
            Uninstall = "Avinstalleringen er startet."
        }
        Complete = @{
            Install = "Installasjon fullført."
            Repair = "Reparasjon fullført."
            Uninstall = "Avinstalleringen er fullført."
        }
        RestartRequired = @{
            Install = "Installasjonen er fullført. En omstart er nødvendig."
            Repair = "Reparasjon fullført. En omstart er påkrevd."
            Uninstall = "Avinstalleringen er fullført. En omstart er påkrevd."
        }
        FastRetry = @{
            Install = "Installasjon ikke fullført."
            Repair = "Reparasjon ikke fullført."
            Uninstall = "Avinstalleringen er ikke fullført."
        }
        Error = @{
            Install = "Installasjon mislyktes."
            Repair = "Reparasjon mislyktes."
            Uninstall = "Avinstalleringen mislyktes."
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'Start av denne applikasjonen er midlertidig blokkert slik at en installasjonsoperasjon kan fullføres.'
            Repair = 'Start av denne applikasjonen er midlertidig blokkert slik at en reparasjonsoperasjon kan fullføres.'
            Uninstall = 'Start av dette programmet har blitt midlertidig blokkert slik at en avinstallasjonsoperasjon kan fullføres.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Appinstallasjon'
            Repair = '{Toolkit\CompanyName} - Appreparasjon'
            Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Du har ikke nok diskplass til å fullføre installasjonen av:`n{0}`n`n`Plass kreves: {1}MB`nTilgjengelig plass: {2}MB`n`nFrigjør nok diskplass for å kunne fortsette med installasjonen."
            Repair = "Du har ikke nok diskplass til å fullføre reparasjonen av:`n{0}`n`nPlass kreves: {1}MB`nTilgjengelig plass: {2}MB`n`nFrigjør nok diskplass for å kunne fortsette med reparasjonen."
            Uninstall = "Du har ikke nok diskplass til å fullføre avinstalleringen av:`n{0}`n`nPlass kreves: {1}MB`nTilgjengelig plass: {2}MB`n`nFrigjør nok diskplass for å kunne fortsette med avinstallasjonen."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Appinstallasjon'
            Repair = '{Toolkit\CompanyName} - Appreparasjon'
            Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Installasjon pågår. Vennligst vent …'
            Repair = 'Reparasjon pågår. Vennligst vent…'
            Uninstall = 'Avinstalleringen pågår. Vennligst vent…'
        }
        MessageDetail = @{
            Install = 'Dette vinduet lukkes automatisk når installasjonen er fullført.'
            Repair = 'Dette vinduet lukkes automatisk når reparasjonen er fullført.'
            Uninstall = 'Dette vinduet lukkes automatisk når avinstallasjonen er fullført.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Appinstallasjon'
            Repair = '{Toolkit\CompanyName} - Appreparasjon'
            Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimer'
        ButtonRestartNow = 'Start på nytt nå'
        Message = @{
            Install = 'For at installasjonen skal fullføres, må du starte datamaskinen på nytt.'
            Repair = 'For at reparasjonen skal fullføres, må du starte datamaskinen på nytt.'
            Uninstall = 'Du må starte datamaskinen på nytt for at avinstallasjonen skal fullføres.'
        }
        CustomMessage = ''
        MessageRestart = 'Datamaskinen startes automatisk på nytt når nedtellingen er over.'
        MessageTime = 'Lagre arbeidet ditt og start på nytt innen den tilmålte tiden.'
        TimeRemaining = 'Gjenværende tid:'
        Title = 'Omstart påkrevd'
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Appinstallasjon'
            Repair = '{Toolkit\CompanyName} - Appreparasjon'
            Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'Følgende program er i ferd med å bli installert:'
                Repair = 'Følgende applikasjon er i ferd med å bli reparert:'
                Uninstall = 'Følgende applikasjon er i ferd med å bli avinstallert:'
            }
            CloseAppsMessage = @{
                Install = "Følgende programmer må lukkes før installasjonen kan fortsette.»`n`nLagre arbeidet ditt, lukk programmene og fortsett deretter. Alternativt kan du lagre arbeidet og klikke på «Lukk programmer»."
                Repair = "Følgende programmer må lukkes før reparasjonen kan fortsette.n`n`nLagre arbeidet, lukk programmene, og fortsett deretter. Alternativt kan du lagre arbeidet og klikke på «Lukk programmer»."
                Uninstall = "Følgende programmer må lukkes før avinstallasjonen kan fortsette.n`n`nLagre arbeidet, lukk programmene, og fortsett deretter. Alternativt kan du lagre arbeidet og klikke på «Lukk programmer»."
            }
            ExpiryMessage = @{
                Install = 'Du kan velge å utsette installasjonen til utsettelsen utløper:'
                Repair = 'Du kan velge å utsette reparasjonen til utsettelsen utløper:'
                Uninstall = 'Du kan velge å utsette avinstallasjonen til utsettelsen utløper:'
            }
            DeferralsRemaining = 'Gjenværende utsettelser:'
            DeferralDeadline = 'Frist:'
            ExpiryWarning = 'Når utsettelsen har utløpt, har du ikke lenger muligheten til å utsette.'
            CountdownDefer = @{
                Install = 'Installasjonen fortsetter automatisk om:'
                Repair = 'Reparasjonen vil automatisk fortsette i:'
                Uninstall = 'Avinstallasjonen vil automatisk fortsette om:'
            }
            CountdownClose = @{
                Install = 'MERK: Programmet/programmene lukkes automatisk om:'
                Repair = 'MERK: Programmet/programmene lukkes automatisk om:'
                Uninstall = 'MERK: Programmet/programmene lukkes automatisk om:'
            }
            ButtonClose = 'Lukk &Programmer'
            ButtonDefer = '&Utsette'
            ButtonContinue = '&Fortsett'
            ButtonContinueTooltip = 'Velg bare «Fortsett» etter at du har lukket ovennevnte program(mer).'
        }
        Fluent = @{
            DialogMessage = @{
                Install = 'Lagre arbeidet ditt før du fortsetter, da følgende programmer vil bli lukket automatisk.'
                Repair = 'Lagre arbeidet ditt før du fortsetter, da følgende programmer vil bli lukket automatisk.'
                Uninstall = 'Lagre arbeidet ditt før du fortsetter, da følgende programmer vil bli lukket automatisk.'
            }
            DialogMessageNoProcesses = @{
                Install = 'Velg Install for å fortsette med installasjonen.'
                Repair = 'Velg Repair for å fortsette med reparasjonen.'
                Uninstall = 'Velg Avinstaller for å fortsette med avinstallasjonen.'
            }
            AutomaticStartCountdown = 'Automatisk nedtelling til start'
            DeferralsRemaining = 'Gjenværende utsettelser'
            DeferralDeadline = 'Frist for utsettelse'
            ButtonLeftText = @{
                Install = 'Lukk apper og installer'
                Repair = 'Lukk apper og reparer'
                Uninstall = 'Lukk apper og avinstaller'
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Installere'
                Repair = 'Reparer'
                Uninstall = 'Avinstaller'
            }
            ButtonRightText = 'Utsett'
            Subtitle = @{
                Install = '{Toolkit\CompanyName} - Appinstallasjon'
                Repair = '{Toolkit\CompanyName} - Appreparasjon'
                Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
            }
        }
        CustomMessage = ''
    }
}
