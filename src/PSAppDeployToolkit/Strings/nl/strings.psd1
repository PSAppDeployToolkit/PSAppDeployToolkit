@{
    BalloonTip = @{
        Start = @{
            Install = 'Installatie gestart.'
            Repair = 'Reparatie gestart.'
            Uninstall = 'De-installatie gestart.'
        }
        Complete = @{
            Install = 'Installatie voltooid.'
            Repair = 'Reparatie voltooid.'
            Uninstall = 'De-installatie voltooid.'
        }
        RestartRequired = @{
            Install = 'Installatie voltooid. Opnieuw opstarten is vereist.'
            Repair = 'Reparatie voltooid. Opnieuw opstarten is vereist.'
            Uninstall = 'De-installatie voltooid. Opnieuw opstarten is vereist.'
        }
        FastRetry = @{
            Install = 'Installatie niet voltooid.'
            Repair = 'Reparatie niet voltooid.'
            Uninstall = 'De-installatie niet voltooid.'
        }
        Error = @{
            Install = 'Installatie mislukt.'
            Repair = 'Reparatie mislukt.'
            Uninstall = 'De-installatie mislukt.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = "Het starten van deze applicatie is tijdelijk geblokkeerd zodat een installatie kan worden uitgevoerd."
            Repair = "Het starten van deze applicatie is tijdelijk geblokkeerd zodat een reparatie kan worden uitgevoerd."
            Uninstall = "Het starten van deze applicatie is tijdelijk geblokkeerd zodat een de-installatie kan worden uitgevoerd."
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - App Installatie'
            Repair = '{Toolkit\CompanyName} - App Reparatie'
            Uninstall = '{Toolkit\CompanyName} - App De-installatie'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "U hebt niet genoeg schijfruimte om de installatie te voltooien van:`n{0}`n`nAfgeronde ruimte: {1}MB`nRuimte beschikbaar: {2}MB`n`nMaak voldoende schijfruimte vrij om door te gaan met de installatie."
            Repair = "U hebt niet genoeg schijfruimte om de reparatie te voltooien van:`n{0}`n`nVerplichte ruimte: {1}MB`nBeschikbare ruimte: {2}MB`n`nMaak voldoende schijfruimte vrij om door te gaan met de reparatie."
            Uninstall = "U hebt niet genoeg schijfruimte om de de-installatie te voltooien van:`n{0}`n`nAfgeronde ruimte: {1}MB`nBeschikbare ruimte: {2}MB`n`nMaak alstublieft voldoende schijfruimte vrij om door te gaan met de de-installatie."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - App Installatie'
            Repair = '{Toolkit\CompanyName} - App Reparatie'
            Uninstall = '{Toolkit\CompanyName} - App De-installatie'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Installatie wordt uitgevoerd. Even geduld a.u.b…'
            Repair = 'Reparatie wordt uitgevoerd. Even geduld a.u.b…'
            Uninstall = 'De-installatie wordt uitgevoerd. Even geduld a.u.b…'
        }
        MessageDetail = @{
            Install = 'Dit venster wordt automatisch gesloten als de installatie voltooid is.'
            Repair = 'Dit venster wordt automatisch gesloten als de reparatie is voltooid.'
            Uninstall = 'Dit venster wordt automatisch gesloten als de de-installatie voltooid is.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - App Installatie'
            Repair = '{Toolkit\CompanyName} - App Reparatie'
            Uninstall = '{Toolkit\CompanyName} - App De-installatie'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimaliseren.'
        ButtonRestartNow = 'Nu opnieuw opstarten.'
        Message = @{
            Install = 'Om de installatie te voltooien, moet u uw computer opnieuw opstarten.'
            Repair = 'Om de reparatie te voltooien, moet u uw computer opnieuw opstarten.'
            Uninstall = 'Om de de-installatie te voltooien, moet u uw computer opnieuw opstarten.'
        }
        CustomMessage = ''
        MessageRestart = 'Uw computer wordt automatisch opnieuw opgestart aan het einde van het aftellen.'
        MessageTime = 'Sla uw werk op en start uw computer binnen de toegewezen tijd opnieuw op.'
        TimeRemaining = 'Resterende tijd:'
        Title = 'Opnieuw opstarten vereist'
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - App Installatie'
            Repair = '{Toolkit\CompanyName} - App Reparatie'
            Uninstall = '{Toolkit\CompanyName} - App De-installatie'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'De volgende toepassing wordt geïnstalleerd:'
                Repair = 'De volgende toepassing wordt gerepareerd:'
                Uninstall = 'De volgende toepassing wordt gede-installeerd:'
            }
            CloseAppsMessage = @{
                Install = "De volgende applicaties moeten worden afgesloten voordat de installatie kan worden uitgevoerd.`n`nSla uw werk op, sluit de applicaties af en ga dan verder. U kunt ook uw werk opslaan en op `"Applicaties sluiten`" klikken."
                Repair = "De volgende applicaties moeten worden afgesloten voordat de reparatie kan worden uitgevoerd.`n`nSla uw werk op, sluit de applicaties af en ga dan verder. U kunt ook uw werk opslaan en op `"Applicaties sluiten`" klikken."
                Uninstall = "De volgende applicaties moeten worden afgesloten voordat de de-installatie kan worden uitgevoerd.`n`nSla uw werk op, sluit de applicaties af en ga dan verder. U kunt ook uw werk opslaan en op `"Applicaties sluiten`" klikken."
            }
            ExpiryMessage = @{
                Install = "U kunt ervoor kiezen de installatie uit te stellen totdat het uitstel verloopt:"
                Repair = "U kunt ervoor kiezen de reparatie uit te stellen totdat het uitstel verloopt:"
                Uninstall = "U kunt ervoor kiezen de de-installatie uit te stellen totdat het uitstel verloopt:"
            }
            DeferralsRemaining = 'Uitstel keren beschikbaar:'
            DeferralDeadline = 'Termijn:'
            ExpiryWarning = 'Als het uitstel is verlopen, hebt u niet langer de mogelijkheid om uit te stellen.'
            CountdownDefer = @{
                Install = 'De installatie gaat automatisch verder na:'
                Repair = 'De reparatie gaat automatisch verder na:'
                Uninstall = 'De de-installatie wordt automatisch voortgezet na:'
            }
            CountdownClose = @{
                Install = 'OPMERKING: De applicaties worden automatisch afgesloten na:'
                Repair = 'OPMERKING: De applicaties worden automatisch afgesloten na:'
                Uninstall = 'OPMERKING: De applicaties worden automatisch afgesloten na:'
            }
            ButtonClose = "Sluit &Applicaties"
            ButtonDefer = '&Verwijderen'
            ButtonContinue = '&Doorgaan'
            ButtonContinueTooltip = "Selecteer alleen ‘Doorgaan’ na het sluiten van de bovengenoemde applicatie(s)."
        }
        Fluent = @{
            DialogMessage = @{
                Install = 'Sla uw werk op voordat u verder gaat, omdat de volgende applicaties automatisch worden gesloten. Als u nog uitstel heeft, kunt u de de-installatie ook uitstellen.'
                Repair = 'Sla uw werk op voordat u verder gaat, omdat de volgende applicaties automatisch worden gesloten. Als u nog uitstel heeft, kunt u de de-installatie ook uitstellen.'
                Uninstall = 'Sla uw werk op voordat u verder gaat, omdat de volgende applicaties automatisch worden gesloten. Als u nog uitstel heeft, kunt u de de-installatie ook uitstellen.'
            }
            DialogMessageNoProcesses = @{
                Install = 'Selecteer Installeren om door te gaan met de installatie. Als u nog uitstel heeft, kunt u er ook voor kiezen om de installatie uit te stellen.'
                Repair = 'Selecteer Repareren om door te gaan met de reparatie. Als u nog uitstel heeft, kunt u er ook voor kiezen om de reparatie uit te stellen.'
                Uninstall = 'Selecteer De-installeren om door te gaan met de de-installatie. Als u nog uitstel heeft, kunt u de de-installatie ook uitstellen.'
            }
            AutomaticStartCountdown = 'Automatische start aftellen'
            DeferralsRemaining = 'Resterende uitstel'
            DeferralDeadline = 'Uitsteltermijn'
            ButtonLeftText = @{
                Install = 'Sluit Apps & installeer.'
                Repair = 'Sluit Apps & repareer.'
                Uninstall = 'Sluit Apps & de-installeer.'
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Installeren.'
                Repair = 'Repareren.'
                Uninstall = 'De-installeren.'
            }
            ButtonRightText = 'Uitstellen'
            Subtitle = @{
                Install = '{Toolkit\CompanyName} - App Installatie'
                Repair = '{Toolkit\CompanyName} - App Reparatie'
                Uninstall = '{Toolkit\CompanyName} - App De-installatie'
            }
        }
        CustomMessage = ''
    }
}
