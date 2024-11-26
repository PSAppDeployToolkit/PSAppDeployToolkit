@{
    BalloonText = @{
        Complete = "færdig."
        Error = "fejlet."
        FastRetry = "ikke færdig."
        RestartRequired = "færdig. En genstart er nødvendig."
        Start = "startet."
    }
    BlockExecution = @{
        Message = "Opstart af denne applikation er midlertidigt blokeret da en installationsproces er under afvikling."
    }
    ClosePrompt = @{
        ButtonClose = "Luk Programmer"
        ButtonContinue = "Fortsæt"
        ButtonContinueTooltip = "Vælg kun `"Fortsæt`" efter at du har afsluttet de ovenfor nævnte programmer."
        ButtonDefer = "Udsæt"
        CountdownMessage = "BEMÆRK: Programmet/Programmerne vil automatisk blive lukket om:"
        Message = "Følgende programmer skal lukkes før installationen kan fortsætte.`n`nGem dit arbejde, luk programmerne og fortsæt. Alternativt kan du gemme dit arbejde og trykke på `"Luk Programmer`"."
    }
    DeferPrompt = @{
        Deadline = "Deadline:"
        ExpiryMessage = "Du kan vælge at udsætte installationen indtil udsættelsesperioden udløber:"
        RemainingDeferrals = "Udsættelser tilbage:"
        WarningMessage = "Når udsættelsesperioden udløber kan du ikke længere udsætte installationen."
        WelcomeMessage = "Følgende applikation vil nu blive installeret:"
    }
    DeploymentType = @{
        Install = "Installation"
        Repair = "Reparere"
        Uninstall = "Afinstallation"
    }
    DiskSpace = @{
        Message = "Du har ikke plads nok til at færdiggøre installationen af:`n{0}`n`nPlads krævet: {1}MB`nPlads tilgængelig: {2}MB`n`nVær venlig at frigøre nok diskplads før du fortsætter installationen."
    }
    Progress = @{
        MessageInstall = "Installation i gang. Vent venligst..."
        MessageInstallDetail = "Dette vindue lukker automatisk, når installationen er færdig."
        MessageRepair = "Reparere i gang. Vent venligst..."
        MessageRepairDetail = "Dette vindue lukkes automatisk, når reparationen er færdig."
        MessageUninstall = "Afinstallation i gang. Vent venligst..."
        MessageUninstallDetail = "Dette vindue lukkes automatisk, når afinstallationen er færdig."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimere"
        ButtonRestartNow = "Genstart Nu"
        Message = "For at færdiggøre installationen skal du genstarte din computer."
        MessageRestart = "Din computer vil automatisk blive genstartet når nedtællingen er færdig."
        MessageTime = "Du bør venligst gemme dit arbejde og genstarte indenfor det givne tidsrum."
        TimeRemaining = "Tid tilbage:"
        Title = "Genstart Nødvendig"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} vil automatisk fortsætte i:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - App {0}'
            DialogMessage = 'Gem venligst dit arbejde, før du fortsætter, da de følgende applikationer lukkes automatisk.'
            DialogMessageNoProcesses = 'Vælg Installer for at fortsætte med installationen. Hvis du har udsættelser tilbage, kan du også vælge at udskyde installationen.'
            ButtonDeferRemaining = 'forblive'
            ButtonLeftText = 'Udskyde'
            ButtonRightText = 'Luk apps og installer'
            ButtonRightTextNoProcesses = 'Installer'
        }
    }
}
