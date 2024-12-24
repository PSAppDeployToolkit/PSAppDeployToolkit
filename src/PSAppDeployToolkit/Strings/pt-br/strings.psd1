@{
    BalloonText = @{
        Complete = @{
            Install = 'Instalação concluída.'
            Repair = 'Reparo concluído.'
            Uninstall = 'Desinstalação concluída.'
        }
        Error = @{
            Install = 'Falha na instalação.'
            Repair = 'Falha no reparo.'
            Uninstall = 'Falha na desinstalação.'
        }
        FastRetry = @{
            Install = 'Instalação não concluída.'
            Repair = 'Reparo não concluído.'
            Uninstall = 'A desinstalação não foi concluída.'
        }
        RestartRequired = @{
            Install = 'Instalação concluída. É necessária uma reinicialização.'
            Repair = 'Reparo concluído. É necessária uma reinicialização.'
            Uninstall = 'Desinstalação concluída. É necessária uma reinicialização.'
        }
        Start = @{
            Install = 'Instalação iniciada.'
            Repair = 'Reparo iniciado.'
            Uninstall = 'Desinstalação iniciada.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = 'A inicialização deste aplicativo foi temporariamente bloqueada para que uma operação de instalação possa ser concluída.'
            Repair = 'A inicialização deste aplicativo foi temporariamente bloqueada para que uma operação de reparo possa ser concluída.'
            Uninstall = 'A inicialização deste aplicativo foi temporariamente bloqueada para que uma operação de desinstalação possa ser concluída.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalação do Aplicativo'
            Repair = 'PSAppDeployToolkit - Reparo do Aplicativo'
            Uninstall = 'PSAppDeployToolkit - Desinstalação do Aplicativo'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "O senhor não tem espaço em disco suficiente para concluir a instalação de:`n{0}`n`nEspaço necessário: {1}MB`nEspaço disponível: {2}MB`n`nPor favor, libere espaço suficiente em disco para prosseguir com a instalação."
            Reparo = "O senhor não tem espaço em disco suficiente para concluir o reparo de:`n{0}`n`nEspaço necessário: {1}MB`nEspaço disponível: {2}MB`n`nPor favor, libere espaço suficiente em disco para prosseguir com o reparo."
            Uninstall = "O senhor não tem espaço em disco suficiente para concluir a desinstalação de:`n{0}`n`nEspaço necessário: {1}MB`nEspaço disponível: {2}MB`n`nPor favor, libere espaço suficiente em disco para prosseguir com a desinstalação."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Instalação em andamento. Por favor, aguarde...'
            Repair = 'Reparo em andamento. Aguarde...'
            Uninstall = 'Desinstalação em andamento. Aguarde...'
        }
        MessageDetail = @{
            Install = 'Esta janela se fechará automaticamente quando a instalação for concluída.'
            Repair = 'Esta janela se fechará automaticamente quando o reparo for concluído.'
            Uninstall = 'Esta janela se fechará automaticamente quando a desinstalação for concluída.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalação do Aplicativo'
            Repair = 'PSAppDeployToolkit - Reparo do Aplicativo'
            Uninstall = 'PSAppDeployToolkit - Desinstalação do Aplicativo'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalação do Aplicativo'
            Repair = 'PSAppDeployToolkit - Reparo do Aplicativo'
            Uninstall = 'PSAppDeployToolkit - Desinstalação do Aplicativo'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimizar'
        ButtonRestartNow = 'Reiniciar Agora'
        Message = @{
            Install = 'Para que a instalação seja concluída, o senhor deve reiniciar o computador.'
            Repair = 'Para que o reparo seja concluído, o senhor deve reiniciar o computador.'
            Uninstall = 'Para que a desinstalação seja concluída, o senhor deve reiniciar o computador.'
        }
        MessageRestart = 'Seu computador será reiniciado automaticamente ao final da contagem regressiva.'
        MessageTime = 'Salve seu trabalho e reinicie dentro do tempo alocado.'
        TimeRemaining = 'Tempo restante:'
        Title = 'Reiniciar Necessário'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalação do Aplicativo'
            Repair = 'PSAppDeployToolkit - Reparo do Aplicativo'
            Uninstall = 'PSAppDeployToolkit - Desinstalação do Aplicativo'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Fechar &Programas'
                ButtonContinue = '&“Continuar”'
                ButtonContinueTooltip = 'Somente selecione “Continuar” após fechar os aplicativos listados acima.'
                ButtonDefer = '&Adiar'
                CountdownMessage = 'OBSERVAÇÃO: O(s) programa(s) será(ão) fechado(s) automaticamente em:'
                Message = @{
                    Install = "Os programas a seguir devem ser fechados antes que a instalação possa prosseguir.`n`nPor favor, salve seu trabalho, feche os programas e depois continue. Como alternativa, salve seu trabalho e clique em `“Fechar programas`”."
                    Repair = "Os programas a seguir devem ser fechados para que o reparo possa prosseguir.`n`nPor favor, salve seu trabalho, feche os programas e continue. Como alternativa, salve seu trabalho e clique em `“Fechar programas`”."
                    Uninstall = "Os seguintes programas devem ser fechados para que a desinstalação possa prosseguir.`n`nPor favor, salve seu trabalho, feche os programas e continue. Como alternativa, salve seu trabalho e clique em `“Fechar programas`”."
                }
            }
            Defer = @{
                Deadline = 'Prazo final:'
                ExpiryMessage = @{
                    Install = 'O senhor pode optar por adiar a instalação até que o adiamento expire:'
                    Repair = 'O senhor pode optar por adiar o reparo até que o adiamento expire:'
                    Uninstall = 'O senhor pode optar por adiar a desinstalação até que o prazo expire:'
                }
                RemainingDeferrals = 'Deferimentos restantes:'
                WarningMessage = 'Quando o adiamento expirar, o senhor não terá mais a opção de adiar.'
                WelcomeMessage = @{
                    Install = 'O aplicativo a seguir está prestes a ser instalado:'
                    Repair = 'O aplicativo a seguir está prestes a ser reparado:'
                    Uninstall = 'O aplicativo a seguir está prestes a ser desinstalado:'
                }
            }
            CountdownMessage = @{
                Install = 'A instalação continuará automaticamente em:'
                Repair = 'O reparo continuará automaticamente em:'
                Uninstall = 'A desinstalação continuará automaticamente em:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Instalação do Aplicativo'
                Repair = 'PSAppDeployToolkit - Reparo do Aplicativo'
                Uninstall = 'PSAppDeployToolkit - Desinstalação do Aplicativo'
            }
            DialogMessage = 'Por favor, salve seu trabalho antes de continuar, pois os seguintes aplicativos serão fechados automaticamente.'
            DialogMessageNoProcesses = @{
                Install = 'Selecione Install para continuar com a instalação. Se o senhor tiver algum adiamento restante, também poderá optar por adiar a instalação.'
                Repair = 'Selecione Repair para continuar com o reparo. Se ainda houver adiamentos, o senhor também pode optar por adiar o reparo.'
                Uninstall = 'Selecione Desinstalar para continuar com a desinstalação. Se houver algum adiamento restante, o senhor também pode optar por adiar a desinstalação.'
            }
            ButtonDeferRemaining = 'permanecer'
            ButtonLeftText = 'Deferir'
            ButtonRightText = @{
                Install = 'Fechar Aplicativos e Instalar'
                Repair = 'Fechar Aplicativos & Reparar'
                Uninstall = 'Fechar Aplicativos e Desinstalar'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Instalar'
                Repair = 'Reparar'
                Uninstall = 'Desinstalar'
            }
        }
    }
}
