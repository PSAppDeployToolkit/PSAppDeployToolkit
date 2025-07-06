@{
    BalloonTip = @{
        Start = @{
            Install = 'Instalação iniciada.'
            Repair = 'Reparo iniciado.'
            Uninstall = 'Desinstalação iniciada.'
        }
        Complete = @{
            Install = 'Instalação concluída.'
            Repair = 'Reparo concluído.'
            Uninstall = 'Desinstalação concluída.'
        }
        RestartRequired = @{
            Install = 'Instalação concluída. É necessário reiniciar.'
            Repair = 'Reparo concluído. É necessário reiniciar.'
            Uninstall = 'Desinstalação concluída. É necessário reiniciar.'
        }
        FastRetry = @{
            Install = 'Instalação não concluída.'
            Repair = 'Reparo não concluído.'
            Uninstall = 'Desinstalação não concluída.'
        }
        Error = @{
            Install = 'A instalação falhou.'
            Repair = 'O reparo falhou.'
            Uninstall = 'A desinstalação falhou.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'A inicialização deste aplicativo foi temporariamente bloqueada para que uma operação de instalação possa ser concluída.'
            Repair = 'A inicialização deste aplicativo foi temporariamente bloqueada para que uma operação de reparo possa ser concluído.'
            Uninstall = 'A inicialização deste aplicativo foi temporariamente bloqueada para que uma operação de desinstalação possa ser concluída.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalação do Aplicativo'
            Repair = '{Toolkit\CompanyName} - Reparo do Aplicativo'
            Uninstall = '{Toolkit\CompanyName} - Desinstalação do Aplicativo'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Não há espaço em disco suficiente para concluir a instalação de:`n{0}`n`nEspaço necessário: {1}MB`nEspaço disponível: {2}MB`n`nPor favor, libere espaço suficiente em disco para prosseguir com a instalação."
            Reparo = "Não há espaço em disco suficiente para concluir o reparo de:`n{0}`n`nEspaço necessário: {1}MB`nEspaço disponível: {2}MB`n`nPor favor, libere espaço suficiente em disco para prosseguir com a reparação."
            Uninstall = "Não há espaço em disco suficiente para concluir a desinstalação de:`n{0}`n`nEspaço necessário: {1}MB`nEspaço disponível: {2}MB`n`nPor favor, libere espaço suficiente em disco para prosseguir com a desinstalação."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalação do Aplicativo'
            Repair = '{Toolkit\CompanyName} - Reparo do Aplicativo'
            Uninstall = '{Toolkit\CompanyName} - Desinstalação do Aplicativo'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Instalação em andamento. Por favor, aguarde…'
            Repair = 'Reparo em andamento. Por favor, aguarde…'
            Uninstall = 'Desinstalação em andamento. Aguarde…'
        }
        MessageDetail = @{
            Install = 'Esta janela se fechará automaticamente quando a instalação for concluída.'
            Repair = 'Esta janela se fechará automaticamente quando o reparo for concluído.'
            Uninstall = 'Esta janela se fechará automaticamente quando a desinstalação for concluída.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalação do Aplicativo'
            Repair = '{Toolkit\CompanyName} - Reparo do Aplicativo'
            Uninstall = '{Toolkit\CompanyName} - Desinstalação do Aplicativo'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimizar'
        ButtonRestartNow = 'Reiniciar Agora'
        Message = @{
            Install = 'Para que a instalação seja concluída, é preciso reiniciar o computador.'
            Repair = 'Para que o reparo seja concluído, é preciso reiniciar o computador.'
            Uninstall = 'Para que a desinstalação seja concluída, é preciso reiniciar o computador.'
        }
        CustomMessage = ''
        MessageRestart = 'Seu computador será reiniciado automaticamente ao final da contagem regressiva.'
        MessageTime = 'Salve seu trabalho e reinicie o computador dentro do tempo estipulado.'
        TimeRemaining = 'Tempo restante:'
        Title = 'É necessário reiniciar'
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Instalação do Aplicativo'
            Repair = '{Toolkit\CompanyName} - Reparo do Aplicativo'
            Uninstall = '{Toolkit\CompanyName} - Desinstalação do Aplicativo'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'O seguinte aplicativo está prestes a ser instalado:'
                Repair = 'O seguinte aplicativo está prestes a ser reparado:'
                Uninstall = 'O seguinte aplicativo está prestes a ser desinstalado:'
            }
            CloseAppsMessage = @{
                Install = "Os seguintes programas devem ser fechados antes que a instalação possa prosseguir.`n`nPor favor, salve seu trabalho, feche os programas e depois continue. Como alternativa, salve seu trabalho e clique em `“Fechar programas`”."
                Repair = "Os seguintes programas devem ser fechados antes que o reparo possa prosseguir.`n`nPor favor, salve seu trabalho, feche os programas e continue. Como alternativa, salve seu trabalho e clique em `“Fechar programas`”."
                Uninstall = "Os seguintes programas devem ser fechados antes que a desinstalação possa prosseguir.`n`nPor favor, salve seu trabalho, feche os programas e continue. Como alternativa, salve seu trabalho e clique em `“Fechar programas`”."
            }
            ExpiryMessage = @{
                Install = 'Você pode optar por adiar a instalação até que o prazo de adiamento expire:'
                Repair = 'Você pode optar por adiar o reparo até que o prazo de adiamento expire:'
                Uninstall = 'Você pode optar por adiar a desinstalação até que o prazo de adiamento expire:'
            }
            DeferralsRemaining = 'Adiamentos restantes:'
            DeferralDeadline = 'Prazo final:'
            ExpiryWarning = 'Quando o adiamento expirar, você não terá mais a opção de adiar.'
            CountdownDefer = @{
                Install = 'A instalação continuará automaticamente em:'
                Repair = 'O reparo continuará automaticamente em:'
                Uninstall = 'A desinstalação continuará automaticamente em:'
            }
            CountdownClose = @{
                Install = 'OBSERVAÇÃO: O(s) programa(s) será(ão) fechado(s) automaticamente em:'
                Repair = 'OBSERVAÇÃO: O(s) programa(s) será(ão) fechado(s) automaticamente em:'
                Uninstall = 'OBSERVAÇÃO: O(s) programa(s) será(ão) fechado(s) automaticamente em:'
            }
            ButtonClose = 'Fechar &Programas'
            ButtonDefer = '&Adiar'
            ButtonContinue = '&“Continuar”'
            ButtonContinueTooltip = 'Somente selecione “Continuar” após fechar os aplicativos listados acima.'
        }
        Fluent = @{
            DialogMessage = @{
                Install = 'Por favor, salve seu trabalho antes de continuar, pois os seguintes aplicativos serão fechados automaticamente.'
                Repair = 'Por favor, salve seu trabalho antes de continuar, pois os seguintes aplicativos serão fechados automaticamente.'
                Uninstall = 'Por favor, salve seu trabalho antes de continuar, pois os seguintes aplicativos serão fechados automaticamente.'
            }
            DialogMessageNoProcesses = @{
                Install = 'Selecione Instalar para continuar com a instalação. Se você tiver algum adiamento restante, também poderá optar por adiar a instalação.'
                Repair = 'Selecione Reparar para continuar com o reparo. Se ainda houver adiamentos, o senhor também pode optar por adiar o reparo.'
                Uninstall = 'Selecione Desinstalar para continuar com a desinstalação. Se houver algum adiamento restante, você também pode optar por adiar a desinstalação.'
            }
            AutomaticStartCountdown = 'Contagem regressiva para início automático'
            DeferralsRemaining = 'Adiamentos restantes'
            DeferralDeadline = 'Prazo final do adiamento'
            ButtonLeftText = @{
                Install = 'Fechar Aplicativos e Instalar'
                Repair = 'Fechar Aplicativos e Reparar'
                Uninstall = 'Fechar Aplicativos e Desinstalar'
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Instalar'
                Repair = 'Reparar'
                Uninstall = 'Desinstalar'
            }
            ButtonRightText = 'Adiar'
            Subtitle = @{
                Install = '{Toolkit\CompanyName} - Instalação do Aplicativo'
                Repair = '{Toolkit\CompanyName} - Reparo do Aplicativo'
                Uninstall = '{Toolkit\CompanyName} - Desinstalação do Aplicativo'
            }
        }
        CustomMessage = ''
    }
}
