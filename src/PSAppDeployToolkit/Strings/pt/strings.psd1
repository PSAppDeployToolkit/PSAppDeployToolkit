@{
    BalloonTip = @{
        Start = @{
            Install = 'Instalação iniciada.'
            Repair = 'Reparação iniciada.'
            Uninstall = 'Desinstalação iniciada.'
        }
        Complete = @{
            Install = 'Instalação concluída.'
            Repair = 'Reparação concluída.'
            Uninstall = 'Desinstalação concluída.'
        }
        RestartRequired = @{
            Install = 'Instalação concluída. É necessário reiniciar.'
            Repair = 'Reparação concluída. É necessário reiniciar.'
            Uninstall = 'Desinstalação concluída. É necessário reiniciar.'
        }
        FastRetry = @{
            Install = 'Instalação não concluída.'
            Repair = 'Reparação não concluída.'
            Uninstall = 'Desinstalação não concluída.'
        }
        Error = @{
            Install = 'A instalação falhou.'
            Repair = 'Falha na reparação.'
            Uninstall = 'Falha na desinstalação.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'O arranque desta aplicação foi temporariamente bloqueado para que uma operação de instalação possa ser concluída.'
            Repair = 'O arranque desta aplicação foi temporariamente bloqueado para que uma operação de reparação possa ser concluída.'
            Uninstall = 'O lançamento desta aplicação foi temporariamente bloqueado para que uma operação de desinstalação possa ser concluída.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalação da Aplicação'
            Repair = '{Toolkit\CompanyName} - Reparação da Aplicação'
            Uninstall = '{Toolkit\CompanyName} - Desinstalação da Aplicação'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Não tem espaço em disco suficiente para completar a instalação de:`n{0}`n`nEspaço necessário: {1}MB`n`nEspaço disponível: {2}MB`n`nPor favor, liberte espaço suficiente no disco para poder prosseguir com a instalação."
            Repair = "Não tem espaço suficiente no disco para concluir a reparação de:`n{0}`n`n`nEspaço necessário: {1}MB`n`nEspaço disponível: {2}MB`n`nPor favor, liberte espaço suficiente no disco para poder prosseguir com a reparação."
            Uninstall = "Não tem espaço suficiente em disco para concluir a desinstalação de:`n{0}`n`n`nEspaço necessário: {1}MB`n`nEspaço disponível: {2}MB`n`nPor favor, liberte espaço suficiente no disco para prosseguir com a desinstalação."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalação da Aplicação'
            Repair = '{Toolkit\CompanyName} - Reparação da Aplicação'
            Uninstall = '{Toolkit\CompanyName} - Desinstalação da Aplicação'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Instalação em curso. Aguarde…'
            Repair = 'Reparação em curso. Aguarde…'
            Uninstall = 'Desinstalação em curso. Aguarde…'
        }
        MessageDetail = @{
            Install = 'Esta janela fechar-se-á automaticamente quando a instalação estiver concluída.'
            Repair = 'Esta janela fechar-se-á automaticamente quando a reparação estiver concluída.'
            Uninstall = 'Esta janela fechar-se-á automaticamente quando a desinstalação estiver concluída.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalação de aplicações'
            Repair = '{Toolkit\CompanyName} - Reparação da Aplicação'
            Uninstall = '{Toolkit\CompanyName} - Desinstalação da aplicação'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimizar'
        ButtonRestartNow = 'Reiniciar agora'
        Message = @{
            Install = 'Para que a instalação seja concluída, tem de reiniciar o computador.'
            Repair = 'Para que a reparação seja concluída, tem de reiniciar o computador.'
            Uninstall = 'Para que a desinstalação seja concluída, tem de reiniciar o computador.'
        }
        CustomMessage = ''
        MessageRestart = 'O seu computador será reiniciado automaticamente no final da contagem decrescente.'
        MessageTime = 'Guarde o seu trabalho e reinicie dentro do tempo previsto.'
        TimeRemaining = 'Tempo restante:'
        Title = 'É necessário reiniciar'
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalação da Aplicação'
            Repair = '{Toolkit\CompanyName} - Reparação da Aplicação'
            Uninstall = '{Toolkit\CompanyName} - Desinstalação da Aplicação'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'A seguinte aplicação está prestes a ser instalada:'
                Repair = 'A seguinte aplicação está prestes a ser reparada:'
                Uninstall = 'A seguinte aplicação está prestes a ser desinstalada:'
            }
            CloseAppsMessage = @{
                Install = "Os seguintes programas devem ser fechados antes que a instalação possa prosseguir.`n`nPor favor, guarde o seu trabalho, feche os programas e depois continue. Em alternativa, guarde o seu trabalho e clique em `“Fechar programas`”."
                Repair = "Os seguintes programas devem ser fechados para que a reparação possa prosseguir.`n`nPor favor, guarde o seu trabalho, feche os programas e depois continue. Em alternativa, guarde o seu trabalho e clique em `“Fechar programas`”."
                Uninstall = "Os seguintes programas devem ser fechados antes que a desinstalação possa prosseguir.`n`nPor favor, guarde o seu trabalho, feche os programas e depois continue. Em alternativa, guarde o seu trabalho e clique em `“Fechar programas`”."
            }
            ExpiryMessage = @{
                Install = 'Pode optar por adiar a instalação até que o adiamento expire:'
                Repair = 'Pode optar por adiar a reparação até que o adiamento expire:'
                Uninstall = 'Pode optar por adiar a desinstalação até o prazo de adiamento expirar:'
            }
            DeferralsRemaining = 'Restantes adiamentos:'
            DeferralDeadline = 'Prazo:'
            ExpiryWarning = 'Quando o adiamento expirar, deixará de ter a opção de adiar.'
            CountdownDefer = @{
                Install = 'A instalação continuará automaticamente em:'
                Repair = 'A reparação continuará automaticamente em:'
                Uninstall = 'A desinstalação continuará automaticamente em:'
            }
            CountdownClose = 'NOTA: O(s) programa(s) será(ão) automaticamente encerrado(s) em:'
            ButtonClose = 'Fechar &Programas'
            ButtonDefer = '&Deferir'
            ButtonContinue = '&Continuar'
            ButtonContinueTooltip = 'Selecione “Continuar” apenas depois de fechar a(s) aplicação(ões) acima indicada(s).'
        }
        Fluent = @{
            DialogMessage = 'Por favor, guarde o seu trabalho antes de continuar, pois as seguintes aplicações serão fechadas automaticamente.'
            DialogMessageNoProcesses = @{
                Install = 'Selecione Install para continuar com a instalação. Se ainda tiver alguns adiamentos, também pode optar por adiar a instalação.'
                Repair = 'Selecione Reparar para continuar com a reparação. Se ainda tiver alguns adiamentos, também pode optar por adiar a reparação.'
                Uninstall = 'Selecione Desinstalar para continuar com a desinstalação. Se ainda tiver alguns adiamentos, também pode optar por adiar a desinstalação.'
            }
            AutomaticStartCountdown = 'Contagem decrescente de início automático'
            DeferralsRemaining = 'Diferimentos Restantes'
            DeferralDeadline = 'Prazo de Adiamento'
            ButtonLeftText = @{
                Install = 'Fechar aplicações e instalar'
                Repair = 'Fechar aplicações e reparar'
                Uninstall = 'Fechar aplicações e desinstalar'
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Instalar'
                Repair = 'Reparar'
                Uninstall = 'Desinstalar'
            }
            ButtonRightText = 'Deferir'
            Subtitle = @{
                Install = '{Toolkit\CompanyName} - Instalação da Aplicação'
                Repair = '{Toolkit\CompanyName} - Reparação da Aplicação'
                Uninstall = '{Toolkit\CompanyName} - Desinstalação da Aplicação'
            }
        }
        CustomMessage = ''
    }
}
