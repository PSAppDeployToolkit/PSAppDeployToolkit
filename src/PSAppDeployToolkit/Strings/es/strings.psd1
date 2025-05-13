@{
    BalloonTip = @{
        Start = @{
            Install = 'Instalación iniciada.'
            Repair = 'Reparación iniciada.'
            Uninstall = 'Desinstalación iniciada.'
        }
        Complete = @{
            Install = 'Instalación completada.'
            Repair = 'Reparación completada.'
            Uninstall = 'Desinstalación completada.'
        }
        RestartRequired = @{
            Install = 'Instalación completada. Se requiere un reinicio.'
            Repair = 'Reparación completada. Se requiere un reinicio.'
            Uninstall = 'Desinstalación completada. Se requiere un reinicio.'
        }
        FastRetry = @{
            Install = 'Instalación no completada.'
            Repair = 'Reparación no completada.'
            Uninstall = 'Desinstalación no completada.'
        }
        Error = @{
            Install = 'Instalación fallida.'
            Repair = 'Reparación fallida.'
            Uninstall = 'Falló la desinstalación.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'Se ha bloqueado temporalmente el inicio de esta aplicación para que pueda completarse una operación de instalación.'
            Repair = 'Se ha bloqueado temporalmente el inicio de esta aplicación para que pueda completarse una operación de reparación'.
            Uninstall = 'Se ha bloqueado temporalmente el inicio de esta aplicación para que pueda completarse una operación de desinstalación.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalación de la aplicación'
            Repair = 'PSAppDeployToolkit - Reparación de la aplicación'
            Uninstall = 'PSAppDeployToolkit - Desinstalación de la aplicación'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "No tiene suficiente espacio en disco para completar la instalación de:`n{0}`n`nespacio requerido: {1}MB`nEspacio disponible: {2}MB`n`nPor favor, libere suficiente espacio en disco para poder proceder con la instalación."
            Repair = "No dispone de suficiente espacio en disco para completar la reparación de:`n{0}`n`nespacio necesario: {1}MB`nEspacio disponible: {2}MB`n`nPor favor, libere suficiente espacio en disco para proceder con la reparación."
            Uninstall = "No dispone de suficiente espacio en disco para completar la desinstalación de:`n{0}`n`nespacio necesario: {1}MB`nEspacio disponible: {2}MB`n`nPor favor, libere suficiente espacio en disco para poder proceder con la desinstalación."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalación de App'
            Repair = 'PSAppDeployToolkit - Reparación de la aplicación'
            Uninstall = 'PSAppDeployToolkit - Desinstalación de la aplicación'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Instalación en curso. Por favor espere...'
            Repair = 'Reparación en curso. Por favor espere...'
            Uninstall = 'Desinstalación en curso. Por favor espere...'
        }
        MessageDetail = @{
            Install = 'Esta ventana se cerrará automáticamente cuando finalice la instalación.'
            Repair = 'Esta ventana se cerrará automáticamente cuando finalice la reparación.'
            Uninstall = 'Esta ventana se cerrará automáticamente cuando finalice la desinstalación.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalación de la aplicación'
            Repair = 'PSAppDeployToolkit - Reparación de la aplicación'
            Uninstall = 'PSAppDeployToolkit - Desinstalación de la aplicación'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimizar'
        ButtonRestartNow = 'Reiniciar ahora'
        Message = @{
            Install = 'Para que la instalación se complete, debe reiniciar su ordenador'.
            Repair = 'Para que la reparación se complete, debe reiniciar su ordenador.'
            Uninstall = 'Para que la desinstalación se complete, debe reiniciar su ordenador.'
        }
        CustomMessage = ''
        MessageRestart = 'Su ordenador se reiniciará automáticamente al final de la cuenta atrás.'
        MessageTime = 'Por favor, guarde su trabajo y reinicie dentro del tiempo asignado.'
        TimeRemaining = 'Tiempo restante:'
        Title = 'Es necesario reiniciar'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Instalación de la aplicación'
            Repair = 'PSAppDeployToolkit - Reparación de la aplicación'
            Uninstall = 'PSAppDeployToolkit - Desinstalación de la aplicación'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'La siguiente aplicación está a punto de ser instalada:'
                Repair = 'La siguiente aplicación está a punto de ser reparada:'
                Uninstall = 'La siguiente aplicación está a punto de ser desinstalada:'
            }
            CloseAppsMessage = @{
                Install = "Los siguientes programas deben cerrarse antes de que la instalación pueda continuar.`n`nPor favor, guarde su trabajo, cierre los programas y continúe. Alternativamente, guarde su trabajo y haga clic en «Cerrar programas»."
                Repair = "Los siguientes programas deben cerrarse antes de proceder a la reparación.`n`nPor favor, guarde su trabajo y haga clic en «Cerrar programas»."
                Uninstall = "Los siguientes programas deben cerrarse antes de proceder a la desinstalación.`n`nPor favor, guarde su trabajo, cierre los programas y continúe. Alternativamente, guarde su trabajo y haga clic en «Cerrar Programas»."
            }
            ExpiryMessage = @{
                Install = 'Puede elegir aplazar la instalación hasta que expire el aplazamiento:'
                Repair = 'Puede elegir aplazar la reparación hasta que expire el aplazamiento:'
                Uninstall = 'Puede elegir aplazar la desinstalación hasta que expire el aplazamiento:'
            }
            DeferralsRemaining = 'Aplazamientos restantes:'
            DeferralDeadline = 'Fecha límite:'
            ExpiryWarning = 'Una vez que haya expirado el aplazamiento, ya no tendrá la opción de aplazarlo.'
            CountdownDefer = @{
                Install = 'La instalación continuará automáticamente en:'
                Repair = 'La reparación continuará automáticamente en:'
                Uninstall = 'La desinstalación continuará automáticamente en:'
            }
            CountdownClose = 'NOTA: El programa o programas se cerrarán automáticamente en:'
            ButtonClose = 'Cerrar &Programas'
            ButtonDefer = '&Defer'
            ButtonContinue = '&Continuar'
            ButtonContinueTooltip = 'Sólo seleccione «Continuar» después de cerrar la(s) aplicación(es) arriba indicada(s).'
        }
        Fluent = @{
            DialogMessage = 'Por favor, guarde su trabajo antes de continuar ya que las siguientes aplicaciones se cerrarán automáticamente.'
            DialogMessageNoProcesses = @{
                Install = 'Por favor, seleccione Instalar para continuar con la instalación. Si le queda algún aplazamiento, también puede optar por retrasar la instalación.'
                Repair = 'Por favor, seleccione Reparar para continuar con la reparación. Si le queda algún aplazamiento, también puede optar por retrasar la reparación.'
                Uninstall = 'Por favor, seleccione Desinstalar para continuar con la desinstalación. Si le queda algún aplazamiento, también puede optar por retrasar la desinstalación.'
            }
            AutomaticStartCountdown = 'Cuenta regresiva de inicio automático'
            DeferralsRemaining = 'Aplazamientos restantes'
            DeferralDeadline = 'Fecha límite de aplazamiento'
            ButtonLeftText = 'Aplazar'
            ButtonRightText = @{
                Install = 'Cerrar Aplicaciones e Instalar'
                Repair = 'Cerrar Aplicaciones y Reparar'
                Uninstall = 'Cerrar Aplicaciones y Desinstalar'
            }
            ButtonRightTextNoProcesos = @{
                Install = 'Instalar'
                Repair = 'Reparar'
                Uninstall = 'Desinstalar'
            }
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Instalación de la aplicación'
                Repair = 'PSAppDeployToolkit - Reparación de la aplicación'
                Uninstall = 'PSAppDeployToolkit - Desinstalación de App'
            }
        }
        CustomMessage = ''
    }
}
