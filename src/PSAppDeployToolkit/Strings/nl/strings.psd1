@{
    BalloonText = @{
        Complete = "voltooid"
        Error = "gefaald"
        FastRetry = "onvolledig"
        RestartRequired = "voltooid. Een herstart is nodig"
        Start = "gestart"
    }
    BlockExecution = @{
        Message = "Het opstarten van deze applicatie werd tijdelijk geblokkeerd om een installatie uit te voeren."
    }
    ClosePrompt = @{
        ButtonClose = "Sluit Applicaties"
        ButtonContinue = "Doorgaan"
        ButtonContinueTooltip = "Selecteer alleen 'Doorgaan' na het sluiten van de bovenstaande toepassing(en)."
        ButtonDefer = "Uitstel"
        CountdownMessage = "LET OP: De applicatie(s) worden afgesloten over:"
        Message = "De volgende applicaties moeten afgesloten worden om de installatie te voltooien.`n`nSla je werk op, sluit de applicaties, en ga verder.`nOf, sla je werk op en klik op 'Sluit Applicaties'."
    }
    DeferPrompt = @{
        Deadline = "Deadline:"
        ExpiryMessage = "Je kan de installatie uitstellen tot de maximale uitsteltermijn is verstreken:"
        RemainingDeferrals = "Aantal keer uitstellen:"
        WarningMessage = "Na verstrijken van de uitsteltermijn is deze optie niet langer beschikbaar."
        WelcomeMessage = "De volgende applicatie wordt zometeen geïnstalleerd:"
    }
    DeploymentType = @{
        Install = "Installatie"
        Repair = "Reparatie"
        Uninstall = "Verwijderen"
    }
    DiskSpace = @{
        Message = "Er is onvoldoende schijfruimte voor de installatie van:`n{0}`n`nRuimte nodig: {1}MB`nRuimte beschikbaar: {2}MB`n`nGelieve voldoende schijfruimte vrij te maken om de installatie te starten."
    }
    Progress = @{
        MessageInstall = "Installatie bezig. Even geduld..."
        MessageInstallDetail = "Dit venster wordt automatisch gesloten wanneer de installatie voltooid is."
        MessageRepair = "Reparatie bezig. Even geduld..."
        MessageRepairDetail = "Dit venster sluit automatisch wanneer de reparatie is voltooid."
        MessageUninstall = "Verwijderen bezig. Even geduld..."
        MessageUninstallDetail = "Dit venster wordt automatisch gesloten als de de-installatie voltooid is."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimaliseren"
        ButtonRestartNow = "Herstart Nu"
        Message = "Om de installatie te voltooien is een herstart nodig."
        MessageRestart = "De computer zal herstarten als de teller op nul staat"
        MessageTime = "Gelieve je werk op te slaan en binnen de toegestane termijn de computer herstarten"
        TimeRemaining = "Resterende tijd:"
        Title = "Herstart nodig"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "De {0} gaat automatisch door over:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - App {0}'
            DialogMessage = "Sla je werk op voordat je verdergaat, want de volgende programma's worden automatisch afgesloten."
            DialogMessageNoProcesses = 'Selecteer Installeren om door te gaan met de installatie. Als je nog uitstel hebt, kun je er ook voor kiezen om de installatie uit te stellen.'
            ButtonDeferRemaining = 'resterend'
            ButtonLeftText = 'Uitstellen'
            ButtonRightText = 'Apps sluiten en installeren'
            ButtonRightTextNoProcesses = 'Installeren'
        }
    }
}
