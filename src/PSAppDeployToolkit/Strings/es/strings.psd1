@{
    BalloonText = @{
        Complete = "completada."
        Error = "fallida."
        FastRetry = "incompleta."
        RestartRequired = "completada. Se requiere un reinicio."
        Start = "iniciada."
    }
    BlockExecution = @{
        Message = "La ejecución de esta aplicación se ha bloqueado temporalmente para que se pueda completar una operación de instalación."
    }
    ClosePrompt = @{
        ButtonClose = "Cerrar Programas"
        ButtonContinue = "Continuar"
        ButtonContinueTooltip = "Solo seleccione `"Continuar`" después de cerrar la(s) aplicacion(es) de la lista."
        ButtonDefer = "Aplazar"
        CountdownMessage = "NOTA: El/los programa(s) se cerrará(n) automáticamente en:"
        Message = "Los siguientes programas deben estar cerrados antes de que la instalación pueda continuar.`n`nGuarde su trabajo, cierre los programas y luego continúe.`nAlternativamente, guarde su trabajo y haga clic en `"Cerrar programas`"."
    }
    DeferPrompt = @{
        Deadline = "Fecha tope:"
        ExpiryMessage = "Puede optar por aplazar la instalación hasta que expire el aplazamiento:"
        RemainingDeferrals = "Aplazamientos restantes:"
        WarningMessage = "Una vez vencido el aplazamiento, ya no tendrá la opción de aplazar."
        WelcomeMessage = "La siguiente aplicación está a punto de instalarse:"
    }
    DeploymentType = @{
        Install = "Instalación"
        Repair = "Reparación"
        Uninstall = "Desinstalación"
    }
    DiskSpace = @{
        Message = "El espacio en disco es insuficiente para completar la instalación de:`n{0}`n`nEspacio requerido: {1}MB`nEspacio disponible: {2}MB`n`nPor favor, libere suficiente espacio en disco para continuar con la instalación."
    }
    Progress = @{
        MessageInstall = "Instalación en curso. Por favor, espere..."
        MessageInstallDetail = "Esta ventana se cerrará automáticamente cuando finalice la instalación."
        MessageRepair = "Reparación en curso. Por favor, espere..."
        MessageRepairDetail = "Esta ventana se cerrará automáticamente cuando finalice la reparación."
        MessageUninstall = "Desinstalación en curso. Por favor, espere..."
        MessageUninstallDetail = "Esta ventana se cerrará automáticamente cuando finalice la desinstalación."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimizar"
        ButtonRestartNow = "Reiniciar Ahora"
        Message = "Para que la instalación se complete, debe reiniciar su equipo."
        MessageRestart = "El equipo se reiniciará automáticamente al final de la cuenta regresiva."
        MessageTime = "Por favor guarde su trabajo y reinicie dentro del tiempo asignado."
        TimeRemaining = "Tiempo restante:"
        Title = "Reinicio Requerido"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "La {0} continuará automáticamente en:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - Aplicación {0}'
            DialogMessage = 'Guarde su trabajo antes de continuar, ya que las siguientes aplicaciones se cerrarán automáticamente.'
            DialogMessageNoProcesses = 'Seleccione Instalar para continuar con la instalación. Si le queda algún aplazamiento, también puede optar por retrasar la instalación.'
            ButtonDeferRemaining = 'permanezca en'
            ButtonLeftText = 'Aplazar'
            ButtonRightText = 'Cerrar aplicaciones e instalar'
            ButtonRightTextNoProcesses = 'Instale'
        }
    }
}
