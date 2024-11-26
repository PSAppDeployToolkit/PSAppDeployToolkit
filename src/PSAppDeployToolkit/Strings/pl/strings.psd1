@{
    BalloonText = @{
        Complete = "zakończona."
        Error = "nie powiodła się."
        FastRetry = "nieukończona."
        RestartRequired = "zakończona. Wymagany jest restart komputera."
        Start = "rozpoczęta."
    }
    BlockExecution = @{
        Message = "Uruchomienie tej aplikacji zostało zablokowane na okres instalacji."
    }
    ClosePrompt = @{
        ButtonClose = "Zamknij Programy"
        ButtonContinue = "Kontynuuj"
        ButtonContinueTooltip = "Tylko wybrać `"Kontynuuj`" po zamknięciu wyżej wymienione aplikacje."
        ButtonDefer = "Odłóż"
        CountdownMessage = "UWAGA: Programy zostaną automatycznie zamknięte za:"
        Message = "Następujące programy muszą zostać zamknięte przed rozpoczęciem instalacji.`n`nProszę zapisać wszystkie dokumenty i zamknąć programy, a następnie kliknąć przycisk `"Kontynuuj`". Alternatywnie zapisz wszystkie dokumenty i kliknij przycisk `"Zamknij Programy`"."
    }
    DeferPrompt = @{
        Deadline = "Ostateczny termin instalacji:"
        ExpiryMessage = "Instalacja może zostać przełożona na późniejszy termin."
        RemainingDeferrals = "Pozostała ilość przełożeń instalacji:"
        WarningMessage = "Jeżeli zostanie przekroczona możliwa ilość przełożeń, opcja `"Odłóż`" będzie niedostępna."
        WelcomeMessage = "Zostanie zainstalowana następująca aplikacja:"
    }
    DeploymentType = @{
        Install = "Instalacja"
        Repair = "Naprawa"
        Uninstall = "Deinstalacja"
    }
    DiskSpace = @{
        Message = "Brak miejsca na dysku:`n{0}`n`nPotrzeba: {1}MB`nObecnie wolnego miejsca: {2}MB`n`nProszę zwiększyć ilość miejsca usuwając zbędne pliki."
    }
    Progress = @{
        MessageInstall = "Trwa instalacja. Proszę czekać..."
        MessageInstallDetail = "Okno to zamknie się automatycznie po zakończeniu instalacji."
        MessageRepair = "Trwa naprawa. Proszę czekać..."
        MessageRepairDetail = "Okno to zamknie się automatycznie po zakończeniu naprawy."
        MessageUninstall = "Trwa deinstalacja. Proszę czekać..."
        MessageUninstallDetail = "Okno to zamknie się automatycznie po zakończeniu dezinstalacji."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Zminimalizować"
        ButtonRestartNow = "Restartuj Teraz"
        Message = "Aby instalacja została poprawnie ukończona wymagany jest restart komputera."
        MessageRestart = "Komputer zostanie automatycznie zrestartowany po upływie wyznaczonego czasu."
        MessageTime = "Proszę zapisać wszystkie dokumenty i zrestartować komputer w wyznaczonym czasie."
        TimeRemaining = "Pozostały czas do restartu automatycznego:"
        Title = "Wymagany Restart"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} będzie automatycznie kontynuować w:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - aplikacja {0}'
            DialogMessage = 'Zapisz swoją pracę przed kontynuowaniem, ponieważ następujące aplikacje zostaną automatycznie zamknięte.'
            DialogMessageNoProcesses = 'Wybierz opcję Zainstaluj, aby kontynuować instalację. Jeśli pozostały jakieś odroczenia, możesz również opóźnić instalację.'
            ButtonDeferRemaining = 'pozostać'
            ButtonLeftText = 'Odroczenie'
            ButtonRightText = 'Zamknij aplikacje i zainstaluj'
            ButtonRightTextNoProcesses = 'Instalacja'
        }
    }
}
