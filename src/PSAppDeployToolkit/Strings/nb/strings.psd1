@{
    BalloonText = @{
        Complete = "fullført."
        Error = "mislyktes."
        FastRetry = "ikke fullført."
        RestartRequired = "ferdig. En omstart er nødvendig."
        Start = "startet."
    }
    BlockExecution = @{
        Message = "Start av dette programmet er midlertidig blokkert inntil pågående programvareinstallasjon er fullført."
    }
    ClosePrompt = @{
        ButtonClose = "Lukk programmer"
        ButtonContinue = "Fortsett"
        ButtonContinueTooltip = "Velg kun `"Fortsett`" etter du har lukket applikasjonen(e) i listen over."
        ButtonDefer = "Utsett"
        CountdownMessage = "OBS: Programmet vil automatisk lukkes om:"
        Message = "Følgende programmer må lukkes før installasjonen kan fortsette.`n`nLagre arbeidet, lukk programmene og velg `"Fortsett`", eller velg `"Lukk programmer`" uten å lagre arbeidet."
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
        MessageInstall = "Installasjon av programvare pågår. Vennligst vent.."
        MessageInstallDetail = "Dette vinduet lukkes automatisk når installasjonen er fullført."
        MessageRepair = "Reparasjon av programvare pågår. Vennligst vent.."
        MessageRepairDetail = "Dette vinduet lukkes automatisk når reparasjonen er fullført."
        MessageUninstall = "Avinstallasjon av programvare pågår. Vennligst vent.."
        MessageUninstallDetail = "Dette vinduet lukkes automatisk når avinstallasjonen er fullført."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimere"
        ButtonRestartNow = "Omstart nå"
        Message = "En omstart av maskinen er nødvendig for å fullføre installasjonen."
        MessageRestart = "Maskinen vil automatisk starte på nytt, når nedtellingen er omme."
        MessageTime = "Lagre arbeidet ditt og ta en omstart av maskinen innen fristen."
        TimeRemaining = "Tid som gjenstår:"
        Title = "Omstart kreves"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} vil automatisk fortsette om:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - App {0}'
            DialogMessage = 'Lagre arbeidet ditt før du fortsetter, fordi de følgende programmene blir lukket automatisk.'
            DialogMessageNoProcesses = 'Velg Installere for å fortsette med installasjonen. Hvis du har noen utsettelser igjen, kan du også velge å utsette installasjonen.'
            ButtonDeferRemaining = 'gjenstår'
            ButtonLeftText = 'Utsette'
            ButtonRightText = 'Lukk apper og installer'
            ButtonRightTextNoProcesses = 'Installere'
        }
    }
}
