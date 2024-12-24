﻿@{
    BalloonText = @{
        Complete = @{
            Install = 'Installationen slutförd.'
            Repair = 'Reparation slutförd.'
            Uninstall = 'Avinstallationen slutförd.'
        }
        Error = @{
            Install = 'Installationen misslyckades.'
            Repair = 'Reparation misslyckades.'
            Uninstall = 'Avinstallationen misslyckades.'
        }
        FastRetry = @{
            Install = 'Installationen slutfördes inte.'
            Repair = 'Reparation inte slutförd.'
            Uninstall = 'Avinstallationen inte slutförd.'
        }
        RestartRequired = @{
            Install = 'Installationen är slutförd. En omstart krävs.'
            Repair = 'Reparation slutförd. En omstart krävs.'
            Uninstall = 'Avinstallation slutförd. En omstart krävs.'
        }
        Start = @{
            Install = 'Installationen har påbörjats.'
            Repair = 'Reparation påbörjad.'
            Uninstall = 'Avinstallation påbörjad.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = 'Starten av det här programmet har tillfälligt blockerats så att en installation kan slutföras.'
            Repair = 'Lanseringen av det här programmet har tillfälligt blockerats så att en reparationsåtgärd kan slutföras.'
            Uninstall = 'Lanseringen av det här programmet har tillfälligt blockerats så att en avinstallation kan slutföras.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Installation av App'
            Repair = 'PSAppDeployToolkit - Reparation av App'
            Uninstall = 'PSAppDeployToolkit - Avinstallation av App'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "Du har inte tillräckligt med diskutrymme för att slutföra installationen av:`n{0}`n`n`Space required: {1}MB`nTillgängligt utrymme: {2}MB`n`nVar vänlig frigör tillräckligt med diskutrymme för att kunna fortsätta med installationen."
            Repair = "Du har inte tillräckligt med diskutrymme för att slutföra reparationen av:`n{0}`n`n`utrymme krävs: {1}MB`nTillgängligt utrymme: {2}MB`n`nVar vänlig frigör tillräckligt med diskutrymme för att kunna fortsätta med reparationen."
            Uninstall = "Du har inte tillräckligt med diskutrymme för att slutföra avinstallationen av:`n{0}`n`nRymdbehov: {1}MB`nTillgängligt utrymme: {2}MB`n`nVar vänlig frigör tillräckligt med diskutrymme för att kunna fortsätta avinstallationen."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Installationen pågår. Vänligen vänta...'
            Repair = 'Reparation pågår. Var vänlig vänta...'
            Uninstall = 'Avinstallation pågår. Var vänlig vänta...'
        }
        MessageDetail = @{
            Install = 'Det här fönstret stängs automatiskt när installationen är klar.'
            Repair = 'Det här fönstret stängs automatiskt när reparationen är klar.'
            Uninstall = 'Det här fönstret stängs automatiskt när avinstallationen är klar.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Installation av App'
            Repair = 'PSAppDeployToolkit - Reparation av App'
            Uninstall = 'PSAppDeployToolkit - Avinstallation av App'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Installation av App'
            Repair = 'PSAppDeployToolkit - Reparation av App'
            Uninstall = 'PSAppDeployToolkit - Avinstallation av App'
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
        MessageRestart = 'Datorn kommer att startas om automatiskt när nedräkningen är slut.'
        MessageTime = 'Spara ditt arbete och starta om inom den tilldelade tiden.'
        TimeRemaining = 'Återstående tid:'
        Title = 'Omstart krävs'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Installation av App'
            Repair = 'PSAppDeployToolkit - Reparation av App'
            Uninstall = 'PSAppDeployToolkit - Avinstallation av App'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Stäng &Program'
                ButtonContinue = '&Fortsätt'
                ButtonContinueTooltip = "Välj ’Fortsätt’ först efter att du har stängt ovanstående program."
                ButtonDefer = '&Skjut Upp'
                CountdownMessage = 'OBS: Programmet/programmen kommer att stängas automatiskt i:'
                Message = @{
                    Install = "Följande program måste stängas innan installationen kan fortsätta.`n`nSpara ditt arbete, stäng programmen och fortsätt sedan. Alternativt kan du spara ditt arbete och klicka på `”Stäng program`”."
                    Repair = "Följande program måste stängas innan reparationen kan fortsätta.`n`nVar vänlig spara ditt arbete, stäng programmen och fortsätt sedan. Alternativt kan du spara ditt arbete och klicka på `”Stäng program`”."
                    Uninstall = "Följande program måste stängas innan avinstallationen kan fortsätta.`n`nSpara ditt arbete, stäng programmen och fortsätt sedan. Alternativt kan du spara ditt arbete och klicka på `”Stäng program`”."
                }
            }
            Defer = @{
                Deadline = 'Tidsfrist:'
                ExpiryMessage = @{
                    Install = 'Du kan välja att skjuta upp installationen tills uppskjutandet löper ut:'
                    Repair = 'Du kan välja att skjuta upp reparationen tills uppskjutandet löper ut:'
                    Uninstall = 'Du kan välja att skjuta upp avinstallationen tills uppskovet löper ut:'
                }
                RemainingDeferrals = 'Återstående uppskjutanden:'
                WarningMessage = 'När uppskovet har löpt ut har du inte längre möjlighet att skjuta upp det.'
                WelcomeMessage = @{
                    Install = 'Följande program är på väg att installeras:'
                    Repair = 'Följande applikation är på väg att repareras:'
                    Uninstall = 'Följande program är på väg att avinstalleras:'
                }
            }
            CountdownMessage = @{
                Install = 'Installationen kommer automatiskt att fortsätta om:'
                Repair = 'Reparationen kommer automatiskt att fortsätta i:'
                Uninstall = 'Avinstallationen kommer automatiskt att fortsätta i:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Installation av App'
                Repair = 'PSAppDeployToolkit - Reparation av App'
                Uninstall = 'PSAppDeployToolkit - Avinstallation av App'
            }
            DialogMessage = 'Spara ditt arbete innan du fortsätter eftersom följande program kommer att stängas automatiskt.'
            DialogMessageNoProcesses = @{
                Install = 'Välj Install för att fortsätta med installationen. Om du har några uppskjutanden kvar kan du också välja att skjuta upp installationen.'
                Repair = 'Välj Repair för att fortsätta med reparationen. Om du har några återstående förseningar kan du också välja att skjuta upp reparationen.'
                Uninstall = 'Välj Avinstallera för att fortsätta med avinstallationen. Om du har några kvarvarande uppskjutanden kan du också välja att skjuta upp avinstallationen.'
            }
            ButtonDeferRemaining = 'kvarstår'
            ButtonLeftText = 'Skjut upp'
            ButtonRightText = @{
                Install = 'Stäng Appar och Installera'
                Repair = 'Stäng Appar och Reparera'
                Uninstall = 'Stäng Appar & Avinstallera'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Installera'
                Repair = 'Reparation'
                Uninstall = 'Avinstallera'
            }
        }
    }
}
