@{
    BalloonTip = @{
        Start = @{
            Install = 'Instalace byla zahájena.'
            Repair = 'Oprava zahájena.'
            Uninstall = 'Odinstalace zahájena.'
        }
        Complete = @{
            Install = 'Instalace dokončena.'
            Repair = 'Oprava dokončena.'
            Uninstall = 'Odinstalace dokončena.'
        }
        RestartRequired = @{
            Install = 'Instalace dokončena. Je vyžadován restart.'
            Repair = 'Oprava dokončena. Je vyžadován restart.'
            Uninstall = 'Odinstalace dokončena. Je vyžadován restart.'
        }
        FastRetry = @{
            Install = 'Instalace nebyla dokončena.'
            Repair = 'Oprava nebyla dokončena.'
            Uninstall = 'Odinstalace nebyla dokončena.'
        }
        Error = @{
            Install = 'Instalace se nezdařila.'
            Repair = 'Oprava se nezdařila.'
            Uninstall = 'Odinstalace se nezdařila.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'Spuštění této aplikace bylo dočasně zablokováno, aby mohla být dokončena instalační operace.'
            Repair = 'Spuštění této aplikace bylo dočasně zablokováno, aby mohla být dokončena operace opravy.'
            Uninstall = 'Spuštění této aplikace bylo dočasně zablokováno, aby mohla být dokončena operace odinstalace.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalace Aplikace'
            Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
            Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Nemáte dostatek místa na disku pro dokončení instalace:`n{0}`n`nPotřebné místo: {1}MB`nDostupné místo: {2}MB`n`nUvolněte prosím dostatek místa na disku, abyste mohli pokračovat v instalaci."
            Repair = "Nemáte dostatek místa na disku pro dokončení oprava:`n{0}`n`nPotřebné místo: {1}MB`nDostupné místo: {2}MB`n`nUvolněte prosím dostatek místa na disku, abyste mohli pokračovat v opravě."
            Uninstall = "Nemáte dostatek místa na disku, abyste mohli dokončit odinstalaci:`n{0}`n`nPotřebné místo: {1}MB`nDostupné místo: {2}MB`n`nUvolněte prosím dostatek místa na disku, abyste mohli pokračovat v odinstalaci."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalace Aplikace'
            Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
            Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Probíhá instalace. Počkejte prosím…'
            Repair = 'Probíhá oprava. Počkejte prosím…'
            Uninstall = 'Probíhá odinstalace. Počkejte prosím…'
        }
        MessageDetail = @{
            Install = 'Toto okno se po dokončení instalace automaticky zavře.'
            Repair = 'Toto okno se automaticky zavře po dokončení opravy.'
            Uninstall = 'Toto okno se automaticky zavře po dokončení odinstalace.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalace Aplikace'
            Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
            Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimalizovat'
        ButtonRestartNow = 'Restartovat nyní'
        Message = @{
            Install = 'Aby se instalace dokončila, musíte restartovat počítač.'
            Repair = 'Aby byla oprava dokončena, musíte restartovat počítač.'
            Uninstall = 'Aby se odinstalace dokončila, musíte restartovat počítač.'
        }
        CustomMessage = ''
        MessageRestart = 'Po skončení odpočítávání bude počítač automaticky restartován.'
        MessageTime = 'Uložte prosím svou práci a restartujte ji ve stanoveném čase.'
        TimeRemaining = 'Zbývající čas:'
        Title = 'Požadovaný Restart'
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalace Aplikace'
            Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
            Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'Chystá se instalace následující aplikace:'
                Repair = 'Následující aplikace bude opravena:'
                Uninstall = 'Následující aplikace bude odinstalována:'
            }
            CloseAppsMessage = @{
                Install = "Před pokračováním instalace je nutné ukončit následující programy.`n`nProsím, uložte svou práci, ukončete programy a poté pokračujte. Případně uložte svou práci a klikněte na `„Zavřít programy`“."
                Repair = "Před pokračováním opravy je nutné zavřít následující programy.`n`nProsím, uložte svou práci, zavřete programy a poté pokračujte. Případně uložte svou práci a klikněte na `„Zavřít programy`“."
                Uninstall = "Před pokračováním odinstalace je nutné zavřít následující programy.`n`nProsím, uložte svou práci, zavřete programy a poté pokračujte. Případně uložte svou práci a klikněte na `„Zavřít programy`“."
            }
            ExpiryMessage = @{
                Install = 'Můžete se rozhodnout odložit instalaci až do vypršení odkladu:'
                Repair = 'Můžete se rozhodnout odložit opravu až do vypršení odkladu:'
                Uninstall = 'Můžete se rozhodnout odložit odinstalaci až do vypršení odkladu:'
            }
            DeferralsRemaining = 'Zbývající odklady:'
            DeferralDeadline = 'Termín:'
            ExpiryWarning = 'Po uplynutí odkladu již nebudete mít možnost odložit.'
            CountdownDefer = @{
                Install = 'Instalace bude automaticky pokračovat za:'
                Repair = 'Oprava bude automaticky pokračovat za:'
                Uninstall = 'Odinstalace bude automaticky pokračovat za:'
            }
            CountdownClose = @{
                Install = 'POZNÁMKA: Program(y) budou automaticky ukončeny za:'
                Repair = 'POZNÁMKA: Program(y) budou automaticky ukončeny za:'
                Uninstall = 'POZNÁMKA: Program(y) budou automaticky ukončeny za:'
            }
            ButtonClose = 'Zavřít &Programy'
            ButtonDefer = '&Odložení'
            ButtonContinue = '&Pokračovat'
            ButtonContinueTooltip = 'Po zavření výše uvedených aplikací vyberte pouze možnost „Pokračovat“.'
        }
        Fluent = @{
            DialogMessage = @{
                Install = 'Před pokračováním prosím uložte svou práci, protože následující aplikace budou automaticky ukončeny.'
                Repair = 'Před pokračováním prosím uložte svou práci, protože následující aplikace budou automaticky ukončeny.'
                Uninstall = 'Před pokračováním prosím uložte svou práci, protože následující aplikace budou automaticky ukončeny.'
            }
            DialogMessageNoProcesses = @{
                Install = 'Chcete-li pokračovat v instalaci, vyberte prosím možnost Install.'
                Repair = 'Pro pokračování v opravě vyberte prosím Repair.'
                Uninstall = 'Chcete-li pokračovat v odinstalaci, vyberte prosím možnost Odinstalovat.'
            }
            AutomaticStartCountdown = 'Automatické odpočítávání spuštění'
            DeferralsRemaining = 'Zbývající odklady'
            DeferralDeadline = 'Lhůta pro odložení'
            ButtonLeftText = @{
                Install = 'Zavřít Aplikace a Nainstalovat'
                Repair = 'Zavřít Aplikace a Opravit'
                Uninstall = 'Zavřít Aplikace a Odinstalovat'
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Instalovat'
                Repair = 'Opravit'
                Uninstall = 'Odinstalovat'
            }
            ButtonRightText = 'Odložit'
            Subtitle = @{
                Install = '{Toolkit\CompanyName} - Instalace Aplikace'
                Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
                Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
            }
        }
        CustomMessage = ''
    }
}
