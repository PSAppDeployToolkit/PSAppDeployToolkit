@{
    BalloonText = @{
        Complete = "dokončena."
        Error = "se nepodařila."
        FastRetry = "nedokončena."
        RestartRequired = "dokončena. Je nutné restartovat počítač."
        Start = "zahájena."
    }
    BlockExecution = @{
        Message = "Spuštění této aplikace bylo dočasně zakázáno, aby mohla proběhnout instalace."
    }
    ClosePrompt = @{
        ButtonClose = "Ukončit programy"
        ButtonContinue = "Pokračovat"
        ButtonContinueTooltip = "Klikněte na `"Pokračovat`", až budete mít výše uvedené aplikace zavřené."
        ButtonDefer = "Odložit"
        CountdownMessage = "Upozornění: Programy budou automaticky zavřené za:"
        Message = "Následující programy musí být zavřené, aby instalace mohla pokračovat. Prosím, uložte svou práci, zavřete program a potom klikněte na `"Pokračovat`". Případně můžete svou práci uložit a kliknout na tlačítko `"Ukončit programy`"."
    }
    DeferPrompt = @{
        Deadline = "Termín:"
        ExpiryMessage = "Instalaci můžete několikrát odložit:"
        RemainingDeferrals = "Zbývající počet odložení:"
        WarningMessage = "Jakmile vyčerpáte všechna odložení, už nebudete mít šanci odložit instalaci."
        WelcomeMessage = "Nasledující aplikace bude nainstalována:"
    }
    DeploymentType = @{
        Install = "Instalace"
        Repair = "Oprava"
        Uninstall = "Odinstalace"
    }
    DiskSpace = @{
        Message = "Nemáte dostatek volného místa na instalaci aplikace:`n{0}`n`nPotřebné místo na disku: {1}MB`nDostupné místo na disku: {2}MB`n`nUvolněte prosím dostatek místa k pokračovaní instalace."
    }
    Progress = @{
        MessageInstall = "Instalace právě probíhá. Prosím čekejte..."
        MessageInstallDetail = "Toto okno se po dokončení instalace automaticky zavře."
        MessageRepair = "Oprava právě probíhá. Prosím čekejte..."
        MessageRepairDetail = "Toto okno se po dokončení opravy automaticky zavře."
        MessageUninstall = "Probíhá odinstalace. Prosím čekejte..."
        MessageUninstallDetail = "Po dokončení odinstalace se toto okno automaticky zavře."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimalizovat"
        ButtonRestartNow = "Restartovat nyní"
        Message = "Pro dokončení instalace musíte váš počítač restartovat."
        MessageRestart = "Na konci odpočítávání, bude váš počítač automaticky restartovaný."
        MessageTime = "Prosím, uložte si práci a restartujte počítač ve stanoveném čase."
        TimeRemaining = "Zbývající čas:"
        Title = "Je nutné restartovat počítač."
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} bude automaticky pokračovat za:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - Aplikace {0}'
            DialogMessage = 'Před pokračováním v práci ji uložte, protože následující aplikace budou automaticky uzavřeny.'
            DialogMessageNoProcesses = 'Chcete-li pokračovat v instalaci, vyberte možnost Instalovat. Pokud vám zbývají nějaké odklady, můžete také zvolit odložení instalace.'
            ButtonDeferRemaining = 'zůstat'
            ButtonLeftText = 'Odložení'
            ButtonRightText = 'Zavření aplikací a instalace'
            ButtonRightTextNoProcesses = 'Instalace'
        }
    }
}
