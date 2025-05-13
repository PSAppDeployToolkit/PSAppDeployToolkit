@{
    BalloonTip = @{
        Start = @{
            Install = 'Rozpoczęto instalację.'
            Repair = 'Rozpoczęto naprawę.'
            Uninstall = 'Rozpoczęto dezinstalację.'
        }
        Complete = @{
            Install = 'Instalacja zakończona.'
            Repair = 'Naprawa zakończona.'
            Uninstall = 'Dezinstalacja zakończona.'
        }
        RestartRequired = @{
            Install = 'Instalacja zakończona. Wymagany jest restart.'
            Repair = 'Naprawa zakończona. Wymagany jest restart.'
            Uninstall = 'Dezinstalacja zakończona. Wymagane jest ponowne uruchomienie komputera.'
        }
        FastRetry = @{
            Install = 'Instalacja nie została ukończona.'
            Repair = 'Naprawa nie została zakończona.'
            Uninstall = 'Dezinstalacja nie została ukończona.'
        }
        Error = @{
            Install = 'Instalacja nie powiodła się.'
            Repair = 'Naprawa nie powiodła się.'
            Uninstall = 'Dezinstalacja nie powiodła się.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'Uruchamianie tej aplikacji zostało tymczasowo zablokowane, aby można było ukończyć operację instalacji.'
            Repair = 'Uruchomienie tej aplikacji zostało tymczasowo zablokowane, aby umożliwić zakończenie operacji naprawy.'
            Uninstall = 'Uruchamianie tej aplikacji zostało tymczasowo zablokowane, aby można było zakończyć operację dezinstalacji.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalacja Aplikacji'
            Repair = 'PSAppDeployToolkit - Naprawa Aplikacji'
            Uninstall = 'PSAppDeployToolkit - Dezinstalacja Aplikacji'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Nie mają Państwo wystarczającej ilości miejsca na dysku, aby ukończyć instalację:`n{0}`n`nWymagane miejsce: {1}MB`nDostępne miejsce: {2}MB`n`nProszę zwolnić wystarczającą ilość miejsca na dysku, aby kontynuować instalację."
            Repair = "Nie ma wystarczającej ilości miejsca na dysku, aby dokończyć naprawę:`n{0}`n`nWymagane miejsce: {1}MB`nDostępne miejsce: {2}MB`n`nProszę zwolnić wystarczającą ilość miejsca na dysku, aby kontynuować naprawę."
            Uninstall = "Nie ma wystarczającej ilości miejsca na dysku, aby ukończyć dezinstalację:`n{0}`n`nWymagane miejsce: {1}MB`nDostępne miejsce: {2}MB`n`nProszę zwolnić wystarczającą ilość miejsca na dysku, aby kontynuować dezinstalację."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalacja Aplikacji'
            Repair = 'PSAppDeployToolkit - Naprawa Aplikacji'
            Uninstall = 'PSAppDeployToolkit - Dezinstalacja Aplikacji'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Instalacja w toku. Proszę czekać...'
            Repair = 'Trwa naprawa. Proszę czekać...'
            Uninstall = 'Trwa dezinstalacja. Proszę czekać...'
        }
        MessageDetail = @{
            Install = 'To okno zamknie się automatycznie po zakończeniu instalacji.'
            Repair = 'To okno zostanie zamknięte automatycznie po zakończeniu naprawy.'
            Uninstall = 'To okno zostanie zamknięte automatycznie po zakończeniu dezinstalacji.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalacja Aplikacji'
            Repair = 'PSAppDeployToolkit - Naprawa Aplikacji'
            Uninstall = 'PSAppDeployToolkit - Dezinstalacja Aplikacji'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimalizuj'
        ButtonRestartNow = 'Uruchom ponownie teraz'
        Message = @{
            Install = 'Aby zakończyć instalację, należy ponownie uruchomić komputer.'
            Repair = 'Aby zakończyć naprawę, należy ponownie uruchomić komputer.'
            Uninstall = 'Aby zakończyć dezinstalację, należy ponownie uruchomić komputer.'
        }
        CustomMessage = ''
        MessageRestart = 'Państwa komputer zostanie automatycznie uruchomiony ponownie po zakończeniu odliczania.'
        MessageTime = 'Proszę zapisać swoją pracę i ponownie uruchomić komputer w wyznaczonym czasie.'
        TimeRemaining = 'Pozostały czas:'
        Title = 'Proszę uruchomić ponownie'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalacja Aplikacji'
            Repair = 'PSAppDeployToolkit - Naprawa Aplikacji'
            Uninstall = 'PSAppDeployToolkit - Dezinstalacja Aplikacji'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'Następująca aplikacja zostanie wkrótce zainstalowana:'
                Repair = 'Następująca aplikacja ma zostać naprawiona:'
                Uninstall = 'Następująca aplikacja ma zostać odinstalowana:'
            }
            CloseAppsMessage = @{
                Install = "Następujące programy muszą zostać zamknięte przed kontynuowaniem instalacji.`n`nProszę zapisać swoją pracę, zamknąć programy, a następnie kontynuować. Alternatywnie, proszę zapisać pracę i kliknąć `„Zamknij programy`”."
                Repair = "Następujące programy muszą zostać zamknięte przed kontynuowaniem naprawy.`n`nProszę zapisać swoją pracę, zamknąć programy, a następnie kontynuować. Alternatywnie, proszę zapisać swoją pracę i kliknąć `„Zamknij programy`”."
                Uninstall = "Następujące programy muszą zostać zamknięte przed przystąpieniem do dezinstalacji.`n`nProszę zapisać pracę, zamknąć programy, a następnie kontynuować. Alternatywnie, proszę zapisać pracę i kliknąć `„Zamknij programy`”."
            }
            ExpiryMessage = @{
                Install = 'Mogą Państwo wybrać odroczenie instalacji do czasu wygaśnięcia odroczenia:'
                Repair = 'Mogą Państwo wybrać opcję odroczenia naprawy do momentu wygaśnięcia odroczenia:'
                Uninstall = 'Mogą Państwo wybrać odroczenie deinstalacji do czasu wygaśnięcia odroczenia:'
            }
            DeferralsRemaining = 'Pozostałe odroczenia:'
            DeferralDeadline = 'Termin:'
            ExpiryWarning = 'Po wygaśnięciu odroczenia nie będzie już możliwości odroczenia.'
            CountdownDefer = @{
                Install = 'Instalacja będzie automatycznie kontynuowana w:'
                Repair = 'Naprawa będzie automatycznie kontynuowana w:'
                Uninstall = 'Deinstalacja będzie automatycznie kontynuowana w:'
            }
            CountdownClose = 'UWAGA: Program(y) zostanie(ą) automatycznie zamknięty(e) w:'
            ButtonClose = 'Zamknij &Programy'
            ButtonDefer = '&Odroczyć'
            ButtonContinue = '&Kontynuuj'
            ButtonContinueTooltip = 'Proszę wybrać »Kontynuuj« tylko po zamknięciu wyżej wymienionych aplikacji.'
        }
        Fluent = @{
            DialogMessage = 'Proszę zapisać pracę przed kontynuowaniem, ponieważ następujące aplikacje zostaną automatycznie zamknięte.'
            DialogMessageNoProcesses = @{
                Install = 'Proszę wybrać Install, aby kontynuować instalację. Jeśli pozostały jakieś odroczenia, możesz również opóźnić instalację.'
                Repair = 'Proszę wybrać Repair, aby kontynuować naprawę. Jeśli pozostały jakieś odroczenia, możesz również opóźnić naprawę.'
                Uninstall = 'Proszę wybrać Uninstall, aby kontynuować dezinstalację. Jeśli pozostały jakieś odroczenia, możesz również opóźnić dezinstalację.'
            }
            AutomaticStartCountdown = 'Automatyczne odliczanie do rozpoczęcia'
            DeferralsRemaining = 'Pozostałe odroczenia'
            DeferralDeadline = 'Termin odroczenia'
            ButtonLeftText = 'Odroczyć'
            ButtonRightText = @{
                Install = 'Zamknij aplikacje i zainstaluj'
                Repair = 'Zamknij aplikacje i napraw'
                Uninstall = 'Zamknij aplikacje i odinstaluj'
            }
            ButtonRightNoProcessesText = @{
                Install = 'Zainstaluj'
                Repair = 'Napraw'
                Uninstall = 'Odinstaluj'
            }
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Instalacja Aplikacji'
                Repair = 'PSAppDeployToolkit - Naprawa Aplikacji'
                Uninstall = 'PSAppDeployToolkit - Dezinstalacja Aplikacji'
            }
        }
        CustomMessage = ''
    }
}
