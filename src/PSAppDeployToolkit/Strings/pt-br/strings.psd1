@{
    BalloonText = @{
        Complete = "concluída."
        Error = "falhou."
        FastRetry = "não concluída."
        RestartRequired = "concluída. É necessário reiniciar."
        Start = "iniciada."
    }
    BlockExecution = @{
        Message = "A execução deste aplicativo foi temporariamente bloqueada para que uma operação de instalação seja concluída."
    }
    ClosePrompt = @{
        ButtonClose = "Fechar Programas"
        ButtonContinue = "Continuar"
        ButtonContinueTooltip = "Apenas selecione `"Continuar`" depois de fechar aplicativo(s) acima."
        ButtonDefer = "Adiar"
        CountdownMessage = "OBSERVAÇÃO: O(s) programa(s) será(ão) automaticamente fechado(s) em:"
        Message = "Os seguintes programas precisam ser fechados antes que a instalação possa prosseguir.`nSalve seu trabalho, feche os programas e depois continue. Como alternativa, salve seu trabalho e clique em `"Fechar Programas`"."
    }
    DeferPrompt = @{
        Deadline = "Prazo:"
        ExpiryMessage = "Você pode optar por adiar a instalação até que o adiamento expire:"
        RemainingDeferrals = "Adiamentos Restantes:"
        WarningMessage = "Depois que o adiamento expirar, você não terá mais a opção de adiar."
        WelcomeMessage = "O seguinte aplicativo está prestes a ser instalado:"
    }
    DeploymentType = @{
        Install = "Instalação"
        Repair = "Reparação"
        Uninstall = "Desinstalação"
    }
    DiskSpace = @{
        Message = "Você não tem espaço em disco suficiente para concluir a instalação de:`n{0}`n`nEspaço necessário: {1}MB`nEspaço disponível: {2}MB`n`nLibere espaço em disco suficiente para prosseguir com a instalação."
    }
    Progress = @{
        MessageInstall = "Instalação em andamento. Aguarde..."
        MessageInstallDetail = "Essa janela será fechada automaticamente quando a instalação for concluída."
        MessageRepair = "Reparação em andamento. Aguarde..."
        MessageRepairDetail = "Essa janela será fechada automaticamente quando o reparo for concluído."
        MessageUninstall = "Desinstalação em andamento. Aguarde..."
        MessageUninstallDetail = "Essa janela será fechada automaticamente quando a desinstalação for concluída."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimizar"
        ButtonRestartNow = "Reiniciar Agora"
        Message = "Para que a instalação seja concluída, é necessário reiniciar o computador."
        MessageRestart = "Seu computador será reiniciado automaticamente no final da contagem regressiva."
        MessageTime = "Salve seu trabalho e reinicie dentro do prazo estipulado."
        TimeRemaining = "Tempo restante:"
        Title = "Reinicialização Necessária"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "O {0} continuará automaticamente em:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - Aplicativo {0}'
            DialogMessage = 'Salve seu trabalho antes de continuar, pois os aplicativos a seguir serão fechados automaticamente.'
            DialogMessageNoProcesses = 'Selecione Install para continuar com a instalação. Se houver algum adiamento restante, você também poderá optar por adiar a instalação.'
            ButtonDeferRemaining = 'permanecer'
            ButtonLeftText = 'Adiar'
            ButtonRightText = 'Fechar aplicativos e instalar'
            ButtonRightTextNoProcesses = 'Instalar'
        }
    }
}
