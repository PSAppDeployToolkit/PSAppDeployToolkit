@{
    BalloonText = @{
        Complete = @{
            Install = 'Instalação concluída.'
            Repair = 'Reparação concluída.'
            Uninstall = 'Desinstalação concluída.'
        }
        Error = @{
            Install = 'A instalação falhou.'
            Repair = 'Falha na reparação.'
            Uninstall = 'Falha na desinstalação.'
        }
        FastRetry = @{
            Install = 'Instalação não concluída.'
            Repair = 'Reparação não concluída.'
            Uninstall = 'Desinstalação não concluída.'
        }
        RestartRequired = @{
            Install = 'Instalação concluída. É necessário reiniciar.'
            Repair = 'Reparação concluída. É necessário reiniciar.'
            Uninstall = 'Desinstalação concluída. É necessário reiniciar.'
        }
        Start = @{
            Install = 'Instalação iniciada.'
            Repair = 'Reparação iniciada.'
            Uninstall = 'Desinstalação iniciada.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = 'O arranque desta aplicação foi temporariamente bloqueado para que uma operação de instalação possa ser concluída.'
            Repair = 'O arranque desta aplicação foi temporariamente bloqueado para que uma operação de reparação possa ser concluída.'
            Uninstall = 'O lançamento desta aplicação foi temporariamente bloqueado para que uma operação de desinstalação possa ser concluída.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalação da Aplicação'
            Repair = 'PSAppDeployToolkit - Reparação da Aplicação'
            Uninstall = 'PSAppDeployToolkit - Desinstalação da Aplicação'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "Não tem espaço em disco suficiente para completar a instalação de:`n{0}`n`nEspaço necessário: {1}MB`n`nEspaço disponível: {2}MB`n`nPor favor, liberte espaço suficiente no disco para poder prosseguir com a instalação."
            Repair = "Não tem espaço suficiente no disco para concluir a reparação de:`n{0}`n`n`nEspaço necessário: {1}MB`n`nEspaço disponível: {2}MB`n`nPor favor, liberte espaço suficiente no disco para poder prosseguir com a reparação."
            Uninstall = "Não tem espaço suficiente em disco para concluir a desinstalação de:`n{0}`n`n`nEspaço necessário: {1}MB`n`nEspaço disponível: {2}MB`n`nPor favor, liberte espaço suficiente no disco para prosseguir com a desinstalação."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Instalação em curso. Aguarde...'
            Repair = 'Reparação em curso. Aguarde...'
            Uninstall = 'Desinstalação em curso. Aguarde...'
        }
        MessageDetail = @{
            Install = 'Esta janela fechar-se-á automaticamente quando a instalação estiver concluída.'
            Repair = 'Esta janela fechar-se-á automaticamente quando a reparação estiver concluída.'
            Uninstall = 'Esta janela fechar-se-á automaticamente quando a desinstalação estiver concluída.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalação de aplicações'
            Repair = 'PSAppDeployToolkit - Reparação da Aplicação'
            Uninstall = 'PSAppDeployToolkit - Desinstalação da aplicação'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalação da Aplicação'
            Repair = 'PSAppDeployToolkit - Reparação da Aplicação'
            Uninstall = 'PSAppDeployToolkit - Desinstalação da Aplicação'
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
        MessageRestart = 'O seu computador será reiniciado automaticamente no final da contagem decrescente.'
        MessageTime = 'Guarde o seu trabalho e reinicie dentro do tempo previsto.'
        TimeRemaining = 'Tempo restante:'
        Title = 'É necessário reiniciar'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalação da Aplicação'
            Repair = 'PSAppDeployToolkit - Reparação da Aplicação'
            Uninstall = 'PSAppDeployToolkit - Desinstalação da Aplicação'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Fechar &Programas'
                ButtonContinue = '&Continuar'
                ButtonContinueTooltip = 'Selecione “Continuar” apenas depois de fechar a(s) aplicação(ões) acima indicada(s)'.
                ButtonDefer = '&Deferir'
                CountdownMessage = 'NOTA: O(s) programa(s) será(ão) automaticamente encerrado(s) em:'
                Message = @{
                    Install = "Os seguintes programas devem ser fechados antes que a instalação possa prosseguir.`n`nPor favor, guarde o seu trabalho, feche os programas e depois continue. Em alternativa, guarde o seu trabalho e clique em `“Fechar programas`”."
                    Repair = "Os seguintes programas devem ser fechados para que a reparação possa prosseguir.`n`nPor favor, guarde o seu trabalho, feche os programas e depois continue. Em alternativa, guarde o seu trabalho e clique em `“Fechar programas`”."
                    Uninstall = "Os seguintes programas devem ser fechados antes que a desinstalação possa prosseguir.`n`nPor favor, guarde o seu trabalho, feche os programas e depois continue. Em alternativa, guarde o seu trabalho e clique em `“Fechar programas`”."
                }
            }
            Defer = @{
                Deadline = 'Prazo:'
                ExpiryMessage = @{
                    Install = 'Pode optar por adiar a instalação até que o adiamento expire:'
                    Repair = 'Pode optar por adiar a reparação até que o adiamento expire:'
                    Uninstall = 'Pode optar por adiar a desinstalação até o prazo de adiamento expirar:'
                }
                RemainingDeferrals = 'Restantes adiamentos:'
                WarningMessage = 'Quando o adiamento expirar, deixará de ter a opção de adiar'.
                WelcomeMessage = @{
                    Install = 'A seguinte aplicação está prestes a ser instalada:'
                    Repair = 'A seguinte aplicação está prestes a ser reparada:'
                    Uninstall = 'A seguinte aplicação está prestes a ser desinstalada:'
                }
            }
            CountdownMessage = @{
                Install = 'A instalação continuará automaticamente em:'
                Repair = 'A reparação continuará automaticamente em:'
                Uninstall = 'A desinstalação continuará automaticamente em:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Instalação da Aplicação'
                Repair = 'PSAppDeployToolkit - Reparação da Aplicação'
                Uninstall = 'PSAppDeployToolkit - Desinstalação da Aplicação'
            }
            DialogMessage = 'Por favor, guarde o seu trabalho antes de continuar, pois as seguintes aplicações serão fechadas automaticamente'.
            DialogMessageNoProcesses = @{
                Install = 'Selecione Install para continuar com a instalação. Se ainda tiver alguns adiamentos, também pode optar por adiar a instalação.'
                Repair = 'Selecione Reparar para continuar com a reparação. Se ainda tiver alguns adiamentos, também pode optar por adiar a reparação.'
                Uninstall = 'Selecione Desinstalar para continuar com a desinstalação. Se ainda tiver alguns adiamentos, também pode optar por adiar a desinstalação.'
            }
            ButtonDeferRemaining = 'permanecer'
            ButtonLeftText = 'Deferir'
            ButtonRightText = @{
                Install = 'Fechar aplicações e instalar'
                Repair = 'Fechar aplicações e reparar'
                Uninstall = 'Fechar aplicações e desinstalar'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Instalar'
                Repair = 'Reparar'
                Uninstall = 'Desinstalar'
            }
        }
    }
}
