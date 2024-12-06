@{
    BalloonText = @{
        Complete = "fullført."
        Error = "mislyktes."
        FastRetry = "ikke fullført."
        RestartRequired = "ferdig. En omstart er nødvendig."
        Start = "startet."
    }
    BlockExecution = @{
        Message = "Start av dette programmet er midlertidig blokkert til programvareinstallasjon er ferdig."
    }
    ClosePrompt = @{
        ButtonClose = "Lukk Programmer"
        ButtonContinue = "Fortsett"
        ButtonContinueTooltip = "Velg kun `"Fortsett`" etter du har lukket applikasjonen(e) oppført over."
        ButtonDefer = "Utsett"
        CountdownMessage = "OBS: Programmet vil automatisk lukkes om:"
        Message = "Følgende programmer må lukkes før installasjonen kan fortsette.`n`nLagre arbeidet, lukk programmene og velg `"Fortsett`" Eller velg `"Lukk Programmer`" uten å lagre."
    }
    DeferPrompt = @{
        Deadline = "Frist:"
        ExpiryMessage = "Du kan velge å utsette installasjonen et begrenset antall ganger inntil fristen utløper:"
        RemainingDeferrals = "Gjenstående utsettelser:"
        WarningMessage = "Når fristen har utløpt kan du ikke lenger utsette installasjonen."
        WelcomeMessage = "Følgende program vil bli installert:"
    }
    DeploymentType = @{
        Install = "Installasjon"
        Repair = "Reparasjon"
        Uninstall = "Avinstallasjon"
    }
    DiskSpace = @{
        Message = "Du har ikke nok diskplass for å fullføre installasjonen av:`n{0}`n`nLedig plass påkrevd: {1}MB`nLedig plass tilgjengelig: {2}MB`n`nFrigjør diskplass for å fortsette installasjonen."
    }
    Progress = @{
        MessageInstall = "Programvareinstallasjon pågår. Vennligst vent.."
        MessageInstallDetail = "Dette vinduet lukkes automatisk når installasjonen er fullført."
        MessageRepair = "ProgramvareReparasjon pågår. Vennligst vent.."
        MessageRepairDetail = "Dette vinduet lukkes automatisk når reparasjonen er fullført."
        MessageUninstall = "ProgramvareAvinstallasjon pågår. Vennligst vent.."
        MessageUninstallDetail = "Dette vinduet lukkes automatisk når avinstallasjonen er fullført."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimere"
        ButtonRestartNow = "Omstart Nå"
        Message = "En omstart av pcen er nødvendig for å fullføre installasjonen."
        MessageRestart = "Pcen vil automatisk starte på nytt, når nedtellingen er slutt."
        MessageTime = "Lagre arbeidet ditt og gjør en omstart av pc innen fristen."
        TimeRemaining = "Tid som gjenstår:"
        Title = "Omstart Kreves"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} vil automatisk fortsette i:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - App {0}'
            DialogMessage = 'Lagre arbeidet ditt før du fortsetter, da de følgende programmene lukkes automatisk.'
            DialogMessageNoProcesses = 'Velg Installer for å fortsette med installasjonen. Hvis du har noen utsettelser igjen, kan du også velge å utsette installasjonen.'
            ButtonDeferRemaining = 'forbli'
            ButtonLeftText = 'Utsette'
            ButtonRightText = 'Lukk apper og installer'
            ButtonRightTextNoProcesses = 'Installere'
        }
    }
}
