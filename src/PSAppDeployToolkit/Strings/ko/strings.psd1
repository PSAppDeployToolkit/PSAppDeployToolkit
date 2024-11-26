@{
    BalloonText = @{
        Complete = "완료되었습니다."
        Error = "실패했습니다."
        FastRetry = "완료되지 않았습니다."
        RestartRequired = "완료되었습니다. 재부팅이 필요합니다."
        Start = "시작되었습니다."
    }
    BlockExecution = @{
        Message = "설치 작업을 완료할 수 있도록 응용 프로그램의 시작을 잠시 차단했습니다."
    }
    ClosePrompt = @{
        ButtonClose = "프로그램 종료"
        ButtonContinue = "계속"
        ButtonContinueTooltip = "위에 표시된 응용 프로그램을 종료한 후에만 `"계속`"을 선택하세요."
        ButtonDefer = "연기"
        CountdownMessage = "참고: 프로그램이 자동으로 종료되는 경우:"
        Message = "설치를 계속하려면 다음의 프로그램을 종료해야 합니다.`n`n사용자 작업을 저장하고 프로그램을 종료한 후 계속하세요. 다른 방법으로는 사용자 작업을 저장하고 `"프로그램 종료`"를 클릭하세요."
    }
    DeferPrompt = @{
        Deadline = "마감:"
        ExpiryMessage = "지연 기간이 만료될 때까지 설치를 연기할 수 있습니다:"
        RemainingDeferrals = "남은 지연 기간:"
        WarningMessage = "일단 지연 기간이 만료되면 더 이상 연기할 수 있는 옵션은 없습니다."
        WelcomeMessage = "다음의 응용 프로그램을 설치합니다:"
    }
    DeploymentType = @{
        Install = "설치"
        Repair = "수리"
        Uninstall = "제거"
    }
    DiskSpace = @{
        Message = "다음의 설치 완료를 위해 필요한 디스크 공간이 충분하지 않습니다:`n{0}`n`n필요한 공간: {1}MB`n사용 가능한 공간: {2}MB`n`n설치를 계속하려면 디스크 공간을 충분하게 확보하세요."
    }
    Progress = @{
        MessageInstall = "설치 중입니다. 기다리세요..."
        MessageInstallDetail = "이 창은 설치가 완료되면 자동으로 닫힙니다."
        MessageRepair = "수리 중입니다. 기다리세요..."
        MessageRepairDetail = "이 창은 수리가 완료되면 자동으로 닫힙니다."
        MessageUninstall = "제거 중입니다. 기다리세요..."
        MessageUninstallDetail = "이 창은 제거가 완료되면 자동으로 닫힙니다."
    }
    RestartPrompt = @{
        ButtonRestartLater = "최소화"
        ButtonRestartNow = "지금 다시 시작"
        Message = "설치를 완료하려면 컴퓨터를 다시 시작해야 합니다."
        MessageRestart = "카운트다운이 종료되면 컴퓨터는 자동으로 다시 시작합니다."
        MessageTime = "사용자 작업을 저장하고 지정된 시간 이내에 다시 시작하세요."
        TimeRemaining = "남은 시간:"
        Title = "다시 시작해야 합니다"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0}는 자동으로 계속됩니다:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - 앱 {0}'
            DialogMessage = '다음 애플리케이션은 자동으로 종료되므로 계속하기 전에 작업을 저장해 주세요.'
            DialogMessageNoProcesses = '설치를 계속하려면 설치를 선택하세요. 연기할 항목이 남아 있는 경우 설치를 연기하도록 선택할 수도 있습니다.'
            ButtonDeferRemaining = '남아있음'
            ButtonLeftText = '연기하다'
            ButtonRightText = '앱 닫기 및 설치'
            ButtonRightTextNoProcesses = '설치'
        }
    }
}
