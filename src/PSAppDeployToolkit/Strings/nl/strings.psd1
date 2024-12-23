@{
    BalloonText = @{
        Complete = @{
            Install = 'Installatie voltooid.'
            Repair = 'Reparatie voltooid.'
            Uninstall = 'Installatie ongedaan maken voltooid.'
        }
        Error = @{
            Install = 'Installatie mislukt.'
            Repair = 'Reparatie mislukt.'
            Uninstall = 'De-installatie mislukt.'
        }
        FastRetry = @{
            Install = 'Installatie niet voltooid.'
            Repair = 'Reparatie niet voltooid.'
            Uninstall = 'De-installatie niet voltooid.'
        }
        RestartRequired = @{
            Install = 'Installatie voltooid. Een herstart is vereist.'
            Repair = 'Reparatie voltooid. Opnieuw opstarten is vereist.'
            Uninstall = 'De-installatie voltooid. Opnieuw opstarten is vereist.'
        }
        Start = @{
            Install = 'Installatie gestart.'
            Repair = 'Reparatie gestart.'
            Uninstall = 'De-installatie gestart.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = "Het starten van deze toepassing is tijdelijk geblokkeerd zodat een installatiebewerking kan worden voltooid."
            Repair = "Het starten van deze applicatie is tijdelijk geblokkeerd zodat een reparatie kan worden uitgevoerd."
            Uninstall = "Het starten van deze toepassing is tijdelijk geblokkeerd zodat een de-installatie kan worden voltooid."
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installatie'
            Repair = 'PSAppDeployToolkit - App Repareren'
            Uninstall = 'PSAppDeployToolkit - App De-installatie'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "U hebt niet genoeg schijfruimte om de installatie van:`n{0}`n`nAfgeronde ruimte: {1}MB`nRuimte beschikbaar: {2}MB`n`nMaak voldoende schijfruimte vrij om door te gaan met de installatie."
            Repair = "U hebt niet genoeg schijfruimte om de reparatie te voltooien van:`n{0}`n`nVerplichte ruimte: {1}MB`nBeschikbare ruimte: {2}MB`n`nMaak voldoende schijfruimte vrij om door te gaan met de reparatie."
            Uninstall = "U hebt niet genoeg schijfruimte om de deïnstallatie van:`n{0}`n`nAfgeronde ruimte: {1}MB`nBeschikbare ruimte: {2}MB`n`nMaak alstublieft voldoende schijfruimte vrij om door te gaan met de deïnstallatie."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Installatie wordt uitgevoerd. Even geduld a.u.b...'
            Repair = 'Reparatie wordt uitgevoerd. Even geduld a.u.b...'
            Uninstall = 'Installatie wordt uitgevoerd. Even geduld a.u.b...'
        }
        MessageDetail = @{
            Install = 'Dit venster wordt automatisch gesloten als de installatie voltooid is.'
            Repair = 'Dit venster wordt automatisch gesloten als de reparatie is voltooid.'
            Uninstall = 'Dit venster wordt automatisch gesloten wanneer de de-installatie voltooid is.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installatie'
            Repair = 'PSAppDeployToolkit - App Repareren'
            Uninstall = 'PSAppDeployToolkit - App De-installatie'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installatie'
            Repair = 'PSAppDeployToolkit - App Repareren'
            Uninstall = 'PSAppDeployToolkit - App De-installatie'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimaliseren'.
        ButtonRestartNow = 'Nu opnieuw starten'.
        Message = @{
            Install = 'Om de installatie te voltooien, moet u uw computer opnieuw opstarten.'
            Repair = 'Om de reparatie te voltooien, moet u uw computer opnieuw opstarten.'
            Uninstall = 'Om de de-installatie te voltooien, moet u uw computer opnieuw opstarten.'
        }
        MessageRestart = 'Uw computer wordt automatisch opnieuw opgestart aan het einde van het aftellen.'
        MessageTime = 'Sla uw werk op en start binnen de toegewezen tijd opnieuw op.'
        TimeRemaining = 'Resterende tijd:'
        Title = 'Opnieuw opstarten vereist'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installatie'
            Repair = 'PSAppDeployToolkit - App Repareren'
            Uninstall = 'PSAppDeployToolkit - App De-installatie'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = "Sluit &Programma's"
                ButtonContinue = '&Doorgaan'
                ButtonContinueTooltip = 'Selecteer alleen ‘Doorgaan’ na het sluiten van de bovengenoemde toepassing(en).'
                ButtonDefer = '&Verwijderen'
                CountdownMessage = "OPMERKING: De programma's worden automatisch afgesloten in:"
                Message = @{
                    Install = "De volgende programma's moeten worden afgesloten voordat de installatie kan doorgaan.`n`nSla uw werk op, sluit de programma's af en ga dan verder. U kunt ook uw werk opslaan en op “Programma's sluiten” klikken."
                    Repair = "De volgende programma's moeten worden afgesloten voordat de reparatie kan worden uitgevoerd.`n`nSla uw werk op, sluit de programma's af en ga dan verder. U kunt ook uw werk opslaan en op “Programma`s sluiten” klikken."
                    Uninstall = "De volgende programma's moeten gesloten worden voordat de de-installatie kan doorgaan.`n`nSla uw werk op, sluit de programma's af en ga dan verder. U kunt ook uw werk opslaan en op “Programma's sluiten” klikken."
                }
            }
            Defer = @{
                Deadline = 'Termijn:'
                ExpiryMessage = @{
                    Install = "U kunt ervoor kiezen de installatie uit te stellen totdat het uitstel verloopt:"
                    Repair = "U kunt ervoor kiezen de reparatie uit te stellen totdat het uitstel verloopt:"
                    Uninstall = "U kunt ervoor kiezen de de-installatie uit te stellen totdat het uitstel verloopt:"
                }
                RemainingDeferrals = 'Resterende uitstellen:'
                WarningMessage = 'Als het uitstel is verlopen, hebt u niet langer de mogelijkheid om uit te stellen.'
                WelcomeMessage = @{
                    Install = 'De volgende toepassing wordt geïnstalleerd:'
                    Repair = 'De volgende toepassing wordt gerepareerd:'
                    Uninstall = 'De volgende toepassing wordt verwijderd:'
                }
            }
            CountdownMessage = @{
                Install = 'De installatie gaat automatisch verder in:'
                Repair = 'De reparatie gaat automatisch verder in:'
                Uninstall = 'De de-installatie wordt automatisch voortgezet in:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - App Installatie'
                Repair = 'PSAppDeployToolkit - App Repareren'
                Uninstall = 'PSAppDeployToolkit - App De-installatie'
            }
            DialogMessage = 'Sla uw werk op voordat u verder gaat, want de volgende toepassingen worden automatisch gesloten.'
            DialogMessageNoProcesses = @{
                Install = 'Selecteer Installeren om door te gaan met de installatie. Als u nog uitstel heeft, kunt u er ook voor kiezen om de installatie uit te stellen.'
                Repair = 'Selecteer Reparatie om door te gaan met de reparatie. Als u nog uitstel heeft, kunt u er ook voor kiezen om de reparatie uit te stellen.'
                Uninstall = 'Selecteer Uninstall om door te gaan met de de-installatie. Als u nog uitstel heeft, kunt u de de-installatie ook uitstellen.'
            }
            ButtonDeferRemaining = 'blijven'
            ButtonLeftText = 'Uitstellen'
            ButtonRightText = @{
                Install = 'Apps sluiten & installeren'.
                Repair = 'Apps sluiten & repareren'.
                Uninstall = 'Apps sluiten & verwijderen'.
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Installeren'.
                Repair = 'Repareren'.
                Uninstall = 'Verwijderen'.
            }
        }
    }
}
