@{
    BalloonText = @{
        Complete = "completo."
        Error = "falhou."
        FastRetry = "não completar."
        RestartRequired = "completa. Uma reinicialização é necessária."
        Start = "começou a."
    }
    BlockExecution = @{
        Message = "Lançar este aplicativo está temporariamente bloqueado para que possa concluir uma operação de instalação."
    }
    ClosePrompt = @{
        ButtonClose = "Fechar Programas"
        ButtonContinue = "Continuar"
        ButtonContinueTooltip = "Selecione `"Continuar`" somente após fechar a(s) aplicação(ões) listada(s) abaixo."
        ButtonDefer = "Adiar"
        CountdownMessage = "NOTA: O programa será fechado automaticamente em:"
        Message = "Programas de seguir devem ser fechados antes que a instalação possa prosseguir.`n`nPor favor, guarde o seu trabalho, feche os programas e em seguida continuar. Como alternativa, salve seu trabalho e clique em `"Fechar Programas`"."
    }
    DeferPrompt = @{
        Deadline = "Prazo:"
        ExpiryMessage = "Você pode optar por adiar a instalação até que expire o diferimento:"
        RemainingDeferrals = "Restantes diferimentos:"
        WarningMessage = "Uma vez que o diferimento expirou, você já não terá a opção de adiar a."
        WelcomeMessage = "O seguinte aplicativo está prestes a ser instalado:"
    }
    DeploymentType = @{
        Install = "Instalação"
        Repair = "Reparação"
        Uninstall = "Desinstalação"
    }
    DiskSpace = @{
        Message = "Você não tem espaço em disco suficiente para concluir a instalação de:`n{0}`n`nEspaço necessário: {1}MB`nEspaço disponível: {2}MB`n`nPor favor, espaço livre em disco suficiente, a fim de prosseguir com a instalação."
    }
    Progress = @{
        MessageInstall = "Instalação em andamento. Por favor aguarde..."
        MessageInstallDetail = "Esta janela fechar-se-á automaticamente quando a instalação estiver concluída."
        MessageRepair = "Reparação em andamento. Por favor aguarde..."
        MessageRepairDetail = "Esta janela fechar-se-á automaticamente quando a reparação estiver concluída."
        MessageUninstall = "Desinstalação em andamento. Por favor aguarde..."
        MessageUninstallDetail = "Esta janela fechar-se-á automaticamente quando a desinstalação estiver concluída."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimizar"
        ButtonRestartNow = "Reinicie Agora"
        Message = "Em ordem para completar a instalação, você deve reiniciar seu computador."
        MessageRestart = "Seu computador será reiniciado automaticamente no final da contagem regressiva."
        MessageTime = "Por favor, salve o trabalho e reiniciar no tempo alocado."
        TimeRemaining = "Tempo restante:"
        Title = "Reinicialização Necessária"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "O {0} continuará automaticamente em:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - Aplicação {0}'
            DialogMessage = 'Guarde o seu trabalho antes de continuar, pois as aplicações seguintes serão encerradas automaticamente.'
            DialogMessageNoProcesses = 'Selecione Instalar para continuar com a instalação. Se ainda tiver algum adiamento, também pode optar por adiar a instalação.'
            ButtonDeferRemaining = 'permanecer'
            ButtonLeftText = 'Adiar'
            ButtonRightText = 'Fechar aplicações e instalar'
            ButtonRightTextNoProcesses = 'Instalar'
        }
    }
}
