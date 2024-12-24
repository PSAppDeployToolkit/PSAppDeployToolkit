﻿@{
    BalloonText = @{
        Complete = @{
            Install = '설치 완료.'
            Repair = '복구 완료.'
            Uninstall = '제거 완료.'
        }
        Error = @{
            Install = '설치에 실패했습니다.'
            Repair = '복구에 실패했습니다.'
            Uninstall = '제거에 실패했습니다.'
        }
        FastRetry = @{
            Install = '설치가 완료되지 않았습니다.'
            Repair = '복구가 완료되지 않았습니다.'
            Uninstall = '제거가 완료되지 않았습니다.'
        }
        RestartRequired = @{
            Install = '설치 완료. 재부팅이 필요합니다.'
            Repair = '복구 완료. 재부팅이 필요합니다.'
            Uninstall = '제거 완료. 재부팅이 필요합니다.'
        }
        Start = @{
            Install = '설치가 시작되었습니다.'
            Repair = '복구 시작.'
            Uninstall = '제거가 시작되었습니다.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = '설치 작업을 완료할 수 있도록 이 애플리케이션 실행이 일시적으로 차단되었습니다.'
            Repair = '복구 작업을 완료하기 위해 이 애플리케이션 실행이 일시적으로 차단되었습니다.'
            Uninstall = '제거 작업을 완료하기 위해 이 애플리케이션 실행이 일시적으로 차단되었습니다.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - 앱 설치'
            Repair = 'PSAppDeployToolkit - 앱 복구'
            Uninstall = 'PSAppDeployToolkit - 앱 제거'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "설치를 완료할 디스크 공간이 부족합니다:`n{0}`n`n공간 필요: {1}MB`n공간 사용 가능: {2}MB`n 설치를 계속하려면 디스크 공간을 충분히 확보하십시오."
            Repair = "디스크 공간이 부족하여:`n{0}`n`n복구를 완료하려면 공간이 필요합니다: {1}MB`n사용 가능한 공간: {2}MB`n수리를 계속하려면 디스크 공간을 충분히 확보하십시오."
            Uninstall = "디스크 공간이 부족하여:`n{0}`n`n제거를 완료할 공간이 없습니다: {1}MB`n사용 가능한 공간: {2}MB`n제거를 계속하려면 디스크 공간을 충분히 확보하세요."
        }
    }
    Progress = @{
        Message = @{
            Install = '설치 중입니다. 잠시만 기다려주세요...'
            Repair = '수리 중입니다. 잠시만 기다려주세요...'
            Uninstall = '제거 중입니다. 잠시만 기다려주세요...'
        }
        MessageDetail = @{
            Install = '설치가 완료되면 이 창이 자동으로 닫힙니다.'
            Repair = '복구가 완료되면 이 창이 자동으로 닫힙니다.'
            Uninstall = '제거가 완료되면 이 창이 자동으로 닫힙니다.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - 앱 설치'
            Repair = 'PSAppDeployToolkit - 앱 복구'
            Uninstall = 'PSAppDeployToolkit - 앱 제거'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - 앱 설치'
            Repair = 'PSAppDeployToolkit - 앱 복구'
            Uninstall = 'PSAppDeployToolkit - 앱 제거'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = '최소화'
        ButtonRestartNow = '지금 다시 시작'
        Message = @{
            Install = '설치를 완료하려면 컴퓨터를 다시 시작해야 합니다.'
            Repair = '복구가 완료되려면 컴퓨터를 다시 시작해야 합니다.'
            Uninstall = '제거를 완료하려면 컴퓨터를 다시 시작해야 합니다.'
        }
        MessageRestart = '카운트다운이 끝나면 컴퓨터가 자동으로 다시 시작됩니다.'
        MessageTime = '작업을 저장하고 할당된 시간 내에 다시 시작하세요.'
        TimeRemaining = '남은 시간:'
        Title = '재시작 필요'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - 앱 설치'
            Repair = 'PSAppDeployToolkit - 앱 복구'
            Uninstall = 'PSAppDeployToolkit - 앱 제거'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = '&프로그램 닫기'
                ButtonContinue = '&계속하기'
                ButtonContinueTooltip = "위에 나열된 애플리케이션을 닫은 후에만 '계속'을 선택하세요."
                ButtonDefer = '&연기하다'
                CountdownMessage = '참고: 프로그램이 자동으로 닫히는 위치:'
                Message = @{
                    Install = "다음 프로그램을 닫아야 설치를 계속할 수 있습니다.`n`n작업을 저장하고 프로그램을 닫은 다음 계속하세요. 또는 작업을 저장하고 ``프로그램 닫기``를 클릭하세요."
                    복구 = "복구를 진행하려면 다음 프로그램을 닫아야 합니다.`n`n작업을 저장하고 프로그램을 닫은 다음 계속하십시오. 또는 작업을 저장하고 ``프로그램 닫기``를 클릭하세요."
                    제거 = "제거를 진행하려면 다음 프로그램을 닫아야 합니다.`n`n작업을 저장하고 프로그램을 닫은 후 계속하세요. 또는 작업을 저장하고 ``프로그램 닫기``를 클릭하세요."
                }
            }
            Defer = @{
                Deadline = '마감일:'
                ExpiryMessage = @{
                    Install = '연기가 만료될 때까지 설치를 연기하도록 선택할 수 있습니다:'
                    Repair = '연기가 만료될 때까지 수리를 연기하도록 선택할 수 있습니다:'
                    Uninstall = '유예가 만료될 때까지 제거를 연기하도록 선택할 수 있습니다:'
                }
                RemainingDeferrals = '남은 연기:'
                WarningMessage = '연기가 만료되면 더 이상 연기할 수 있는 옵션이 없습니다.'
                WelcomeMessage = @{
                    Install = '다음 애플리케이션을 설치하려고 합니다.'
                    Repair = '다음 애플리케이션을 복구하려고 합니다:'
                    Uninstall = '다음 애플리케이션을 제거하려고 합니다:'
                }
            }
            CountdownMessage = @{
                Install = '설치가 자동으로 다음 위치에서 계속됩니다:'
                Repair = '복구는 자동으로 다음에서 계속됩니다:'
                Uninstall = '제거는 자동으로 다음 위치에서 계속됩니다:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - 앱 설치'
                Repair = 'PSAppDeployToolkit - 앱 복구'
                Uninstall = 'PSAppDeployToolkit - 앱 제거'
            }
            DialogMessage = '다음 애플리케이션이 자동으로 닫히므로 계속하기 전에 작업을 저장하세요.'
            DialogMessageNoProcesses = @{
                Install = '설치를 계속하려면 설치를 선택하세요. 연기할 항목이 남아 있는 경우 설치를 연기하도록 선택할 수도 있습니다.'
                Repair = '수리를 계속하려면 수리를 선택하세요. 연기된 항목이 남아 있는 경우 수리를 연기하도록 선택할 수도 있습니다.'
                Uninstall = '제거를 계속하려면 제거를 선택하세요. 연기가 남아 있는 경우 제거를 연기하도록 선택할 수도 있습니다.'
            }
            ButtonDeferRemaining = '남아 있음'
            ButtonLeftText = '연기'
            ButtonRightText = @{
                Install = '앱 닫기 및 설치'
                Repair = '앱 닫기 & 복구'
                Uninstall = '앱 닫기 및 제거'
            }
            ButtonRightTextNoProcesses = @{
                Install = '설치'
                Repair = '복구'
                Uninstall = '제거'
            }
        }
    }
}
