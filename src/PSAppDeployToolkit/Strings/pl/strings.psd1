@{
    BalloonText = @{
        Complete = @{
            Install = 'Instalacja zakończona.'
            Repair = 'Naprawa zakończona.'
            Uninstall = 'Dezinstalacja zakończona.'
        }
        Error = @{
            Install = 'Instalacja nie powiodła się.'
            Repair = 'Naprawa nie powiodła się.'
            Uninstall = 'Dezinstalacja nie powiodła się.'
        }
        FastRetry = @{
            Install = 'Instalacja nie została ukończona.'
            Repair = 'Naprawa nie została zakończona.'
            Uninstall = 'Dezinstalacja nie została ukończona.'
        }
        RestartRequired = @{
            Install = 'Instalacja zakończona. Wymagany jest restart.'
            Repair = 'Naprawa zakończona. Wymagany jest restart.'
            Uninstall = 'Dezinstalacja zakończona. Wymagane jest ponowne uruchomienie komputera.'
        }
        Start = @{
            Install = 'Rozpoczęto instalację.'
            Repair = 'Rozpoczęto naprawę.'
            Uninstall = 'Rozpoczęto dezinstalację.'
        }
    }
    BlockExecution = @{
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
    DiskSpace = @{
        Message = @{
            Install = "Nie mają Państwo wystarczającej ilości miejsca na dysku, aby ukończyć instalację:`n{0}`n`nWymagane miejsce: {1}MB`nDostępne miejsce: {2}MB`n`nProszę zwolnić wystarczającą ilość miejsca na dysku, aby kontynuować instalację."
            Repair = "Nie ma wystarczającej ilości miejsca na dysku, aby dokończyć naprawę:`n{0}`n`nWymagane miejsce: {1}MB`nDostępne miejsce: {2}MB`n`nProszę zwolnić wystarczającą ilość miejsca na dysku, aby kontynuować naprawę."
            Uninstall = "Nie ma wystarczającej ilości miejsca na dysku, aby ukończyć dezinstalację:`n{0}`n`nWymagane miejsce: {1}MB`nDostępne miejsce: {2}MB`n`nProszę zwolnić wystarczającą ilość miejsca na dysku, aby kontynuować dezinstalację."
        }
    }
    Progress = @{
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
    Prompt = @{
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
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Zamknij &Programy'
                ButtonContinue = '&Kontynuuj'
                ButtonContinueTooltip = 'Proszę wybrać »Kontynuuj« tylko po zamknięciu wyżej wymienionych aplikacji.'
                ButtonDefer = '&Odroczyć'
                CountdownMessage = 'UWAGA: Program(y) zostanie(ą) automatycznie zamknięty(e) w:'
                Message = @{
                    Install = "Następujące programy muszą zostać zamknięte przed kontynuowaniem instalacji.`n`nProszę zapisać swoją pracę, zamknąć programy, a następnie kontynuować. Alternatywnie, proszę zapisać pracę i kliknąć `„Zamknij programy`”."
                    Repair = "Następujące programy muszą zostać zamknięte przed kontynuowaniem naprawy.`n`nProszę zapisać swoją pracę, zamknąć programy, a następnie kontynuować. Alternatywnie, proszę zapisać swoją pracę i kliknąć `„Zamknij programy`”."
                    Uninstall = "Następujące programy muszą zostać zamknięte przed przystąpieniem do dezinstalacji.`n`nProszę zapisać pracę, zamknąć programy, a następnie kontynuować. Alternatywnie, proszę zapisać pracę i kliknąć `„Zamknij programy`”."
                }
            }
            Defer = @{
                Deadline = 'Termin:'
                ExpiryMessage = @{
                    Install = 'Mogą Państwo wybrać odroczenie instalacji do czasu wygaśnięcia odroczenia:'
                    Repair = 'Mogą Państwo wybrać opcję odroczenia naprawy do momentu wygaśnięcia odroczenia:'
                    Uninstall = 'Mogą Państwo wybrać odroczenie deinstalacji do czasu wygaśnięcia odroczenia:'
                }
                RemainingDeferrals = 'Pozostałe odroczenia:'
                WarningMessage = 'Po wygaśnięciu odroczenia nie będzie już możliwości odroczenia.'
                WelcomeMessage = @{
                    Install = 'Następująca aplikacja zostanie wkrótce zainstalowana:'
                    Repair = 'Następująca aplikacja ma zostać naprawiona:'
                    Uninstall = 'Następująca aplikacja ma zostać odinstalowana:'
                }
            }
            CountdownMessage = @{
                Install = 'Instalacja będzie automatycznie kontynuowana w:'
                Repair = 'Naprawa będzie automatycznie kontynuowana w:'
                Uninstall = 'Deinstalacja będzie automatycznie kontynuowana w:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Instalacja Aplikacji'
                Repair = 'PSAppDeployToolkit - Naprawa Aplikacji'
                Uninstall = 'PSAppDeployToolkit - Dezinstalacja Aplikacji'
            }
            DialogMessage = 'Proszę zapisać pracę przed kontynuowaniem, ponieważ następujące aplikacje zostaną automatycznie zamknięte.'
            DialogMessageNoProcesses = @{
                Install = 'Proszę wybrać Install, aby kontynuować instalację. Jeśli pozostały jakieś odroczenia, możesz również opóźnić instalację.'
                Repair = 'Proszę wybrać Repair, aby kontynuować naprawę. Jeśli pozostały jakieś odroczenia, możesz również opóźnić naprawę.'
                Uninstall = 'Proszę wybrać Uninstall, aby kontynuować dezinstalację. Jeśli pozostały jakieś odroczenia, możesz również opóźnić dezinstalację.'
            }
            ButtonDeferRemaining = 'pozostało'
            ButtonLeftText = 'Odroczyć'
            ButtonRightText = @{
                Install = 'Zamknij aplikacje i zainstaluj'
                Repair = 'Zamknij aplikacje i napraw'
                Uninstall = 'Zamknij aplikacje i odinstaluj'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Zainstaluj'
                Repair = 'Napraw'
                Uninstall = 'Odinstaluj'
            }
        }
    }
}
