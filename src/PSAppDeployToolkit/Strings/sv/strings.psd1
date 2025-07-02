@{
    BalloonTip = @{
        Start = @{
            Install = 'Installation påbörjad.'
            Repair = 'Reparation påbörjad.'
            Uninstall = 'Avinstallation påbörjad.'
        }
        Complete = @{
            Install = 'Installation slutförd.'
            Repair = 'Reparation slutförd.'
            Uninstall = 'Avinstallation slutförd.'
        }
        RestartRequired = @{
            Install = 'Installationen slutförd. En omstart krävs.'
            Repair = 'Reparation slutförd. En omstart krävs.'
            Uninstall = 'Avinstallation slutförd. En omstart krävs.'
        }
        FastRetry = @{
            Install = 'Installationen slutfördes inte.'
            Repair = 'Reparationen slutfördes inte.'
            Uninstall = 'Avinstallationen slutfördes inte.'
        }
        Error = @{
            Install = 'Installationen misslyckades.'
            Repair = 'Reparationen misslyckades.'
            Uninstall = 'Avinstallationen misslyckades.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'Den här applikationen har temporärt blockerats så att en installation kan slutföras.'
            Repair = 'Den här applikationen har temporärt blockerats så att en reparation kan slutföras.'
            Uninstall = 'Den här applikationen har temporärt blockerats så att en avinstallation kan slutföras.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation av App'
            Repair = '{Toolkit\CompanyName} - Reparation av App'
            Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Du har inte tillräckligt med ledigt diskutrymme för att kunna installera:`n{0}`n`nDiskutrymme som krävs: {1}MB`nLedigt diskutrymme: {2}MB`n`nVar vänlig frigör tillräckligt med diskutrymme för att kunna fortsätta med installationen."
            Repair = "Du har inte tillräckligt med ledigt diskutrymme för att kunna reparera:`n{0}`n`nDiskutrymme som krävs: {1}MB`nLedigt diskutrymmee: {2}MB`n`nVar vänlig frigör tillräckligt med diskutrymme för att kunna fortsätta med reparationen."
            Uninstall = "Du har inte tillräckligt med ledigt diskutrymme för att kunna avinstallera:`n{0}`n`nDiskutrymme som krävs: {1}MB`nLedigt diskutrymmee: {2}MB`n`nVar vänlig frigör tillräckligt med diskutrymme för att kunna fortsätta med avinstallationen."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation av App'
            Repair = '{Toolkit\CompanyName} - Reparation av App'
            Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Installation pågår. Var god vänta…'
            Repair = 'Reparation pågår. Var god vänta…'
            Uninstall = 'Avinstallation pågår. Var god vänta…'
        }
        MessageDetail = @{
            Install = 'Detta fönster stängs automatiskt när installationen är klar.'
            Repair = 'Detta fönster stängs automatiskt när reparationen är klar.'
            Uninstall = 'Detta fönster stängs automatiskt när avinstallationen är klar.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation av App'
            Repair = '{Toolkit\CompanyName} - Reparation av App'
            Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimera'
        ButtonRestartNow = 'Starta om nu'
        Message = @{
            Install = 'För att installationen ska kunna slutföras måste du starta om datorn.'
            Repair = 'För att reparationen ska kunna slutföras måste du starta om datorn.'
            Uninstall = 'För att avinstallationen ska kunna slutföras måste du starta om datorn.'
        }
        CustomMessage = ''
        MessageRestart = 'Din dator kommer att startas om automatiskt när nedräkningen är slut.'
        MessageTime = 'Var vänlig spara ditt arbete och starta om datorn innan tiden går ut.'
        TimeRemaining = 'Återstående tid:'
        Title = 'Omstart Krävs'
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation av App'
            Repair = '{Toolkit\CompanyName} - Reparation av App'
            Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'Följande applikation kommer att installeras:'
                Repair = 'Följande applikation kommer att repareras:'
                Uninstall = 'Följande applikation kommer att avinstalleras:'
            }
            CloseAppsMessage = @{
                Install = "Följande program måste stängas innan installationen kan fortsätta.`n`nVar vänlig spara ditt arbete, stäng de öppna programmen och`nklicka sedan på `"Fortsätt`".`nAlternativt, spara ditt arbete och klicka sedan på `"Stäng Program`"."
                Repair = "Följande program måste stängas innan reparationen kan fortsätta.`n`nVar vänlig spara ditt arbete, stäng de öppna programmen och`nklicka sedan på `"Fortsätt`".`nAlternativt, spara ditt arbete och klicka sedan på `"Stäng Program`"."
                Uninstall = "Följande program måste stängas innan avinstallationen kan fortsätta.`n`nVar vänlig spara ditt arbete, stäng de öppna programmen och`nklicka sedan på `"Fortsätt`".`nAlternativt, spara ditt arbete och klicka sedan på `"Stäng Program`"."
            }
            ExpiryMessage = @{
                Install = 'Du kan välja att fördröja installationen ett begränsat antal gånger under en begränsad tid:'
                Repair = 'Du kan välja att fördröja reparationen ett begränsat antal gånger under en begränsad tid:'
                Uninstall = 'Du kan välja att fördröja avinstallationen ett begränsat antal gånger under en begränsad tid:'
            }
            DeferralsRemaining = 'Antal återstående fördröjningar:'
            DeferralDeadline = 'Deadline:'
            ExpiryWarning = 'När antalet fördröjningar är slut eller deadline inträffat är detta alternativ inte längre tillgängligt.'
            CountdownDefer = @{
                Install = 'Installationen kommer automatiskt att fortsätta om:'
                Repair = 'Reparationen kommer automatiskt att fortsätta om:'
                Uninstall = 'Avinstallationen kommer automatiskt att fortsätta om:'
            }
            CountdownClose = 'OBS: Program stängs automatisk om:'
            ButtonClose = 'Stäng &Program'
            ButtonDefer = '&Skjut Upp'
            ButtonContinue = '&Fortsätt'
            ButtonContinueTooltip = 'Välj "Fortsätt" först efter att du har stängt ovanstående program.'
        }
        Fluent = @{
            DialogMessage = 'Följande program måste stängas. Var vänlig spara ditt arbete och stäng sedan de öppna programmen.'
            DialogMessageNoProcesses = @{
                Install = 'Välj Installera för att fortsätta med installationen eller välj "Skjut upp" för att installationen skall utföras vid ett senare tillfälle.'
                Repair = 'Välj Reparera för att fortsätta med reparationen.'
                Uninstall = 'Välj Avinstallera för att fortsätta med avinstallationen.'
            }
            AutomaticStartCountdown = 'Fortsätter automatisk om:'
            DeferralsRemaining = 'Antal återstående fördröjningar'
            DeferralDeadline = 'Deadline:'
            ButtonLeftText = @{
                Install = 'Stäng Appar & Installera'
                Repair = 'Stäng Appar & Reparera'
                Uninstall = 'Stäng Appar & Avinstallera'
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Installera'
                Repair = 'Reparera'
                Uninstall = 'Avinstallera'
            }
            ButtonRightText = 'Skjut upp'
            Subtitle = @{
                Install = '{Toolkit\CompanyName} - Installation av App'
                Repair = '{Toolkit\CompanyName} - Reparation av App'
                Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
            }
        }
        CustomMessage = ''
    }
}
