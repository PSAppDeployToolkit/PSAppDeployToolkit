#-----------------------------------------------------------------------------
#
# MARK: Module Constants and Function Exports
#
#-----------------------------------------------------------------------------

# Rethrowing caught exceptions makes the error output from Import-Module look better.
try
{
    # Set all functions as read-only, export all public definitions and finalise the CommandTable.
    Set-Item -LiteralPath $FunctionPaths -Options ReadOnly; Get-Item -LiteralPath $FunctionPaths | & { process { $CommandTable.Add($_.get_Name(), $_) } }
    New-Variable -Name CommandTable -Value ([System.Collections.ObjectModel.ReadOnlyDictionary[System.String, System.Management.Automation.CommandInfo]]::new($CommandTable)) -Option Constant -Force -Confirm:$false
    Export-ModuleMember -Function $Module.Manifest.FunctionsToExport

    # Define object for holding all PSADT variables.
    New-Variable -Name ADT -Option Constant -Value ([pscustomobject]@{
            ModuleDefaults = ([ordered]@{
                    Strings = ([ordered]@{
                            '' = {
                                @{
                                    BalloonTip = @{
                                        # Text displayed in the balloon tip for the start of a deployment type.
                                        Start = @{
                                            Install = 'Installation started.'
                                            Repair = 'Repair started.'
                                            Uninstall = 'Uninstallation started.'
                                        }

                                        # Text displayed in the balloon tip for successful completion of a deployment type.
                                        Complete = @{
                                            Install = 'Installation complete.'
                                            Repair = 'Repair complete.'
                                            Uninstall = 'Uninstallation complete.'
                                        }

                                        # Text displayed in the balloon tip for successful completion of a deployment type.
                                        RestartRequired = @{
                                            Install = 'Installation complete. A reboot is required.'
                                            Repair = 'Repair complete. A reboot is required.'
                                            Uninstall = 'Uninstallation complete. A reboot is required.'
                                        }

                                        # Text displayed in the balloon tip for fast retry of a deployment.
                                        FastRetry = @{
                                            Install = 'Installation not complete.'
                                            Repair = 'Repair not complete.'
                                            Uninstall = 'Uninstallation not complete.'
                                        }

                                        # Text displayed in the balloon tip for a failed deployment type.
                                        Error = @{
                                            Install = 'Installation failed.'
                                            Repair = 'Repair failed.'
                                            Uninstall = 'Uninstallation failed.'
                                        }
                                    }

                                    BlockExecutionText = @{
                                        # Text displayed when prompting user that an application has been blocked.
                                        Message = @{
                                            Install = 'Launching this application has been temporarily blocked so that an installation operation can complete.'
                                            Repair = 'Launching this application has been temporarily blocked so that a repair operation can complete.'
                                            Uninstall = 'Launching this application has been temporarily blocked so that an uninstallation operation can complete.'
                                        }

                                        # The subtitle underneath the Install Title, e.g. Company Name. Only for Fluent dialogs.
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - App Installation'
                                            Repair = '{Toolkit\CompanyName} - App Repair'
                                            Uninstall = '{Toolkit\CompanyName} - App Uninstallation'
                                        }
                                    }

                                    DiskSpaceText = @{
                                        # Text displayed when the system does not have sufficient free disk space available to complete the installation.
                                        Message = @{
                                            Install = "You do not have enough disk space to complete the installation of:`n{0}`n`nSpace required: {1}MB`nSpace available: {2}MB`n`nPlease free up enough disk space in order to proceed with the installation."
                                            Repair = "You do not have enough disk space to complete the repair of:`n{0}`n`nSpace required: {1}MB`nSpace available: {2}MB`n`nPlease free up enough disk space in order to proceed with the repair."
                                            Uninstall = "You do not have enough disk space to complete the uninstallation of:`n{0}`n`nSpace required: {1}MB`nSpace available: {2}MB`n`nPlease free up enough disk space in order to proceed with the uninstallation."
                                        }
                                    }

                                    InstallationPrompt = @{
                                        # The subtitle underneath the Install Title, e.g. Company Name. Only for Fluent dialogs.
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - App Installation'
                                            Repair = '{Toolkit\CompanyName} - App Repair'
                                            Uninstall = '{Toolkit\CompanyName} - App Uninstallation'
                                        }
                                    }

                                    ListSelectionPrompt = @{
                                        # Default message displayed next to the list selection dropdown.
                                        ListSelectionMessage = 'Select an item:'
                                    }

                                    ProgressPrompt = @{
                                        # Default message displayed in the progress bar.
                                        Message = @{
                                            Install = 'Installation in progress. Please wait…'
                                            Repair = 'Repair in progress. Please wait…'
                                            Uninstall = 'Uninstallation in progress. Please wait…'
                                        }

                                        # Default message detail displayed in the progress bar.
                                        MessageDetail = @{
                                            Install = 'This window will close automatically when the installation is complete.'
                                            Repair = 'This window will close automatically when the repair is complete.'
                                            Uninstall = 'This window will close automatically when the uninstallation is complete.'
                                        }

                                        # The subtitle underneath the Install Title, e.g. Company Name. Only for Fluent dialogs.
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - App Installation'
                                            Repair = '{Toolkit\CompanyName} - App Repair'
                                            Uninstall = '{Toolkit\CompanyName} - App Uninstallation'
                                        }
                                    }

                                    RestartPrompt = @{
                                        # Button text for allowing the user to restart later.
                                        ButtonRestartLater = 'Minimize'

                                        # Button text for when wanting to restart the device now.
                                        ButtonRestartNow = 'Restart Now'

                                        # Text displayed when the device requires a restart.
                                        Message = @{
                                            Install = 'In order for the installation to complete, you must restart your computer.'
                                            Repair = 'In order for the repair to complete, you must restart your computer.'
                                            Uninstall = 'In order for the uninstallation to complete, you must restart your computer.'
                                        }

                                        # This is a custom message to display at the Restart window.
                                        CustomMessage = $null

                                        # Text displayed when indicating when the device will be restarted.
                                        MessageRestart = 'Your computer will be automatically restarted at the end of the countdown.'

                                        # Text displayed as a prefix to the time remaining, indicating that users should save their work, etc.
                                        MessageTime = 'Please save your work and restart within the allotted time.'

                                        # Text displayed to indicate the amount of time remaining until a restart will occur.
                                        TimeRemaining = 'Time remaining:'

                                        # Text displayed in the title of the restart prompt which helps the script identify whether there is already a restart prompt being displayed and not to duplicate it.
                                        Title = 'Restart Required'

                                        # The subtitle underneath the Install Title, e.g. Company Name. Only for Fluent dialogs.
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - App Installation'
                                            Repair = '{Toolkit\CompanyName} - App Repair'
                                            Uninstall = '{Toolkit\CompanyName} - App Uninstallation'
                                        }
                                    }

                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            # Text displayed when only the deferral dialog is to be displayed and there are no applications to close.
                                            WelcomeMessage = @{
                                                Install = 'The following application is about to be installed:'
                                                Repair = 'The following application is about to be repaired:'
                                                Uninstall = 'The following application is about to be uninstalled:'
                                            }

                                            # Text displayed when prompting to close running programs.
                                            CloseAppsMessage = @{
                                                Install = "The following programs must be closed before the installation can proceed.`n`nPlease save your work, close the programs, and then continue. Alternatively, save your work and click `"Close Programs`"."
                                                Repair = "The following programs must be closed before the repair can proceed.`n`nPlease save your work, close the programs, and then continue. Alternatively, save your work and click `"Close Programs`"."
                                                Uninstall = "The following programs must be closed before the uninstallation can proceed.`n`nPlease save your work, close the programs, and then continue. Alternatively, save your work and click `"Close Programs`"."
                                            }

                                            # Text displayed when a deferral option is available.
                                            ExpiryMessage = @{
                                                Install = 'You can choose to defer the installation until the deferral expires:'
                                                Repair = 'You can choose to defer the repair until the deferral expires:'
                                                Uninstall = 'You can choose to defer the uninstallation until the deferral expires:'
                                            }

                                            # Text displayed when there are a specific number of deferrals remaining.
                                            DeferralsRemaining = 'Remaining Deferrals:'

                                            # Text displayed when there is a specific deferral deadline.
                                            DeferralDeadline = 'Deadline:'

                                            # Text displayed after the deferral options.
                                            ExpiryWarning = 'Once the deferral has expired, you will no longer have the option to defer.'

                                            # The countdown message displayed at the Welcome Screen to indicate when the deployment will continue if no response from user.
                                            CountdownDefer = @{
                                                Install = 'The installation will automatically continue in:'
                                                Repair = 'The repair will automatically continue in:'
                                                Uninstall = 'The uninstallation will automatically continue in:'
                                            }

                                            # Text displayed when counting down to automatically closing applications.
                                            CountdownClose = @{
                                                Install = 'NOTE: The program(s) will be automatically closed in:'
                                                Repair = 'NOTE: The program(s) will be automatically closed in:'
                                                Uninstall = 'NOTE: The program(s) will be automatically closed in:'
                                            }

                                            # Text displayed on the close button when prompting to close running programs.
                                            ButtonClose = 'Close &Programs'

                                            # Text displayed on the defer button when prompting to close running programs.
                                            ButtonDefer = '&Defer'

                                            # Text displayed on the continue button when prompting to close running programs.
                                            ButtonContinue = '&Continue'

                                            # Tooltip text displayed on the continue button when prompting to close running programs.
                                            ButtonContinueTooltip = 'Only select "Continue" after closing the above listed application(s).'
                                        }

                                        Fluent = @{
                                            # This is a message to prompt users to save their work.
                                            DialogMessage = @{
                                                Install = 'Please save your work before continuing as the following applications will be closed automatically.'
                                                Repair = 'Please save your work before continuing as the following applications will be closed automatically.'
                                                Uninstall = 'Please save your work before continuing as the following applications will be closed automatically.'
                                            }

                                            # This is a message to when there are no running processes available.
                                            DialogMessageNoProcesses = @{
                                                Install = 'Please select Install to continue with the installation.'
                                                Repair = 'Please select Repair to continue with the repair.'
                                                Uninstall = 'Please select Uninstall to continue with the uninstallation.'
                                            }

                                            # A string to describe the automatic start countdown.
                                            AutomaticStartCountdown = 'Automatic Start Countdown'

                                            # Text displayed when there are a specific number of deferrals remaining.
                                            DeferralsRemaining = 'Remaining Deferrals'

                                            # Text displayed when there is a specific deferral deadline.
                                            DeferralDeadline = 'Deferral Deadline'

                                            # This is a phrase used to describe the process of closing applications and commencing the deployment.
                                            ButtonLeftText = @{
                                                Install = 'Close Apps & Install'
                                                Repair = 'Close Apps & Repair'
                                                Uninstall = 'Close Apps & Uninstall'
                                            }

                                            # This is a phrase used to describe the process of commencing the deployment.
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Install'
                                                Repair = 'Repair'
                                                Uninstall = 'Uninstall'
                                            }

                                            # This is a phrase used to describe the process of deferring a deployment.
                                            ButtonRightText = 'Defer'

                                            # The subtitle underneath the Install Title, e.g. Company Name.
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - App Installation'
                                                Repair = '{Toolkit\CompanyName} - App Repair'
                                                Uninstall = '{Toolkit\CompanyName} - App Uninstallation'
                                            }
                                        }

                                        # This is a custom message to display at the Welcome Screen window.
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'ar' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'بدء التثبيت.'
                                            Repair = 'بدأ الإصلاح.'
                                            Uninstall = 'بدأ إلغاء التثبيت.'
                                        }
                                        Complete = @{
                                            Install = 'اكتمل التثبيت.'
                                            Repair = 'اكتمل الإصلاح.'
                                            Uninstall = 'اكتمل إلغاء التثبيت.'
                                        }
                                        RestartRequired = @{
                                            Install = 'اكتمل التثبيت. مطلوب إعادة التشغيل.'
                                            Repair = 'اكتمل الإصلاح. مطلوب إعادة التشغيل.'
                                            Uninstall = 'اكتملت عملية إلغاء التثبيت. مطلوب إعادة التشغيل.'
                                        }
                                        FastRetry = @{
                                            Install = 'لم يكتمل التثبيت.'
                                            Repair = 'لم يكتمل الإصلاح.'
                                            Uninstall = 'لم تكتمل عملية إلغاء التثبيت.'
                                        }
                                        Error = @{
                                            Install = 'فشل التثبيت.'
                                            Repair = 'فشل الإصلاح.'
                                            Uninstall = 'فشلت عملية إلغاء التثبيت.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'تم حظر تشغيل هذا التطبيق مؤقتاً حتى تكتمل عملية التثبيت.'
                                            Repair = 'تم حظر تشغيل هذا التطبيق مؤقتاً حتى تكتمل عملية الإصلاح.'
                                            Uninstall = 'تم حظر تشغيل هذا التطبيق مؤقتاً حتى تكتمل عملية إلغاء التثبيت.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - تثبيت التطبيق'
                                            Repair = '{Toolkit\CompanyName} - إصلاح التطبيق'
                                            Uninstall = '{Toolkit\CompanyName} - إلغاء تثبيت التطبيق'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "ليس لديك مساحة قرص كافية لإكمال تثبيت:>`n{0}`n`nمساحة القرص المطلوبة: {1}`n ميغابايت مساحة متوفرة: {2} ميغابايت''`n`nيرجى تحرير مساحة كافية على القرص من أجل متابعة التثبيت."
                                            Repair = "ليس لديك مساحة كافية على القرص لإكمال إصلاح:`n{0}`n`nالمساحة المطلوبة: {1}`nميجابايت}}المساحة المتوفرة: {2}ميجابايت: {1} ميغابايت'' مساحة متوفرة: {2} ميغابايت''`n`nيرجى تحرير مساحة كافية على القرص لمتابعة الإصلاح."
                                            Uninstall = "ليس لديك مساحة كافية على القرص لإكمال عملية إلغاء التثبيت ل:`n{0}`n`nالمساحة المطلوبة: {1}MB`nالمساحة المتوفرة: {2}MB: {1} ميغابايت&مساحة متوفرة: {2} ميغابايت''`n`nيرجى تحرير مساحة كافية على القرص من أجل متابعة عملية إزالة التثبيت."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - تثبيت التطبيق'
                                            Repair = '{Toolkit\CompanyName} - إصلاح التطبيق'
                                            Uninstall = '{Toolkit\CompanyName} - إلغاء تثبيت التطبيق'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'اختر عنصرًا:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'التثبيت قيد التقدم. الرجاء الانتظار…'
                                            Repair = 'الإصلاح قيد التقدم. الرجاء الانتظار…'
                                            Uninstall = 'إلغاء التثبيت قيد التقدم. الرجاء الانتظار…'
                                        }
                                        MessageDetail = @{
                                            Install = 'سيتم إغلاق هذه النافذة تلقائياً عند اكتمال التثبيت.'
                                            Repair = 'سيتم إغلاق هذه النافذة تلقائياً عند اكتمال الإصلاح.'
                                            Uninstall = 'سيتم إغلاق هذه النافذة تلقائياً عند اكتمال إلغاء التثبيت.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - تثبيت التطبيق'
                                            Repair = '{Toolkit\CompanyName} - إصلاح التطبيق'
                                            Uninstall = '{Toolkit\CompanyName} - إلغاء تثبيت التطبيق'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'تصغير'
                                        ButtonRestartNow = 'إعادة التشغيل الآن'
                                        Message = @{
                                            Install = 'لكي يكتمل التثبيت، يجب إعادة تشغيل الكمبيوتر.'
                                            Repair = 'لكي تكتمل عملية الإصلاح، يجب إعادة تشغيل الكمبيوتر.'
                                            Uninstall = 'لكي تكتمل عملية إلغاء التثبيت، يجب إعادة تشغيل الكمبيوتر.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'سيتم إعادة تشغيل الكمبيوتر تلقائياً في نهاية العد التنازلي.'
                                        MessageTime = 'يرجى حفظ عملك وإعادة التشغيل خلال الوقت المخصص.'
                                        TimeRemaining = 'الوقت المتبقي:'
                                        Title = 'إعادة التشغيل مطلوبة'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - تثبيت التطبيق'
                                            Repair = '{Toolkit\CompanyName} - إصلاح التطبيق'
                                            Uninstall = '{Toolkit\CompanyName} - إلغاء تثبيت التطبيق'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'التطبيق التالي على وشك التثبيت:'
                                                Repair = 'التطبيق التالي على وشك أن يتم إصلاحه:'
                                                Uninstall = 'التطبيق التالي على وشك إلغاء التثبيت:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "يجب إغلاق البرامج التالية قبل متابعة التثبيت.`n`nالرجاء حفظ عملك وإغلاق البرامج ثم المتابعة. بدلاً من ذلك، احفظ عملك وانقر فوق `”إغلاق البرامج'`“."
                                                Repair = "يجب إغلاق البرامج التالية قبل متابعة عملية الإصلاح.`n`nيرجى حفظ عملك وإغلاق البرامج ثم المتابعة. بدلاً من ذلك، احفظ عملك وانقر فوق `”إغلاق البرامج`“."
                                                Uninstall = "يجب إغلاق البرامج التالية قبل متابعة عملية إزالة التثبيت.`n`nيرجى حفظ عملك وإغلاق البرامج ثم المتابعة. بدلاً من ذلك، احفظ عملك وانقر فوق `”إغلاق البرامج`“."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'يمكنك اختيار تأجيل التثبيت حتى انتهاء صلاحية التأجيل:'
                                                Repair = 'يمكنك اختيار تأجيل الإصلاح حتى انتهاء صلاحية التأجيل:'
                                                Uninstall = 'يمكنك اختيار تأجيل إلغاء التثبيت حتى انتهاء صلاحية التأجيل:'
                                            }
                                            DeferralsRemaining = 'التأجيلات المتبقية:'
                                            DeferralDeadline = 'الموعد النهائي:'
                                            ExpiryWarning = 'بمجرد انتهاء صلاحية التأجيل، لن يكون لديك خيار التأجيل بعد ذلك.'
                                            CountdownDefer = @{
                                                Install = 'ستتم متابعة التثبيت تلقائيًا في:'
                                                Repair = 'سيستمر الإصلاح تلقائيًا في:'
                                                Uninstall = 'ستتم متابعة إلغاء التثبيت تلقائيًا في:'
                                            }
                                            CountdownClose = @{
                                                Install = 'ملاحظة: سيتم إغلاق البرنامج (البرامج) تلقائيًا في:'
                                                Repair = 'ملاحظة: سيتم إغلاق البرنامج (البرامج) تلقائيًا في:'
                                                Uninstall = 'ملاحظة: سيتم إغلاق البرنامج (البرامج) تلقائيًا في:'
                                            }
                                            ButtonClose = 'إغلاق &برامج'
                                            ButtonDefer = '&تأجيل'
                                            ButtonContinue = '&متابعة'
                                            ButtonContinueTooltip = 'قم باختيار "متابعة" فقط بعد إغلاق التطبيق/التطبيقات المدرجة أعلاه.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'يرجى حفظ عملك قبل المتابعة حيث سيتم إغلاق التطبيقات التالية تلقائيًا.'
                                                Repair = 'يرجى حفظ عملك قبل المتابعة حيث سيتم إغلاق التطبيقات التالية تلقائيًا.'
                                                Uninstall = 'يرجى حفظ عملك قبل المتابعة حيث سيتم إغلاق التطبيقات التالية تلقائيًا.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'الرجاء تحديد تثبيت لمتابعة التثبيت.'
                                                Repair = 'الرجاء تحديد إصلاح لمتابعة الإصلاح.'
                                                Uninstall = 'الرجاء تحديد إلغاء التثبيت لمتابعة إزالة التثبيت.'
                                            }
                                            AutomaticStartCountdown = 'العد التنازلي لبدء التشغيل التلقائي'
                                            DeferralsRemaining = 'التأجيلات المتبقية'
                                            DeferralDeadline = 'الموعد النهائي للتأجيل'
                                            ButtonLeftText = @{
                                                Install = 'إغلاق التطبيقات والتثبيت'
                                                Repair = 'إغلاق التطبيقات وإصلاحها'
                                                Uninstall = 'إغلاق التطبيقات وإلغاء التثبيت'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'تثبيت'
                                                Repair = 'إصلاح'
                                                Uninstall = 'إلغاء التثبيت'
                                            }
                                            ButtonRightText = 'تأجيل'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - تثبيت التطبيق'
                                                Repair = '{Toolkit\CompanyName} - إصلاح التطبيق'
                                                Uninstall = '{Toolkit\CompanyName} - إلغاء تثبيت التطبيق'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'bg' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Инсталацията започна.'
                                            Repair = 'Поправката започна.'
                                            Uninstall = 'Деинсталацията започна.'
                                        }
                                        Complete = @{
                                            Install = 'Инсталацията завърши.'
                                            Repair = 'Поправката завърши.'
                                            Uninstall = 'Деинсталацията завърши.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Инсталацията завърши. Необходим е рестарт.'
                                            Repair = 'Поправката завърши. Необходим е рестарт.'
                                            Uninstall = 'Деинсталацията завърши. Необходим е рестарт.'
                                        }
                                        FastRetry = @{
                                            Install = 'Инсталацията все още не е приключила.'
                                            Repair = 'Поправката все още не е приключила.'
                                            Uninstall = 'Деинсталацията все още не е приключила.'
                                        }
                                        Error = @{
                                            Install = 'Инсталацията беше неуспешна.'
                                            Repair = 'Поправката беше неуспешна.'
                                            Uninstall = 'Деинсталацията беше неуспешна.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Стартирането на това приложение е временно забранено, за да може инсталацията да приключи.'
                                            Repair = 'Стартирането на това приложение е временно забранено, за да може поправката да приключи.'
                                            Uninstall = 'Стартирането на това приложение е временно забранено, за да може деинсталацията да приключи.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Инсталация на приложение'
                                            Repair = '{Toolkit\CompanyName} - Поправка на приложение'
                                            Uninstall = '{Toolkit\CompanyName} - Деинсталация на приложение'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Нямате достатъчно дисково пространство, за да завършите инсталацията на:`n{0}`n`nНеобходимо дисково пространство: {1}MB`nСвободно дисково пространство: {2}MB`n`nМоля, освободете достатъчно дисково пространство, за да можете да продължите с инсталацията."
                                            Repair = "Нямате достатъчно дисково пространство, за да завършите поправката на:`n{0}`n`nНеобходимо дисково пространство: {1}MB`nСвободно дисково пространство: {2}MB`n`nМоля, освободете достатъчно дисково пространство, за да можете да продължите с поправката."
                                            Uninstall = "Нямате достатъчно дисково пространство, за да завършите деинсталацията на:`n{0}`n`nНеобходимо дисково пространство: {1}MB`nСвободно дисково пространство: {2}MB`n`nМоля, освободете достатъчно дисково пространство, за да можете да продължите с деинсталацията."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Инсталация на приложение'
                                            Repair = '{Toolkit\CompanyName} - Поправка на приложение'
                                            Uninstall = '{Toolkit\CompanyName} - Деинсталация на приложение'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Изберете елемент:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'В момента се извършва инсталация. Моля, изчакайте…'
                                            Repair = 'В момента се извършва поправка. Моля, изчакайте…'
                                            Uninstall = 'В момента се извършва деинсталация. Моля, изчакайте…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Този прозорец ще се затвори автоматично, когато инсталацията завърши.'
                                            Repair = 'Този прозорец ще се затвори автоматично, когато поправката завърши.'
                                            Uninstall = 'Този прозорец ще се затвори автоматично, когато деинсталацията завърши.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Инсталация на приложение'
                                            Repair = '{Toolkit\CompanyName} - Поправка на приложение'
                                            Uninstall = '{Toolkit\CompanyName} - Деинсталация на приложение'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Минимизирай'
                                        ButtonRestartNow = 'Рестартирай сега'
                                        Message = @{
                                            Install = 'За да може инсталацията да завърши, е нужно да рестартирате Вашия компютър.'
                                            Repair = 'За да може поправката да завърши, е нужно да рестартирате Вашия компютър.'
                                            Uninstall = 'За да може деинсталацията да завърши, е нужно да рестартирате Вашия компютър.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Компютърът Ви ще се рестартира автоматично след изтичането на обратното броене.'
                                        MessageTime = 'Моля, запазете работата си и рестартирайте компютъра в определеното време.'
                                        TimeRemaining = 'Оставащо време:'
                                        Title = 'Необходим е рестарт'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Инсталация на приложение'
                                            Repair = '{Toolkit\CompanyName} - Поправка на приложение'
                                            Uninstall = '{Toolkit\CompanyName} - Деинсталация на приложение'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Следното приложение ще бъде инсталирано:'
                                                Repair = 'Следното приложение ще бъде поправено:'
                                                Uninstall = 'Следното приложение ще бъде деинсталирано:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Следните програми трябва да бъдат затворени преди инсталацията да продължи.`n`nМоля, запазете работата си, затворете програмите и продължете. Друга възможност е да запазите работата си и да щракнете `"Затвори програмите`"."
                                                Repair = "Следните програми трябва да бъдат затворени преди поправката да продължи.`n`nМоля, запазете работата си, затворете програмите и продължете. Друга възможност е да запазите работата си и да щракнете `"Затвори програмите`"."
                                                Uninstall = "Следните програми трябва да бъдат затворени преди деинсталацията да продължи.`n`nМоля, запазете работата си, затворете програмите и продължете. Друга възможност е да запазите работата си и да щракнете `"Затвори програмите`"."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Можете да изберете да отложите инсталацията до изтичане на гратисния период:'
                                                Repair = 'Можете да изберете да отложите поправката до изтичане на гратисния период:'
                                                Uninstall = 'Можете да изберете да отложите деинсталацията до изтичане на гратисния период:'
                                            }
                                            DeferralsRemaining = 'Оставащи отлагания:'
                                            DeferralDeadline = 'Краен срок:'
                                            ExpiryWarning = 'След като изтече гратисният период, няма да имате възможност да отложите.'
                                            CountdownDefer = @{
                                                Install = 'Инсталацията ще продължи автоматично след:'
                                                Repair = 'Поправката ще продължи автоматично след:'
                                                Uninstall = 'Деинсталацията ще продължи автоматично след:'
                                            }
                                            CountdownClose = @{
                                                Install = 'ЗАБЕЛЕЖКА: Програмите ще бъдат затворени автоматично след:'
                                                Repair = 'ЗАБЕЛЕЖКА: Програмите ще бъдат затворени автоматично след:'
                                                Uninstall = 'ЗАБЕЛЕЖКА: Програмите ще бъдат затворени автоматично след:'
                                            }
                                            ButtonClose = 'Затвори &програмите'
                                            ButtonDefer = '&Отложи'
                                            ButtonContinue = '&Продължи'
                                            ButtonContinueTooltip = 'Щракнете "Продължи" само след като затворите горепосочените програми.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Моля, запазете работата си преди да продължите, тъй като следните програми ще бъдат затворени автоматично.'
                                                Repair = 'Моля, запазете работата си преди да продължите, тъй като следните програми ще бъдат затворени автоматично.'
                                                Uninstall = 'Моля, запазете работата си преди да продължите, тъй като следните програми ще бъдат затворени автоматично.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Моля, щракнете Инсталирай, за да продължите с инсталацията.'
                                                Repair = 'Моля, щракнете Поправи, за да продължите с поправката.'
                                                Uninstall = 'Моля, щракнете Деинсталирай, за да продължите с деинсталацията.'
                                            }
                                            AutomaticStartCountdown = 'Обратно броене до автоматично стартиране'
                                            DeferralsRemaining = 'Оставащи отлагания'
                                            DeferralDeadline = 'Краен срок за отлагане'
                                            ButtonLeftText = @{
                                                Install = 'Затвори програмите и инсталирай'
                                                Repair = 'Затвори програмите и поправи'
                                                Uninstall = 'Затвори програмите и деинсталирай'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Инсталирай'
                                                Repair = 'Поправи'
                                                Uninstall = 'Деинсталирай'
                                            }
                                            ButtonRightText = 'Отложи'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Инсталация на приложение'
                                                Repair = '{Toolkit\CompanyName} - Поправка на приложение'
                                                Uninstall = '{Toolkit\CompanyName} - Деинсталация на приложение'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'cs' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Instalace byla zahájena.'
                                            Repair = 'Oprava zahájena.'
                                            Uninstall = 'Odinstalace zahájena.'
                                        }
                                        Complete = @{
                                            Install = 'Instalace dokončena.'
                                            Repair = 'Oprava dokončena.'
                                            Uninstall = 'Odinstalace dokončena.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Instalace dokončena. Je vyžadován restart.'
                                            Repair = 'Oprava dokončena. Je vyžadován restart.'
                                            Uninstall = 'Odinstalace dokončena. Je vyžadován restart.'
                                        }
                                        FastRetry = @{
                                            Install = 'Instalace nebyla dokončena.'
                                            Repair = 'Oprava nebyla dokončena.'
                                            Uninstall = 'Odinstalace nebyla dokončena.'
                                        }
                                        Error = @{
                                            Install = 'Instalace se nezdařila.'
                                            Repair = 'Oprava se nezdařila.'
                                            Uninstall = 'Odinstalace se nezdařila.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Spuštění této aplikace bylo dočasně zablokováno, aby mohla být dokončena instalační operace.'
                                            Repair = 'Spuštění této aplikace bylo dočasně zablokováno, aby mohla být dokončena operace opravy.'
                                            Uninstall = 'Spuštění této aplikace bylo dočasně zablokováno, aby mohla být dokončena operace odinstalace.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalace Aplikace'
                                            Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
                                            Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Nemáte dostatek místa na disku pro dokončení instalace:`n{0}`n`nPotřebné místo: {1}MB`nDostupné místo: {2}MB`n`nUvolněte prosím dostatek místa na disku, abyste mohli pokračovat v instalaci."
                                            Repair = "Nemáte dostatek místa na disku pro dokončení oprava:`n{0}`n`nPotřebné místo: {1}MB`nDostupné místo: {2}MB`n`nUvolněte prosím dostatek místa na disku, abyste mohli pokračovat v opravě."
                                            Uninstall = "Nemáte dostatek místa na disku, abyste mohli dokončit odinstalaci:`n{0}`n`nPotřebné místo: {1}MB`nDostupné místo: {2}MB`n`nUvolněte prosím dostatek místa na disku, abyste mohli pokračovat v odinstalaci."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalace Aplikace'
                                            Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
                                            Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Vyberte položku:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Probíhá instalace. Počkejte prosím…'
                                            Repair = 'Probíhá oprava. Počkejte prosím…'
                                            Uninstall = 'Probíhá odinstalace. Počkejte prosím…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Toto okno se po dokončení instalace automaticky zavře.'
                                            Repair = 'Toto okno se automaticky zavře po dokončení opravy.'
                                            Uninstall = 'Toto okno se automaticky zavře po dokončení odinstalace.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalace Aplikace'
                                            Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
                                            Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimalizovat'
                                        ButtonRestartNow = 'Restartovat nyní'
                                        Message = @{
                                            Install = 'Aby se instalace dokončila, musíte restartovat počítač.'
                                            Repair = 'Aby byla oprava dokončena, musíte restartovat počítač.'
                                            Uninstall = 'Aby se odinstalace dokončila, musíte restartovat počítač.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Po skončení odpočítávání bude počítač automaticky restartován.'
                                        MessageTime = 'Uložte prosím svou práci a restartujte ji ve stanoveném čase.'
                                        TimeRemaining = 'Zbývající čas:'
                                        Title = 'Požadovaný Restart'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalace Aplikace'
                                            Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
                                            Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Chystá se instalace následující aplikace:'
                                                Repair = 'Následující aplikace bude opravena:'
                                                Uninstall = 'Následující aplikace bude odinstalována:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Před pokračováním instalace je nutné ukončit následující programy.`n`nProsím, uložte svou práci, ukončete programy a poté pokračujte. Případně uložte svou práci a klikněte na `„Zavřít programy`“."
                                                Repair = "Před pokračováním opravy je nutné zavřít následující programy.`n`nProsím, uložte svou práci, zavřete programy a poté pokračujte. Případně uložte svou práci a klikněte na `„Zavřít programy`“."
                                                Uninstall = "Před pokračováním odinstalace je nutné zavřít následující programy.`n`nProsím, uložte svou práci, zavřete programy a poté pokračujte. Případně uložte svou práci a klikněte na `„Zavřít programy`“."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Můžete se rozhodnout odložit instalaci až do vypršení odkladu:'
                                                Repair = 'Můžete se rozhodnout odložit opravu až do vypršení odkladu:'
                                                Uninstall = 'Můžete se rozhodnout odložit odinstalaci až do vypršení odkladu:'
                                            }
                                            DeferralsRemaining = 'Zbývající odklady:'
                                            DeferralDeadline = 'Termín:'
                                            ExpiryWarning = 'Po uplynutí odkladu již nebudete mít možnost odložit.'
                                            CountdownDefer = @{
                                                Install = 'Instalace bude automaticky pokračovat za:'
                                                Repair = 'Oprava bude automaticky pokračovat za:'
                                                Uninstall = 'Odinstalace bude automaticky pokračovat za:'
                                            }
                                            CountdownClose = @{
                                                Install = 'POZNÁMKA: Program(y) budou automaticky ukončeny za:'
                                                Repair = 'POZNÁMKA: Program(y) budou automaticky ukončeny za:'
                                                Uninstall = 'POZNÁMKA: Program(y) budou automaticky ukončeny za:'
                                            }
                                            ButtonClose = 'Zavřít &Programy'
                                            ButtonDefer = '&Odložení'
                                            ButtonContinue = '&Pokračovat'
                                            ButtonContinueTooltip = 'Po zavření výše uvedených aplikací vyberte pouze možnost „Pokračovat“.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Před pokračováním prosím uložte svou práci, protože následující aplikace budou automaticky ukončeny.'
                                                Repair = 'Před pokračováním prosím uložte svou práci, protože následující aplikace budou automaticky ukončeny.'
                                                Uninstall = 'Před pokračováním prosím uložte svou práci, protože následující aplikace budou automaticky ukončeny.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Chcete-li pokračovat v instalaci, vyberte prosím možnost Install.'
                                                Repair = 'Pro pokračování v opravě vyberte prosím Repair.'
                                                Uninstall = 'Chcete-li pokračovat v odinstalaci, vyberte prosím možnost Odinstalovat.'
                                            }
                                            AutomaticStartCountdown = 'Automatické odpočítávání spuštění'
                                            DeferralsRemaining = 'Zbývající odklady'
                                            DeferralDeadline = 'Lhůta pro odložení'
                                            ButtonLeftText = @{
                                                Install = 'Zavřít Aplikace a Nainstalovat'
                                                Repair = 'Zavřít Aplikace a Opravit'
                                                Uninstall = 'Zavřít Aplikace a Odinstalovat'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Instalovat'
                                                Repair = 'Opravit'
                                                Uninstall = 'Odinstalovat'
                                            }
                                            ButtonRightText = 'Odložit'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Instalace Aplikace'
                                                Repair = '{Toolkit\CompanyName} - Oprava Aplikace'
                                                Uninstall = '{Toolkit\CompanyName} - Odinstalace Aplikace'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'da' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Installation startet.'
                                            Repair = 'Reparation startet.'
                                            Uninstall = 'Afinstallation startet.'
                                        }
                                        Complete = @{
                                            Install = 'Installation fuldført.'
                                            Repair = 'Reparation fuldført.'
                                            Uninstall = 'Afinstallation fuldført.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Installationen er fuldført. En genstart er påkrævet.'
                                            Repair = 'Reparation fuldført. En genstart er påkrævet.'
                                            Uninstall = 'Afinstallation fuldført. En genstart er påkrævet.'
                                        }
                                        FastRetry = @{
                                            Install = 'Installation ikke fuldført.'
                                            Repair = 'Reparation ikke fuldført.'
                                            Uninstall = 'Afinstallation ikke fuldført.'
                                        }
                                        Error = @{
                                            Install = 'Installation mislykkedes.'
                                            Repair = 'Reparation mislykkedes.'
                                            Uninstall = 'Afinstallation mislykkedes.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Start af denne applikation er midlertidigt blokeret, så en installation kan gennemføres.'
                                            Repair = 'Start af dette program er midlertidigt blokeret, så en reparation kan gennemføres.'
                                            Uninstall = 'Start af dette program er midlertidigt blokeret, så en afinstallationsoperation kan gennemføres.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation af App'
                                            Repair = '{Toolkit\CompanyName} - Reparation af App'
                                            Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Du har ikke nok diskplads til at fuldføre installationen af:`n{0}`n`nPladsbehov: {1}MB`nPlads til rådighed: {2}MB`n`nFrigør venligst nok diskplads til at fortsætte med installationen."
                                            Repair = "Du har ikke nok diskplads til at fuldføre reparationen af:`n{0}`n`nKrævet plads: {1}MB`nPlads til rådighed: {2}MB`n`nFrigør venligst nok diskplads til at kunne fortsætte med reparationen."
                                            Uninstall = "Du har ikke nok diskplads til at fuldføre afinstallationen af:`n{0}`n`nPladsbehov: {1}MB`nPlads til rådighed: {2}MB`n`nFrigør venligst nok diskplads til at fortsætte med afinstallationen."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation af App'
                                            Repair = '{Toolkit\CompanyName} - Reparation af App'
                                            Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Vælg et element:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Installation i gang. Vent venligst…'
                                            Repair = 'Reparation i gang. Vent venligst…'
                                            Uninstall = 'Afinstallation i gang. Vent venligst…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Dette vindue lukkes automatisk, når installationen er færdig.'
                                            Repair = 'Dette vindue lukkes automatisk, når reparationen er færdig.'
                                            Uninstall = 'Dette vindue lukkes automatisk, når afinstallationen er færdig.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation af App'
                                            Repair = '{Toolkit\CompanyName} - Reparation af App'
                                            Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimér'
                                        ButtonRestartNow = 'Genstart nu'
                                        Message = @{
                                            Install = 'For at installationen kan gennemføres, skal du genstarte din computer.'
                                            Repair = 'For at reparationen kan gennemføres, skal du genstarte din computer.'
                                            Uninstall = 'For at afinstallationen kan gennemføres, skal du genstarte computeren.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Din computer genstartes automatisk, når nedtællingen er slut.'
                                        MessageTime = 'Gem venligst dit arbejde, og genstart inden for den tildelte tid.'
                                        TimeRemaining = 'Resterende tid:'
                                        Title = 'Genstart påkrævet'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation af App'
                                            Repair = '{Toolkit\CompanyName} - Reparation af App'
                                            Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Følgende program er ved at blive installeret:'
                                                Repair = 'Følgende applikation er ved at blive repareret:'
                                                Uninstall = 'Følgende applikation er ved at blive afinstalleret:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = 'Følgende programmer skal lukkes, før installationen kan fortsætte.« Gem venligst dit arbejde, luk programmerne, og fortsæt derefter. Alternativt kan du gemme dit arbejde og klikke på »Luk programmer«.'
                                                Repair = 'Følgende programmer skal lukkes, før reparationen kan fortsætte.« Gem venligst dit arbejde, luk programmerne, og fortsæt derefter. Alternativt kan du gemme dit arbejde og klikke på »Luk programmer«.'
                                                Uninstall = 'Følgende programmer skal lukkes, før afinstallationen kan fortsætte.« Gem venligst dit arbejde, luk programmerne, og fortsæt derefter. Alternativt kan du gemme dit arbejde og klikke på »Luk programmer«.'
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Du kan vælge at udskyde installationen, indtil udskydelsen udløber:'
                                                Repair = 'Du kan vælge at udskyde reparationen, indtil udskydelsen udløber:'
                                                Uninstall = 'Du kan vælge at udskyde afinstallationen, indtil udsættelsen udløber:'
                                            }
                                            DeferralsRemaining = 'Resterende udsættelser:'
                                            DeferralDeadline = 'Tidsfrist:'
                                            ExpiryWarning = 'Når udsættelsen er udløbet, har du ikke længere mulighed for at udskyde.'
                                            CountdownDefer = @{
                                                Install = 'Installationen fortsætter automatisk om:'
                                                Repair = 'Reparationen fortsætter automatisk i:'
                                                Uninstall = 'Afinstallationen fortsætter automatisk om:'
                                            }
                                            CountdownClose = @{
                                                Install = 'BEMÆRK: Programmet/programmerne lukkes automatisk i:'
                                                Repair = 'BEMÆRK: Programmet/programmerne lukkes automatisk i:'
                                                Uninstall = 'BEMÆRK: Programmet/programmerne lukkes automatisk i:'
                                            }
                                            ButtonClose = 'Luk &Programmer'
                                            ButtonDefer = '&Udskyde'
                                            ButtonContinue = '&Fortsæt'
                                            ButtonContinueTooltip = 'Vælg kun »Fortsæt«, når du har lukket ovenstående program(mer).'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Gem venligst dit arbejde, før du fortsætter, da de følgende programmer lukkes automatisk.'
                                                Repair = 'Gem venligst dit arbejde, før du fortsætter, da de følgende programmer lukkes automatisk.'
                                                Uninstall = 'Gem venligst dit arbejde, før du fortsætter, da de følgende programmer lukkes automatisk.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Vælg Install for at fortsætte med installationen.'
                                                Repair = 'Vælg Repair for at fortsætte med reparationen.'
                                                Uninstall = 'Vælg Afinstallation for at fortsætte med afinstallationen.'
                                            }
                                            AutomaticStartCountdown = 'Automatisk startnedtælling'
                                            DeferralsRemaining = 'Resterende udsættelser'
                                            DeferralDeadline = 'Udsættelsesfrist'
                                            ButtonLeftText = @{
                                                Install = 'Luk Apps og Installer'
                                                Repair = 'Luk Apps og Reparer'
                                                Uninstall = 'Luk Apps og Afinstaller'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Installer'
                                                Repair = 'Reparation'
                                                Uninstall = 'Afinstaller'
                                            }
                                            ButtonRightText = 'Udskyd'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Installation af App'
                                                Repair = '{Toolkit\CompanyName} - Reparation af App'
                                                Uninstall = '{Toolkit\CompanyName} - Afinstallation af App'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'de' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Installation wurde gestartet.'
                                            Repair = 'Reparatur wurde gestartet.'
                                            Uninstall = 'Deinstallation wurde gestartet.'
                                        }
                                        Complete = @{
                                            Install = 'Installation wurde abgeschlossen.'
                                            Repair = 'Reparatur wurde abgeschlossen.'
                                            Uninstall = 'Deinstallation wurde abgeschlossen.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Installation wurde abgeschlossen. Neustart erforderlich.'
                                            Repair = 'Reparatur wurde abgeschlossen. Neustart erforderlich.'
                                            Uninstall = 'Deinstallation wurde abgeschlossen. Neustart erforderlich.'
                                        }
                                        FastRetry = @{
                                            Install = 'Installation wurde nicht abgeschlossen.'
                                            Repair = 'Reparatur wurde nicht abgeschlossen.'
                                            Uninstall = 'Deinstallation wurde nicht abgeschlossen.'
                                        }
                                        Error = @{
                                            Install = 'Installation ist fehlgeschlagen.'
                                            Repair = 'Reparatur ist fehlgeschlagen.'
                                            Uninstall = 'Deinstallation ist fehlgeschlagen.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Installationsvorgang abgeschlossen werden kann.'
                                            Repair = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Reparaturvorgang abgeschlossen werden kann.'
                                            Uninstall = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Deinstallationsvorgang abgeschlossen werden kann.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation der Anwendung'
                                            Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
                                            Uninstall = '{Toolkit\CompanyName} - Neuinstallieren der Anwendung'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Der Speicherplatz reicht nicht aus, um die Installation abzuschließen:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie genügend Speicherplatz frei, um mit der Installation fortzufahren."
                                            Repair = "Der Speicherplatz reicht nicht aus, um die Reparatur von:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie ausreichend Speicherplatz frei, um mit der Reparatur fortzufahren."
                                            Uninstall = "Der Speicherplatz reicht nicht aus, um die Deinstallation von:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie ausreichend Speicherplatz frei, um mit der Deinstallation fortzufahren."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation der Anwendung'
                                            Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
                                            Uninstall = '{Toolkit\CompanyName} - Deinstallieren der Anwendung'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Element auswählen:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Installation wird ausgeführt. Bitte warten…'
                                            Repair = 'Reparatur wird ausgeführt. Bitte warten…'
                                            Uninstall = 'Deinstallation wird ausgeführt. Bitte warten…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Dieses Fenster wird automatisch geschlossen, wenn die Installation abgeschlossen ist.'
                                            Repair = 'Dieses Fenster wird automatisch geschlossen, wenn die Reparatur abgeschlossen ist.'
                                            Uninstall = 'Dieses Fenster wird automatisch geschlossen, wenn die Deinstallation abgeschlossen ist.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation der Anwendung'
                                            Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
                                            Uninstall = '{Toolkit\CompanyName} - Deinstallieren der Anwendung'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimieren'
                                        ButtonRestartNow = 'Jetzt neu starten'
                                        Message = @{
                                            Install = 'Damit die Installation abgeschlossen werden kann, müssen Sie Ihren Computer neu starten.'
                                            Repair = 'Damit die Reparatur abgeschlossen werden kann, müssen Sie Ihren Computer neu starten.'
                                            Uninstall = 'Damit die Deinstallation abgeschlossen werden kann, müssen Sie Ihren Computer neu starten.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Ihr Computer wird am Ende des Countdowns automatisch neu gestartet.'
                                        MessageTime = 'Bitte speichern Sie Ihre Arbeit und starten Sie innerhalb der vorgegebenen Zeit neu.'
                                        TimeRemaining = 'Restzeit:'
                                        Title = 'Neustart erforderlich'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation der Anwendung'
                                            Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
                                            Uninstall = '{Toolkit\CompanyName} - Deinstallieren der Anwendung'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Die folgende Anwendung wird installiert:'
                                                Repair = 'Die folgende Anwendung wird repariert:'
                                                Uninstall = 'Die folgende Anwendung wird deinstalliert:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Die folgenden Anwendungen müssen geschlossen werden, bevor die Installation fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Anwendungen und fahren dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Anwendungen schließen`“ klicken."
                                                Repair = "Die folgenden Anwendungen müssen geschlossen werden, bevor die Reparatur fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Anwendungen und fahren dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Anwendungen schließen`“ klicken."
                                                Uninstall = "Die folgenden Anwendungen müssen geschlossen werden, bevor die Deinstallation fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Anwendungen und fahren dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Anwendungen schließen`“ klicken."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Die Installation kann bis zum Ablauf der Aufschiebefrist verschoben werden:'
                                                Repair = 'Die Reparatur kann bis zum Ablauf der Aufschiebefrist verschoben werden:'
                                                Uninstall = 'Die Deinstallation kann bis zum Ablauf der Aufschiebefrist verschoben werden:'
                                            }
                                            DeferralsRemaining = 'Verbleibende Aufschiebungen:'
                                            DeferralDeadline = 'Frist:'
                                            ExpiryWarning = 'Nach Ablauf der Aufschiebung haben Sie keine Möglichkeit mehr zu verschieben.'
                                            CountdownDefer = @{
                                                Install = 'Die Installation wird automatisch fortgesetzt in:'
                                                Repair = 'Die Reparatur wird automatisch fortgesetzt in:'
                                                Uninstall = 'Die Deinstallation wird automatisch fortgesetzt in:'
                                            }
                                            CountdownClose = @{
                                                Install = 'HINWEIS: Die Anwendungen werden automatisch geschlossen in:'
                                                Repair = 'HINWEIS: Die Anwendungen werden automatisch geschlossen in:'
                                                Uninstall = 'HINWEIS: Die Anwendungen werden automatisch geschlossen in:'
                                            }
                                            ButtonClose = 'Anwendungen &schließen'
                                            ButtonDefer = '&Verschieben'
                                            ButtonContinue = '&Weiter'
                                            ButtonContinueTooltip = 'Wählen Sie erst „Weiter“, nachdem Sie die oben aufgeführten Anwendung(en) geschlossen haben.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Bitte speichern Sie Ihre Arbeit, bevor Sie fortfahren, da die folgenden Anwendungen automatisch geschlossen werden.'
                                                Repair = 'Bitte speichern Sie Ihre Arbeit, bevor Sie fortfahren, da die folgenden Anwendungen automatisch geschlossen werden.'
                                                Uninstall = 'Bitte speichern Sie Ihre Arbeit, bevor Sie fortfahren, da die folgenden Anwendungen automatisch geschlossen werden.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Wählen Sie Installieren aus, um mit der Installation fortzufahren.'
                                                Repair = 'Wählen Sie Reparieren aus, um mit der Reparatur fortzufahren.'
                                                Uninstall = 'Wählen Sie Deinstallieren aus, um mit der Deinstallation fortzufahren.'
                                            }
                                            AutomaticStartCountdown = 'Automatischer Start-Countdown'
                                            DeferralsRemaining = 'Verbleibende Aufschiebungen'
                                            DeferralDeadline = 'Aufschiebefrist'
                                            ButtonLeftText = @{
                                                Install = 'Schließen und Installieren'
                                                Repair = 'Schließen und Reparieren'
                                                Uninstall = 'Schließen und Deinstallieren'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Installieren'
                                                Repair = 'Reparatur'
                                                Uninstall = 'Deinstallieren'
                                            }
                                            ButtonRightText = 'Verschieben'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Installation der Anwendung'
                                                Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
                                                Uninstall = '{Toolkit\CompanyName} - Deinstallieren der Anwendung'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'el' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Η εγκατάσταση ξεκίνησε.'
                                            Repair = 'Η επισκευή ξεκίνησε.'
                                            Uninstall = 'Ξεκίνησε η απεγκατάσταση.'
                                        }
                                        Complete = @{
                                            Install = 'Η εγκατάσταση ολοκληρώθηκε.'
                                            Repair = 'Η επισκευή ολοκληρώθηκε.'
                                            Uninstall = 'Ολοκλήρωση της απεγκατάστασης.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Η εγκατάσταση ολοκληρώθηκε. Απαιτείται επανεκκίνηση.'
                                            Repair = 'Η επισκευή ολοκληρώθηκε. Απαιτείται επανεκκίνηση.'
                                            Uninstall = 'Η απεγκατάσταση ολοκληρώθηκε. Απαιτείται επανεκκίνηση.'
                                        }
                                        FastRetry = @{
                                            Install = 'Η εγκατάσταση δεν ολοκληρώθηκε.'
                                            Repair = 'Η επισκευή δεν ολοκληρώθηκε.'
                                            Uninstall = 'Η απεγκατάσταση δεν ολοκληρώθηκε.'
                                        }
                                        Error = @{
                                            Install = 'Η εγκατάσταση απέτυχε.'
                                            Repair = 'Η επισκευή απέτυχε.'
                                            Uninstall = 'Η απεγκατάσταση απέτυχε.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Η εκκίνηση αυτής της εφαρμογής έχει μπλοκαριστεί προσωρινά, ώστε να μπορέσει να ολοκληρωθεί μια λειτουργία εγκατάστασης.'
                                            Repair = 'Η εκκίνηση αυτής της εφαρμογής έχει προσωρινά μπλοκαριστεί ώστε να μπορεί να ολοκληρωθεί μια λειτουργία επισκευής.'
                                            Uninstall = 'Η εκκίνηση αυτής της εφαρμογής έχει μπλοκαριστεί προσωρινά ώστε να μπορεί να ολοκληρωθεί μια λειτουργία απεγκατάστασης.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Εγκατάσταση εφαρμογών'
                                            Repair = '{Toolkit\CompanyName} - Επισκευή εφαρμογής'
                                            Uninstall = '{Toolkit\CompanyName} - Απεγκατάσταση εφαρμογών'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Δεν έχετε αρκετό χώρο στο δίσκο για να ολοκληρώσετε την εγκατάσταση του:`n{0}`n`nΑπαιτούμενος χώρος: {1}MB`nΔιαθέσιμος χώρος: {2}MB`n`nΠαρακαλούμε ελευθερώστε αρκετό χώρο στο δίσκο για να συνεχίσετε την εγκατάσταση."
                                            Repair = "Δεν έχετε αρκετό χώρο στο δίσκο για να ολοκληρώσετε την επισκευή της:`n{0}`n`nΑπαιτούμενος χώρος: {1}MB`nΔιαθέσιμος χώρος: {2}MB`n`nΠαρακαλούμε ελευθερώστε αρκετό χώρο στο δίσκο για να προχωρήσετε με την επισκευή."
                                            Uninstall = "Δεν έχετε αρκετό χώρο στο δίσκο για να ολοκληρώσετε την απεγκατάσταση του:`n{0}`n`nΑπαιτούμενος χώρος: {1}MB`nΔιαθέσιμος χώρος: {2}MB`n`nΠαρακαλούμε ελευθερώστε αρκετό χώρο στο δίσκο για να προχωρήσετε στην απεγκατάσταση."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Εγκατάσταση εφαρμογών'
                                            Repair = '{Toolkit\CompanyName} - Επισκευή εφαρμογών'
                                            Uninstall = '{Toolkit\CompanyName} - Απεγκατάσταση εφαρμογών'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Επιλέξτε ένα στοιχείο:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Η εγκατάσταση βρίσκεται σε εξέλιξη. Παρακαλώ περιμένετε…'
                                            Repair = 'Επισκευή σε εξέλιξη. Παρακαλώ περιμένετε…'
                                            Uninstall = 'Απεγκατάσταση σε εξέλιξη. Παρακαλώ περιμένετε…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Αυτό το παράθυρο θα κλείσει αυτόματα όταν ολοκληρωθεί η εγκατάσταση.'
                                            Repair = 'Αυτό το παράθυρο θα κλείσει αυτόματα όταν ολοκληρωθεί η επισκευή.'
                                            Uninstall = 'Αυτό το παράθυρο θα κλείσει αυτόματα όταν ολοκληρωθεί η απεγκατάσταση.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Εγκατάσταση εφαρμογών'
                                            Repair = '{Toolkit\CompanyName} - Επισκευή εφαρμογής'
                                            Uninstall = '{Toolkit\CompanyName} - Απεγκατάσταση εφαρμογών'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Ελαχιστοποίηση'
                                        ButtonRestartNow = 'Επανεκκίνηση τώρα'
                                        Message = @{
                                            Install = 'Προκειμένου να ολοκληρωθεί η εγκατάσταση, πρέπει να επανεκκινήσετε τον υπολογιστή σας.'
                                            Repair = 'Για να ολοκληρωθεί η επισκευή, πρέπει να επανεκκινήσετε τον υπολογιστή σας.'
                                            Uninstall = 'Για να ολοκληρωθεί η απεγκατάσταση, πρέπει να επανεκκινήσετε τον υπολογιστή σας.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Ο υπολογιστής σας θα επανεκκινηθεί αυτόματα στο τέλος της αντίστροφης μέτρησης.'
                                        MessageTime = 'Παρακαλούμε αποθηκεύστε την εργασία σας και επανεκκινήστε εντός του προβλεπόμενου χρόνου.'
                                        TimeRemaining = 'Υπολειπόμενος χρόνος:'
                                        Title = 'Απαιτείται επανεκκίνηση'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Εγκατάσταση εφαρμογών'
                                            Repair = '{Toolkit\CompanyName} - Επισκευή εφαρμογής'
                                            Uninstall = '{Toolkit\CompanyName} - Απεγκατάσταση εφαρμογών'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Η ακόλουθη εφαρμογή πρόκειται να εγκατασταθεί:'
                                                Repair = 'Η ακόλουθη εφαρμογή πρόκειται να επισκευαστεί:'
                                                Uninstall = 'Η ακόλουθη εφαρμογή πρόκειται να απεγκατασταθεί:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Τα παρακάτω προγράμματα πρέπει να κλείσουν πριν προχωρήσει η εγκατάσταση.`n`nΠαρακαλούμε να αποθηκεύσετε την εργασία σας, να κλείσετε τα προγράμματα και στη συνέχεια να συνεχίσετε. Εναλλακτικά, αποθηκεύστε την εργασία σας και κάντε κλικ στο «Κλείσιμο προγραμμάτων»."
                                                Repair = "Τα παρακάτω προγράμματα πρέπει να κλείσουν πριν προχωρήσει η επισκευή.`n`nΠαρακαλούμε αποθηκεύστε την εργασία σας, κλείστε τα προγράμματα και, στη συνέχεια, συνεχίστε. Εναλλακτικά, αποθηκεύστε την εργασία σας και κάντε κλικ στο «Κλείσιμο προγραμμάτων»."
                                                Uninstall = "Τα ακόλουθα προγράμματα πρέπει να κλείσουν πριν προχωρήσει η απεγκατάσταση.`n`nΠαρακαλούμε αποθηκεύστε την εργασία σας, κλείστε τα προγράμματα και, στη συνέχεια, συνεχίστε. Εναλλακτικά, αποθηκεύστε την εργασία σας και κάντε κλικ στο «Close Programs»."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Μπορείτε να επιλέξετε να αναβάλλετε την εγκατάσταση μέχρι να λήξει η αναβολή:'
                                                Repair = 'Μπορείτε να επιλέξετε να αναβάλλετε την επισκευή μέχρι να λήξει η αναβολή:'
                                                Uninstall = 'Μπορείτε να επιλέξετε να αναβάλλετε την απεγκατάσταση έως ότου λήξει η αναβολή:'
                                            }
                                            DeferralsRemaining = 'Υπόλοιπες αναβολές:'
                                            DeferralDeadline = 'Προθεσμία:'
                                            ExpiryWarning = 'Μόλις λήξει η αναβολή, δεν θα έχετε πλέον τη δυνατότητα αναβολής.'
                                            CountdownDefer = @{
                                                Install = 'Η εγκατάσταση θα συνεχιστεί αυτόματα σε:'
                                                Repair = 'Η επισκευή θα συνεχιστεί αυτόματα σε:'
                                                Uninstall = 'Η απεγκατάσταση θα συνεχιστεί αυτόματα σε:'
                                            }
                                            CountdownClose = @{
                                                Install = 'ΣΗΜΕΙΩΣΗ: Το(τα) πρόγραμμα(α) θα κλείσει(-ουν) αυτόματα σε:'
                                                Repair = 'ΣΗΜΕΙΩΣΗ: Το(τα) πρόγραμμα(α) θα κλείσει(-ουν) αυτόματα σε:'
                                                Uninstall = 'ΣΗΜΕΙΩΣΗ: Το(τα) πρόγραμμα(α) θα κλείσει(-ουν) αυτόματα σε:'
                                            }
                                            ButtonClose = 'Κλείσιμο προγραμμάτων'
                                            ButtonDefer = '&Αναβολή'
                                            ButtonContinue = '&Συνεχίστε'
                                            ButtonContinueTooltip = 'Επιλέξτε «Συνέχεια» μόνο αφού κλείσετε την/τις παραπάνω αναφερόμενη/ες εφαρμογή/ες.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Παρακαλώ αποθηκεύστε την εργασία σας πριν συνεχίσετε, καθώς οι ακόλουθες εφαρμογές θα κλείσουν αυτόματα.'
                                                Repair = 'Παρακαλώ αποθηκεύστε την εργασία σας πριν συνεχίσετε, καθώς οι ακόλουθες εφαρμογές θα κλείσουν αυτόματα.'
                                                Uninstall = 'Παρακαλώ αποθηκεύστε την εργασία σας πριν συνεχίσετε, καθώς οι ακόλουθες εφαρμογές θα κλείσουν αυτόματα.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Παρακαλώ επιλέξτε Install για να συνεχίσετε την εγκατάσταση.'
                                                Repair = 'Παρακαλώ επιλέξτε Επισκευή για να συνεχίσετε με την επισκευή.'
                                                Uninstall = 'Παρακαλούμε επιλέξτε Απεγκατάσταση για να συνεχίσετε με την απεγκατάσταση.'
                                            }
                                            AutomaticStartCountdown = 'Αυτόματη αντίστροφη μέτρηση έναρξης'
                                            DeferralsRemaining = 'Υπόλοιπες αναβολές'
                                            DeferralDeadline = 'Προθεσμία αναβολής'
                                            ButtonLeftText = @{
                                                Install = 'Κλείσιμο εφαρμογών & εγκατάσταση'
                                                Repair = 'Κλείσιμο εφαρμογών & επισκευή'
                                                Uninstall = 'Κλείσιμο εφαρμογών & απεγκατάσταση'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Εγκατάσταση'
                                                Repair = 'Επισκευή'
                                                Uninstall = 'Απεγκατάσταση'
                                            }
                                            ButtonRightText = 'Αναβολή'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Εγκατάσταση εφαρμογών'
                                                Repair = '{Toolkit\CompanyName} - Επισκευή εφαρμογής'
                                                Uninstall = '{Toolkit\CompanyName} - Απεγκατάσταση εφαρμογών'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'es' = {
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
                                            Repair = 'Se ha bloqueado temporalmente el inicio de esta aplicación para que pueda completarse una operación de reparación.'
                                            Uninstall = 'Se ha bloqueado temporalmente el inicio de esta aplicación para que pueda completarse una operación de desinstalación.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalación de la aplicación'
                                            Repair = '{Toolkit\CompanyName} - Reparación de la aplicación'
                                            Uninstall = '{Toolkit\CompanyName} - Desinstalación de la aplicación'
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
                                            Install = '{Toolkit\CompanyName} - Instalación de App'
                                            Repair = '{Toolkit\CompanyName} - Reparación de la aplicación'
                                            Uninstall = '{Toolkit\CompanyName} - Desinstalación de la aplicación'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Seleccione un elemento:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Instalación en curso. Por favor espere…'
                                            Repair = 'Reparación en curso. Por favor espere…'
                                            Uninstall = 'Desinstalación en curso. Por favor espere…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Esta ventana se cerrará automáticamente cuando finalice la instalación.'
                                            Repair = 'Esta ventana se cerrará automáticamente cuando finalice la reparación.'
                                            Uninstall = 'Esta ventana se cerrará automáticamente cuando finalice la desinstalación.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalación de la aplicación'
                                            Repair = '{Toolkit\CompanyName} - Reparación de la aplicación'
                                            Uninstall = '{Toolkit\CompanyName} - Desinstalación de la aplicación'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimizar'
                                        ButtonRestartNow = 'Reiniciar ahora'
                                        Message = @{
                                            Install = 'Para que la instalación se complete, debe reiniciar su ordenador.'
                                            Repair = 'Para que la reparación se complete, debe reiniciar su ordenador.'
                                            Uninstall = 'Para que la desinstalación se complete, debe reiniciar su ordenador.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Su ordenador se reiniciará automáticamente al final de la cuenta atrás.'
                                        MessageTime = 'Por favor, guarde su trabajo y reinicie dentro del tiempo asignado.'
                                        TimeRemaining = 'Tiempo restante:'
                                        Title = 'Es necesario reiniciar'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalación de la aplicación'
                                            Repair = '{Toolkit\CompanyName} - Reparación de la aplicación'
                                            Uninstall = '{Toolkit\CompanyName} - Desinstalación de la aplicación'
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
                                            CountdownClose = @{
                                                Install = 'NOTA: El programa o programas se cerrarán automáticamente en:'
                                                Repair = 'NOTA: El programa o programas se cerrarán automáticamente en:'
                                                Uninstall = 'NOTA: El programa o programas se cerrarán automáticamente en:'
                                            }
                                            ButtonClose = 'Cerrar &Programas'
                                            ButtonDefer = '&Aplazar'
                                            ButtonContinue = '&Continuar'
                                            ButtonContinueTooltip = 'Sólo seleccione «Continuar» después de cerrar la(s) aplicación(es) arriba indicada(s).'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Por favor, guarde su trabajo antes de continuar ya que las siguientes aplicaciones se cerrarán automáticamente.'
                                                Repair = 'Por favor, guarde su trabajo antes de continuar ya que las siguientes aplicaciones se cerrarán automáticamente.'
                                                Uninstall = 'Por favor, guarde su trabajo antes de continuar ya que las siguientes aplicaciones se cerrarán automáticamente.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Por favor, seleccione Instalar para continuar con la instalación.'
                                                Repair = 'Por favor, seleccione Reparar para continuar con la reparación.'
                                                Uninstall = 'Por favor, seleccione Desinstalar para continuar con la desinstalación'
                                            }
                                            AutomaticStartCountdown = 'Cuenta regresiva de inicio automático'
                                            DeferralsRemaining = 'Aplazamientos restantes'
                                            DeferralDeadline = 'Fecha límite de aplazamiento'
                                            ButtonLeftText = @{
                                                Install = 'Cerrar Aplicaciones e Instalar'
                                                Repair = 'Cerrar Aplicaciones y Reparar'
                                                Uninstall = 'Cerrar Aplicaciones y Desinstalar'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Instalar'
                                                Repair = 'Reparar'
                                                Uninstall = 'Desinstalar'
                                            }
                                            ButtonRightText = 'Aplazar'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Instalación de la aplicación'
                                                Repair = '{Toolkit\CompanyName} - Reparación de la aplicación'
                                                Uninstall = '{Toolkit\CompanyName} - Desinstalación de App'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'fi' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Asennus aloitettu.'
                                            Repair = 'Korjaus aloitettu.'
                                            Uninstall = 'Asennuksen poisto aloitettu.'
                                        }
                                        Complete = @{
                                            Install = 'Asennus valmis.'
                                            Repair = 'Korjaus valmis.'
                                            Uninstall = 'Asennuksen poisto valmis.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Asennus suoritettu. Uudelleenkäynnistys vaaditaan.'
                                            Repair = 'Korjaus suoritettu. Uudelleenkäynnistys vaaditaan.'
                                            Uninstall = 'Asennuksen poisto valmis. Uudelleenkäynnistys vaaditaan.'
                                        }
                                        FastRetry = @{
                                            Install = 'Asennus ei ole valmis.'
                                            Repair = 'Korjaus ei ole valmis.'
                                            Uninstall = 'Asennuksen poisto ei ole valmis.'
                                        }
                                        Error = @{
                                            Install = 'Asennus epäonnistui.'
                                            Repair = 'Korjaus epäonnistui.'
                                            Uninstall = 'Asennuksen poisto epäonnistui.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Tämän sovelluksen käynnistäminen on tilapäisesti estetty, jotta asennustoiminto voidaan suorittaa loppuun.'
                                            Repair = 'Sovelluksen käynnistäminen on tilapäisesti estetty, jotta korjaustoiminto voidaan suorittaa loppuun.'
                                            Uninstall = 'Sovelluksen käynnistäminen on tilapäisesti estetty, jotta asennuksen poisto voidaan suorittaa loppuun.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Sovelluksen Asennus'
                                            Repair = '{Toolkit\CompanyName} - Sovelluksen Korjaus'
                                            Uninstall = '{Toolkit\CompanyName} - Sovelluksen Poisto'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Sinulla ei ole tarpeeksi levytilaa asennuksen loppuunsaattamiseen:`n{0}`n`nTilaa tarvitaan: {1}MB`nTilaa käytettävissä: {2}MB`n`nVapauta riittävästi levytilaa, jotta voit jatkaa asennusta."
                                            Repair = "Sinulla ei ole tarpeeksi levytilaa korjauksen suorittamiseen:`n{0}`n`nTilaa tarvitaan: {1}MB`nTilaa käytettävissä: {2}MB`n`nVapauta riittävästi levytilaa, jotta voit jatkaa korjausta."
                                            Uninstall = "Sinulla ei ole tarpeeksi levytilaa, jotta voit suorittaa asennuksen poistamisen loppuun:`n{0}`n`nTilaa tarvitaan: {1}MB`nTilaa käytettävissä: {2}MB`n`nVapauta riittävästi levytilaa, jotta voit jatkaa asennuksen poistamista."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Sovelluksen Asennus'
                                            Repair = '{Toolkit\CompanyName} - Sovelluksen Korjaus'
                                            Uninstall = '{Toolkit\CompanyName} - Sovelluksen Poisto'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Valitse kohde:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Asennus käynnissä. Odota…'
                                            Repair = 'Korjaus käynnissä. Odota…'
                                            Uninstall = 'Asennuksen poisto käynnissä. Odota…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Tämä ikkuna sulkeutuu automaattisesti, kun asennus on valmis.'
                                            Repair = 'Tämä ikkuna sulkeutuu automaattisesti, kun korjaus on valmis.'
                                            Uninstall = 'Tämä ikkuna sulkeutuu automaattisesti, kun asennuksen poisto on valmis.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Sovelluksen Asennus'
                                            Repair = '{Toolkit\CompanyName} - Sovelluksen Korjaus'
                                            Uninstall = '{Toolkit\CompanyName} - Sovelluksen Poisto'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimoi'
                                        ButtonRestartNow = 'Käynnistä uudelleen nyt'
                                        Message = @{
                                            Install = 'Jotta asennus voidaan suorittaa loppuun, sinun on käynnistettävä tietokoneesi uudelleen.'
                                            Repair = 'Jotta korjaus saataisiin päätökseen, sinun on käynnistettävä tietokone uudelleen.'
                                            Uninstall = 'Jotta asennuksen poisto saataisiin päätökseen, sinun on käynnistettävä tietokone uudelleen.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Tietokone käynnistyy automaattisesti uudelleen lähtölaskennan päätyttyä.'
                                        MessageTime = 'Tallenna työsi ja käynnistä tietokone uudelleen annetussa ajassa.'
                                        TimeRemaining = 'Jäljellä oleva aika:'
                                        Title = 'Uudelleenkäynnistys Vaaditaan'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Sovelluksen Asennus'
                                            Repair = '{Toolkit\CompanyName} - Sovelluksen Korjaus'
                                            Uninstall = '{Toolkit\CompanyName} - Sovelluksen Poisto'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Seuraava sovellus asennetaan pian:'
                                                Repair = 'Seuraava sovellus korjataan:'
                                                Uninstall = 'Seuraavan sovelluksen poisto on alkamassa:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Seuraavat ohjelmat on suljettava, ennen kuin asennus voi jatkua.`n`nTallenna työsi, sulje ohjelmat ja jatka sitten. Vaihtoehtoisesti tallenna työsi ja napsauta `”Sulje ohjelmat`”."
                                                Repair = "Seuraavat ohjelmat on suljettava, ennen kuin korjaus voi jatkua.`n`nTallenna työsi, sulje ohjelmat ja jatka sitten. Vaihtoehtoisesti voit tallentaa työsi ja napsauttaa `”Sulje ohjelmat`”."
                                                Uninstall = "Seuraavat ohjelmat on suljettava, ennen kuin asennuksen poisto voi jatkua.`n`nTallenna työsi, sulje ohjelmat ja jatka sitten. Vaihtoehtoisesti voit tallentaa työsi ja napsauttaa `”Sulje ohjelmat`”."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Voit halutessasi lykätä asennusta, kunnes lykkäys päättyy:'
                                                Repair = 'Voit lykätä korjausta, kunnes lykkäys päättyy:'
                                                Uninstall = 'Voit lykätä asennuksen poistamista, kunnes lykkäys päättyy:'
                                            }
                                            DeferralsRemaining = 'Jäljellä olevat lykkäykset:'
                                            DeferralDeadline = 'Määräaika:'
                                            ExpiryWarning = 'Kun lykkäys on päättynyt, et voi enää lykätä.'
                                            CountdownDefer = @{
                                                Install = 'Asennus jatkuu automaattisesti:'
                                                Repair = 'Korjaus jatkuu automaattisesti:'
                                                Uninstall = 'Asennuksen poisto jatkuu automaattisesti:'
                                            }
                                            CountdownClose = @{
                                                Install = 'HUOMAUTUS: Ohjelma(t) suljetaan automaattisesti:'
                                                Repair = 'HUOMAUTUS: Ohjelma(t) suljetaan automaattisesti:'
                                                Uninstall = 'HUOMAUTUS: Ohjelma(t) suljetaan automaattisesti:'
                                            }
                                            ButtonClose = 'Sulje &ohjelmat'
                                            ButtonDefer = '&Siirrä'
                                            ButtonContinue = '&Jatka'
                                            ButtonContinueTooltip = 'Valitse ”Jatka” vasta, kun olet sulkenut edellä luetellut sovellukset.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Tallenna työsi ennen kuin jatkat, sillä seuraavat sovellukset suljetaan automaattisesti.'
                                                Repair = 'Tallenna työsi ennen kuin jatkat, sillä seuraavat sovellukset suljetaan automaattisesti.'
                                                Uninstall = 'Tallenna työsi ennen kuin jatkat, sillä seuraavat sovellukset suljetaan automaattisesti.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Jatka asennusta valitsemalla Asenna.'
                                                Repair = 'Jatka korjausta valitsemalla Korjaa.'
                                                Uninstall = 'Jatka asennuksen poistamista valitsemalla Poista.'
                                            }
                                            AutomaticStartCountdown = 'Automaattinen käynnistyslaskenta'
                                            DeferralsRemaining = 'Jäljellä olevat lykkäykset'
                                            DeferralDeadline = 'Lykkäyksen määräaika'
                                            ButtonLeftText = @{
                                                Install = 'Sulje Sovellukset ja Asenna'
                                                Repair = 'Sulje Sovellukset ja Korjaa'
                                                Uninstall = 'Sulje Sovellukset & Poista Asennus'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Asenna'
                                                Repair = 'Korjaa'
                                                Uninstall = 'Poista Asennus'
                                            }
                                            ButtonRightText = 'Lykkää'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Sovelluksen Asennus'
                                                Repair = '{Toolkit\CompanyName} - Sovelluksen Korjaus'
                                                Uninstall = '{Toolkit\CompanyName} - Sovelluksen Poisto'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'fr' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = "L'installation a commencé."
                                            Repair = 'Réparation commencée.'
                                            Uninstall = 'Désinstallation commencée.'
                                        }
                                        Complete = @{
                                            Install = 'Installation terminée.'
                                            Repair = 'Réparation terminée.'
                                            Uninstall = 'Désinstallation terminée.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Installation terminée. Un redémarrage est nécessaire.'
                                            Repair = 'Réparation terminée. Un redémarrage est nécessaire.'
                                            Uninstall = 'Désinstallation terminée. Un redémarrage est nécessaire.'
                                        }
                                        FastRetry = @{
                                            Install = "L'installation n'est pas terminée."
                                            Repair = 'Réparation non terminée.'
                                            Uninstall = 'Désinstallation non terminée.'
                                        }
                                        Error = @{
                                            Install = 'Installation échouée.'
                                            Repair = 'Échec de la réparation.'
                                            Uninstall = 'La désinstallation a échoué.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = "Le lancement de cette application a été temporairement bloqué pour permettre l'achèvement d'une opération d'installation."
                                            Repair = "Le lancement de cette application a été temporairement bloqué pour permettre la réalisation d'une opération de réparation."
                                            Uninstall = "Le lancement de cette application a été temporairement bloqué afin qu'une opération de désinstallation puisse être menée à bien."
                                        }
                                        Subtitle = @{
                                            Install = "{Toolkit\CompanyName} - Installation de l'application"
                                            Repair = "{Toolkit\CompanyName} - Réparation de l'application"
                                            Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Vous n'avez pas assez d'espace disque pour terminer l'installation de:`n{0}`n`space requis : {1}MB`n espace disponible : {2}MB`n`nVeuillez libérer suffisamment d'espace disque pour poursuivre l'installation."
                                            Repair = "Vous n'avez pas assez d'espace disque pour terminer la réparation de:`n{0}`n`space requis : {1}MB`nEspace disponible : {2}MB`n`nVeuillez libérer suffisamment d'espace disque pour procéder à la réparation."
                                            Uninstall = "Vous n'avez pas assez d'espace disque pour terminer la désinstallation de:`n{0}`n`n`Espace requis : {1}MB`nEspace disponible : {2}MB`n`nVeuillez libérer suffisamment d'espace disque pour procéder à la désinstallation."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = "{Toolkit\CompanyName} - Installation de l'application"
                                            Repair = "{Toolkit\CompanyName} - Réparation de l'application"
                                            Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Sélectionnez un élément :'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Installation en cours. Veuillez patienter…'
                                            Repair = 'Réparation en cours. Veuillez patienter…'
                                            Uninstall = 'Désinstallation en cours. Veuillez patienter…'
                                        }
                                        MessageDetail = @{
                                            Install = "Cette fenêtre se fermera automatiquement lorsque l'installation sera terminée."
                                            Repair = "Cette fenêtre se fermera automatiquement lorsque la réparation sera terminée."
                                            Uninstall = "Cette fenêtre se fermera automatiquement lorsque la désinstallation sera terminée."
                                        }
                                        Subtitle = @{
                                            Install = "{Toolkit\CompanyName} - Installation de l'application"
                                            Repair = "{Toolkit\CompanyName} - Réparation de l'application"
                                            Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Réduire'
                                        ButtonRestartNow = 'Redémarrer maintenant'
                                        Message = @{
                                            Install = "Pour que l'installation soit terminée, vous devez redémarrer votre ordinateur."
                                            Repair = "Pour que la réparation soit terminée, vous devez redémarrer votre ordinateur."
                                            Uninstall = "Pour que la désinstallation soit terminée, vous devez redémarrer votre ordinateur."
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Votre ordinateur sera automatiquement redémarré à la fin du compte à rebours.'
                                        MessageTime = 'Veuillez sauvegarder votre travail et redémarrer dans le temps imparti.'
                                        TimeRemaining = 'Temps restant:'
                                        Title = 'Redémarrage requis'
                                        Subtitle = @{
                                            Install = "{Toolkit\CompanyName} - Installation de l'application"
                                            Repair = "{Toolkit\CompanyName} - Réparation de l'application"
                                            Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = "L'application suivante est sur le point d'être installée:"
                                                Repair = "L'application suivante est sur le point d'être réparée:"
                                                Uninstall = "L'application suivante est sur le point d'être désinstallée:"
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Les programmes suivants doivent être fermés avant que l'installation ne puisse avoir lieu.`n`nVeuillez enregistrer votre travail, fermer les programmes, puis continuer. Vous pouvez également enregistrer votre travail et cliquer sur « Fermer les programmes »."
                                                Repair = "The following programs must be closed before the repair can proceed.`n`nPlease save your work, close the programs, and then continue. Vous pouvez également enregistrer votre travail et cliquer sur « Fermer les programmes »."
                                                Uninstall = "Les programmes suivants doivent être fermés pour que la désinstallation puisse avoir lieu.`n`n Veuillez enregistrer votre travail, fermer les programmes, puis continuer. Vous pouvez également enregistrer votre travail et cliquer sur « Fermer les programmes »."
                                            }
                                            ExpiryMessage = @{
                                                Install = "Vous pouvez choisir de différer l'installation jusqu'à l'expiration du délai:"
                                                Repair = "Vous pouvez choisir de différer la réparation jusqu'à l'expiration du délai:"
                                                Uninstall = "Vous pouvez choisir de différer la désinstallation jusqu'à l'expiration du délai:"
                                            }
                                            DeferralsRemaining = 'Reports restants:'
                                            DeferralDeadline = 'Date limite:'
                                            ExpiryWarning = "Une fois le report expiré, vous n'aurez plus la possibilité de le différer."
                                            CountdownDefer = @{
                                                Install = "L'installation se poursuivra automatiquement dans:"
                                                Repair = "La réparation se poursuivra automatiquement dans:"
                                                Uninstall = "La désinstallation se poursuivra automatiquement dans :"
                                            }
                                            CountdownClose = @{
                                                Install = 'NOTE: Le(s) programme(s) sera(ont) automatiquement fermé(s) dans:'
                                                Repair = 'NOTE: Le(s) programme(s) sera(ont) automatiquement fermé(s) dans:'
                                                Uninstall = 'NOTE: Le(s) programme(s) sera(ont) automatiquement fermé(s) dans:'
                                            }
                                            ButtonClose = 'Fermer &Programmes'
                                            ButtonDefer = '&Report'
                                            ButtonContinue = '&Continuer'
                                            ButtonContinueTooltip = "Ne sélectionnez « Continuer » qu'après avoir fermé la ou les applications listées ci-dessus."
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Veuillez sauvegarder votre travail avant de continuer car les applications suivantes seront fermées automatiquement.'
                                                Repair = 'Veuillez sauvegarder votre travail avant de continuer car les applications suivantes seront fermées automatiquement.'
                                                Uninstall = 'Veuillez sauvegarder votre travail avant de continuer car les applications suivantes seront fermées automatiquement.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = "Veuillez sélectionner Installer pour poursuivre l'installation."
                                                Repair = "Veuillez sélectionner Réparer pour poursuivre la réparation."
                                                Uninstall = "Veuillez sélectionner Désinstaller pour poursuivre la désinstallation."
                                            }
                                            AutomaticStartCountdown = 'Compte à rebours de démarrage automatique'
                                            DeferralsRemaining = 'Reports restants'
                                            DeferralDeadline = 'Date limite de report'
                                            ButtonLeftText = @{
                                                Install = 'Fermer les applications et installer'
                                                Repair = 'Fermer les applications et réparer'
                                                Uninstall = 'Fermer les applications et désinstaller'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Installer'
                                                Repair = 'Réparer'
                                                Uninstall = 'Désinstaller'
                                            }
                                            ButtonRightText = 'Différer'
                                            Subtitle = @{
                                                Install = "{Toolkit\CompanyName} - Installation de l'application"
                                                Repair = "{Toolkit\CompanyName} - Réparation de l'application"
                                                Uninstall = "{Toolkit\CompanyName} - Désinstallation de l'application"
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'he' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'ההתקנה החלה.'
                                            Repair = 'התיקון החל.'
                                            Uninstall = 'הסרת ההתקנה החלה.'
                                        }
                                        Complete = @{
                                            Install = 'ההתקנה הושלמה.'
                                            Repair = 'התיקון הושלם.'
                                            Uninstall = 'הסרת ההתקנה הושלמה.'
                                        }
                                        RestartRequired = @{
                                            Install = 'ההתקנה הושלמה. נדרש אתחול מחדש.'
                                            Repair = 'התיקון הושלם. נדרש אתחול מחדש.'
                                            Uninstall = 'הסרת ההתקנה הושלמה. נדרש אתחול מחדש.'
                                        }
                                        FastRetry = @{
                                            Install = 'ההתקנה לא הושלמה.'
                                            Repair = 'התיקון לא הושלם.'
                                            Uninstall = 'הסרת ההתקנה לא הושלמה.'
                                        }
                                        Error = @{
                                            Install = 'ההתקנה נכשלה.'
                                            Repair = 'התיקון נכשל.'
                                            Uninstall = 'הסרת ההתקנה נכשלה.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'הפעלת אפליקציה זו נחסמה זמנית כדי שניתן יהיה להשלים פעולת התקנה.'
                                            Repair = 'הפעלת אפליקציה זו נחסמה זמנית כדי שניתן יהיה להשלים פעולת תיקון.'
                                            Uninstall = 'הפעלת אפליקציה זו נחסמה באופן זמני כדי שניתן יהיה להשלים פעולת הסרת ההתקנה.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - התקנת אפליקציה'
                                            Repair = '{Toolkit\CompanyName} - תיקון אפליקציות'
                                            Uninstall = '{Toolkit\CompanyName} - הסרת אפליקציה'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "אין לך מספיק שטח דיסק כדי להשלים את ההתקנה של:`n{0}`n`nSpace נדרש: {1}MB`nSpace זמין: {2}MB`n`nאנא פנה מספיק שטח דיסק כדי להמשיך עם ההתקנה."
                                            Repair = "אין לך מספיק שטח דיסק כדי להשלים את התיקון של:`n{0}`n`nSpace נדרש: {1}MB`nSpace זמין: {2}MB`n`nאנא פנה מספיק שטח דיסק כדי להמשיך עם את התיקון."
                                            Uninstall = "אין לך מספיק שטח דיסק כדי להשלים את הסרת ההתקנה של:`n{0}`n`nSpace נדרש: {1}MB`nSpace זמין: {2}MB`n`nאנא פנה מספיק שטח דיסק כדי להמשיך עם הסרת ההתקנה."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - התקנת אפליקציה'
                                            Repair = '{Toolkit\CompanyName} - תיקון אפליקציות'
                                            Uninstall = '{Toolkit\CompanyName} - הסרת אפליקציה'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = ':בחר פריט'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'התקנה בעיצומה. אנא המתן…'
                                            Repair = 'תיקון מתבצע. אנא המתן…'
                                            Uninstall = 'הסרת ההתקנה מתבצעת. אנא המתן…'
                                        }
                                        MessageDetail = @{
                                            Install = 'חלון זה ייסגר אוטומטית עם השלמת ההתקנה.'
                                            Repair = 'חלון זה ייסגר אוטומטית עם השלמת התיקון.'
                                            Uninstall = 'חלון זה ייסגר אוטומטית עם השלמת הסרת ההתקנה.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - התקנת אפליקציה'
                                            Repair = '{Toolkit\CompanyName} - תיקון אפליקציות'
                                            Uninstall = '{Toolkit\CompanyName} - הסרת אפליקציה'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'לְצַמְצֵם'
                                        ButtonRestartNow = 'הפעל מחדש עכשיו'
                                        Message = @{
                                            Install = 'על מנת שההתקנה תסתיים, עליך להפעיל מחדש את המחשב.'
                                            Repair = 'על מנת שהתיקון יסתיים, עליך להפעיל מחדש את המחשב.'
                                            Uninstall = 'כדי שההסרה תסתיים, עליך להפעיל מחדש את המחשב.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'המחשב שלך יופעל מחדש באופן אוטומטי בתום הספירה לאחור.'
                                        MessageTime = 'נא לשמור את עבודתך ולהתחיל מחדש תוך הזמן המוקצב.'
                                        TimeRemaining = 'זמן שנותר:'
                                        Title = 'נדרשת הפעלה מחדש'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - התקנת אפליקציה'
                                            Repair = '{Toolkit\CompanyName} - תיקון אפליקציות'
                                            Uninstall = '{Toolkit\CompanyName} - הסרת אפליקציה'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'האפליקציה הבאה עומדת להיות מותקנת:'
                                                Repair = 'האפליקציה הבאה עומדת לעבור תיקון:'
                                                Uninstall = 'האפליקציה הבאה עומדת להיות מוסרת:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "יש לסגור את התוכניות הבאות לפני שניתן להמשיך בהתקנה.`n`nנא שמור את עבודתך, סגור את התוכניות ולאחר מכן המשך. לחלופין, שמור את עבודתך ולחץ על `"סגור תוכניות`"."
                                                Repair = "יש לסגור את התוכניות הבאות לפני שניתן להמשיך בתיקון.`n`nנא שמור את עבודתך, סגור את התוכניות ולאחר מכן המשך. לחלופין, שמור את עבודתך ולחץ על `"סגור תוכניות`"."
                                                Uninstall = "יש לסגור את התוכניות הבאות לפני שניתן יהיה להמשיך בהסרת ההתקנה.`n`nנא שמור את עבודתך, סגור את התוכניות ולאחר מכן המשך. לחלופין, שמור את עבודתך ולחץ על `"סגור תוכניות`"."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'אתה יכול לבחור לדחות את ההתקנה עד לפקיעת הדחייה:'
                                                Repair = 'אתה יכול לבחור לדחות את התיקון עד לפקיעת הדחייה:'
                                                Uninstall = 'אתה יכול לבחור לדחות את הסרת ההתקנה עד לפקיעת הדחייה:'
                                            }
                                            DeferralsRemaining = 'דחיות שנותרו:'
                                            DeferralDeadline = 'מוֹעֵד אַחֲרוֹן:'
                                            ExpiryWarning = 'לאחר שהדחיה פג, לא תהיה לך יותר אפשרות לדחות.'
                                            CountdownDefer = @{
                                                Install = 'ההתקנה תמשיך אוטומטית ב:'
                                                Repair = 'התיקון ימשיך אוטומטית בעוד:'
                                                Uninstall = 'הסרת ההתקנה תימשך אוטומטית בעוד:'
                                            }
                                            CountdownClose = @{
                                                Install = 'הערה: התוכניות ייסגרו אוטומטית ב:'
                                                Repair = 'הערה: התוכניות ייסגרו אוטומטית ב:'
                                                Uninstall = 'הערה: התוכניות ייסגרו אוטומטית ב:'
                                            }
                                            ButtonClose = 'סגור תוכניות'
                                            ButtonDefer = 'לִדחוֹת'
                                            ButtonContinue = 'לְהַמשִׁיך'
                                            ButtonContinueTooltip = 'בחר "המשך" רק לאחר סגירת האפליקציות המפורטות לעיל.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'אנא שמור את עבודתך לפני שתמשיך שכן היישומים הבאים ייסגרו אוטומטית.'
                                                Repair = 'אנא שמור את עבודתך לפני שתמשיך שכן היישומים הבאים ייסגרו אוטומטית.'
                                                Uninstall = 'אנא שמור את עבודתך לפני שתמשיך שכן היישומים הבאים ייסגרו אוטומטית.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'אנא בחר התקן כדי להמשיך בהתקנה.'
                                                Repair = 'אנא בחר תיקון כדי להמשיך בתיקון.'
                                                Uninstall = 'אנא בחר הסר כדי להמשיך בהסרת ההתקנה.'
                                            }
                                            AutomaticStartCountdown = 'ספירה לאחור אוטומטית להתחלה'
                                            DeferralsRemaining = 'דחיות שנותרו'
                                            DeferralDeadline = 'מועד אחרון לדחייה'
                                            ButtonLeftText = @{
                                                Install = 'סגור אפליקציות והתקן'
                                                Repair = 'סגור אפליקציות ותיקון'
                                                Uninstall = 'סגור אפליקציות והסר התקנה'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'לְהַתְקִין'
                                                Repair = 'לְתַקֵן'
                                                Uninstall = 'הסר את ההתקנה'
                                            }
                                            ButtonRightText = 'לִדחוֹת'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - התקנת אפליקציה'
                                                Repair = '{Toolkit\CompanyName} - תיקון אפליקציות'
                                                Uninstall = '{Toolkit\CompanyName} - הסרת אפליקציה'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'hu' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'A telepítés megkezdődött.'
                                            Repair = 'A javítás megkezdődött.'
                                            Uninstall = 'Az eltávolítás megkezdődött.'
                                        }
                                        Complete = @{
                                            Install = 'A telepítés befejeződött.'
                                            Repair = 'Javítás befejeződött.'
                                            Uninstall = 'Az eltávolítás befejeződött.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Telepítés befejeződött. Újraindítás szükséges.'
                                            Repair = 'Javítás befejeződött. Újraindítás szükséges.'
                                            Uninstall = 'Az eltávolítás befejeződött. Újraindítás szükséges.'
                                        }
                                        FastRetry = @{
                                            Install = 'Telepítés nem fejeződött be.'
                                            Repair = 'Javítás nem fejeződött be.'
                                            Uninstall = 'Az eltávolítás nem fejeződött be.'
                                        }
                                        Error = @{
                                            Install = 'Telepítés sikertelen.'
                                            Repair = 'A javítás sikertelen.'
                                            Uninstall = 'Az eltávolítás sikertelen.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Az alkalmazás indítása ideiglenesen blokkolva van, hogy egy telepítési művelet befejeződhessen.'
                                            Repair = 'Az alkalmazás indítása ideiglenesen blokkolva van, hogy egy javítási művelet befejeződhessen.'
                                            Uninstall = 'Az alkalmazás indítása ideiglenesen blokkolva van, hogy egy eltávolítási művelet befejeződhessen.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Alkalmazás Telepítése'
                                            Repair = '{Toolkit\CompanyName} - Alkalmazás Javítása'
                                            Uninstall = '{Toolkit\CompanyName} - Alkalmazás Eltávolítása'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Nincs elég lemezterület a telepítés befejezéséhez:`n{0}`n`nSzükséges hely: {1}MB`n`: {2}MB`n`nKérjük, szabadítson fel elegendő lemezterületet a telepítés folytatásához."
                                            Repair = "Nincs elég lemezterület a javítás befejezéséhez:`n{0}`n`nSzükséges hely: {1}MB`n`: {2}MB`n`nKérem, szabadítson fel elegendő lemezterületet a javítás folytatásához."
                                            Uninstall = "Nincs elég lemezterület a következő eltávolításának befejezéséhez:`n{0}`n`nSzükséges hely: {1}MB`n: {2}MB`n`nKérem, szabadítson fel elegendő lemezterületet az eltávolítás folytatásához."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Alkalmazás Telepítése'
                                            Repair = '{Toolkit\CompanyName} - Alkalmazás Javítása'
                                            Uninstall = '{Toolkit\CompanyName} - Alkalmazás Eltávolítása'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Válasszon egy elemet:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Telepítés folyamatban. Kérjük várjon…'
                                            Repair = 'Javítás folyamatban. Kérjük várjon…'
                                            Uninstall = 'Eltávolítás folyamatban. Kérjük várjon…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Ez az ablak automatikusan bezáródik, ha a telepítés befejeződött.'
                                            Repair = 'Ez az ablak automatikusan bezáródik, ha a javítás befejeződött.'
                                            Uninstall = 'Ez az ablak automatikusan bezáródik, amikor az eltávolítás befejeződött.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Alkalmazás Telepítése'
                                            Repair = '{Toolkit\CompanyName} - Alkalmazás Javítása'
                                            Uninstall = '{Toolkit\CompanyName} - Alkalmazás Eltávolítása'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimalizálás'
                                        ButtonRestartNow = 'Újraindítás most'
                                        Message = @{
                                            Install = 'A telepítés befejezéséhez újra kell indítania a számítógépet.'
                                            Repair = 'A javítás befejezéséhez újra kell indítania a számítógépet.'
                                            Uninstall = 'Az eltávolítás befejezéséhez újra kell indítania a számítógépet.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'A visszaszámlálás végén a számítógép automatikusan újraindul.'
                                        MessageTime = 'Kérjük, mentse el munkáját, és indítsa újra a megadott időn belül.'
                                        TimeRemaining = 'A hátralévő idő:'
                                        Title = 'Újraindítás szükséges'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Alkalmazás Telepítése'
                                            Repair = '{Toolkit\CompanyName} - Alkalmazás Javítása'
                                            Uninstall = '{Toolkit\CompanyName} - Alkalmazás Eltávolítása'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'A következő alkalmazás telepítése folyamatban van:'
                                                Repair = 'A következő alkalmazás javításra kerül:'
                                                Uninstall = 'A következő alkalmazás eltávolítása folyamatban van:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "A következő programokat be kell zárni, mielőtt a telepítés folytatódhat.`n`nKérjük, mentse el munkáját, zárja be a programokat, majd folytassa. Alternatív megoldásként mentse el a munkáját, és kattintson a `„Programok bezárása`” gombra."
                                                Repair = "A következő programokat be kell zárni, mielőtt a javítás folytatódhat.`n`nKérjük, mentse el munkáját, zárja be a programokat, majd folytassa. Másik lehetőségként mentse el a munkáját, és kattintson a `„Programok bezárása`” gombra."
                                                Uninstall = "Az alábbi programokat be kell zárni, mielőtt az eltávolítás folytatódhat.`n`nKérjük, mentse el munkáját, zárja be a programokat, majd folytassa. Másik lehetőségként mentse el a munkáját, és kattintson a `„Programok bezárása`” gombra."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'A telepítést a halasztás lejártáig elhalaszthatja:'
                                                Repair = 'A javítást a halasztás lejártáig elhalaszthatja:'
                                                Uninstall = 'Az eltávolítást elhalaszthatja a halasztás lejártáig:'
                                            }
                                            DeferralsRemaining = 'Maradék halasztások:'
                                            DeferralDeadline = 'Határidő:'
                                            ExpiryWarning = 'Ha a halasztás lejár, többé nem lesz lehetősége a halasztásra.'
                                            CountdownDefer = @{
                                                Install = 'A telepítés automatikusan folytatódik:'
                                                Repair = 'A javítás automatikusan folytatódik:'
                                                Uninstall = 'Az eltávolítás automatikusan folytatódik:'
                                            }
                                            CountdownClose = @{
                                                Install = 'MEGJEGYZÉS: A program(ok) automatikusan bezárul(nak):'
                                                Repair = 'MEGJEGYZÉS: A program(ok) automatikusan bezárul(nak):'
                                                Uninstall = 'MEGJEGYZÉS: A program(ok) automatikusan bezárul(nak):'
                                            }
                                            ButtonClose = 'Alkalmazások bezárása'
                                            ButtonDefer = '&Elhalasztás'
                                            ButtonContinue = '&Folytatás'
                                            ButtonContinueTooltip = 'Csak a fent felsorolt alkalmazás(ok) bezárása után válassza a „Folytatás” lehetőséget.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Kérjük, mentse el munkáját, mielőtt folytatná, mivel a következő alkalmazások automatikusan bezárásra kerülnek.'
                                                Repair = 'Kérjük, mentse el munkáját, mielőtt folytatná, mivel a következő alkalmazások automatikusan bezárásra kerülnek.'
                                                Uninstall = 'Kérjük, mentse el munkáját, mielőtt folytatná, mivel a következő alkalmazások automatikusan bezárásra kerülnek.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'A telepítés folytatásához válassza a Telepítés lehetőséget.'
                                                Repair = 'A javítás folytatásához válassza a Repair (Javítás) lehetőséget.'
                                                Uninstall = 'Kérjük, válassza az Eltávolítás lehetőséget az eltávolítás folytatásához.'
                                            }
                                            AutomaticStartCountdown = 'Automatikus indítási visszaszámlálás'
                                            DeferralsRemaining = 'Fennmaradó halasztások'
                                            DeferralDeadline = 'Halasztási határidő'
                                            ButtonLeftText = @{
                                                Install = 'Alkalmazások Bezárása és Telepítése'
                                                Repair = 'Alkalmazások Bezárása és Javítása'
                                                Uninstall = 'Alkalmazások Bezárása és Eltávolítása'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Telepítés'
                                                Repair = 'Javítás'
                                                Uninstall = 'Eltávolítás'
                                            }
                                            ButtonRightText = 'Halasztás'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Alkalmazás Telepítése'
                                                Repair = '{Toolkit\CompanyName} - Alkalmazás Javítása'
                                                Uninstall = '{Toolkit\CompanyName} - Alkalmazás Eltávolítása'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'it' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Installazione avviata.'
                                            Repair = 'La riparazione è iniziata.'
                                            Uninstall = 'La disinstallazione è iniziata.'
                                        }
                                        Complete = @{
                                            Install = 'Installazione completata.'
                                            Repair = 'Riparazione completata.'
                                            Uninstall = 'Disinstallazione completata.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Installazione completata. È necessario un riavvio.'
                                            Repair = 'Riparazione completata. È richiesto un riavvio.'
                                            Uninstall = 'Disinstallazione completata. È necessario un riavvio.'
                                        }
                                        FastRetry = @{
                                            Install = 'Installazione non completata.'
                                            Repair = 'Riparazione non completata.'
                                            Uninstall = 'Disinstallazione non completata.'
                                        }
                                        Error = @{
                                            Install = 'Installazione fallita.'
                                            Repair = 'Riparazione fallita.'
                                            Uninstall = 'Disinstallazione non riuscita.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = "L'avvio di questa applicazione è stato temporaneamente bloccato per consentire il completamento di un'operazione di installazione."
                                            Repair = "L'avvio di questa applicazione è stato temporaneamente bloccato per consentire il completamento di un'operazione di riparazione."
                                            Uninstall = "L'avvio di questa applicazione è stato temporaneamente bloccato per consentire il completamento di un'operazione di disinstallazione."
                                        }
                                        Subtitle = @{
                                            Install = "{Toolkit\CompanyName} - Installazione dell'applicazione."
                                            Repair = "{Toolkit\CompanyName} - Riparazione dell'applicazione."
                                            Uninstall = "{Toolkit\CompanyName} - Disinstallazione App."
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Non si dispone di spazio su disco sufficiente per completare l'installazione di:`n{0}`n`nSpazio richiesto: {1}MB`nSpazio disponibile: {2}MB`n`nPer favore, liberi spazio su disco sufficiente per procedere con l'installazione."
                                            Repair = "Non si dispone di spazio su disco sufficiente per completare la riparazione di:`n{0}`n`nSpazio richiesto: {1}MB`nSpazio disponibile: {2}MB`n`nPer favore, liberi spazio su disco sufficiente per procedere con la riparazione."
                                            Uninstall = "Non si dispone di spazio su disco sufficiente per completare la disinstallazione di:`n{0}`n`nSpazio richiesto: {1}MB`nSpazio disponibile: {2}MB`n`nPer favore, liberi spazio su disco sufficiente per procedere con la disinstallazione."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = "{Toolkit\CompanyName} - Installazione dell'applicazione."
                                            Repair = "{Toolkit\CompanyName} - Riparazione app."
                                            Uninstall = "{Toolkit\CompanyName} - Disinstallazione app."
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Seleziona un elemento:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Installazione in corso. Attendere prego…'
                                            Repair = 'Riparazione in corso. Attendere…'
                                            Uninstall = 'Disinstallazione in corso. Attendere prego…'
                                        }
                                        MessageDetail = @{
                                            Install = "Questa finestra si chiuderà automaticamente al termine dell'installazione."
                                            Repair = "Questa finestra si chiuderà automaticamente al termine della riparazione."
                                            Uninstall = "Questa finestra si chiuderà automaticamente al termine della disinstallazione."
                                        }
                                        Subtitle = @{
                                            Install = "{Toolkit\CompanyName} - Installazione di applicazioni."
                                            Repair = "{Toolkit\CompanyName} - Riparazione dell'applicazione."
                                            Uninstall = "{Toolkit\CompanyName} - Disinstallazione dell'App."
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Riduci a icona.'
                                        ButtonRestartNow = 'Riavvia ora.'
                                        Message = @{
                                            Install = "Per completare l'installazione, deve riavviare il computer."
                                            Repair = "Affinché la riparazione sia completata, deve riavviare il computer."
                                            Uninstall = "Affinché la disinstallazione sia completata, deve riavviare il computer."
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Il computer verrà automaticamente riavviato al termine del conto alla rovescia.'
                                        MessageTime = 'Salvi il suo lavoro e riavvii entro il tempo stabilito.'
                                        TimeRemaining = 'Tempo rimanente:'
                                        Title = 'Riavvio richiesto'
                                        Subtitle = @{
                                            Install = "{Toolkit\CompanyName} - Installazione di un'applicazione."
                                            Repair = "{Toolkit\CompanyName} - Riparazione dell'applicazione."
                                            Uninstall = "{Toolkit\CompanyName} - Disinstallazione App."
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'La seguente applicazione sta per essere installata:'
                                                Repair = 'La seguente applicazione sta per essere riparata:'
                                                Uninstall = 'La seguente applicazione sta per essere disinstallata:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "I seguenti programmi devono essere chiusi prima che l'installazione possa procedere.`n'nSalvi il suo lavoro, chiuda i programmi e poi continui. In alternativa, salvi il suo lavoro e clicchi su `“Chiudi programmi`”."
                                                Repair = "I seguenti programmi devono essere chiusi prima che la riparazione possa procedere.`n'nSalvi il suo lavoro, chiuda i programmi e poi continui. In alternativa, salvi il suo lavoro e clicchi su `“Chiudi programmi`”."
                                                Uninstall = "I seguenti programmi devono essere chiusi prima di procedere alla disinstallazione.`n'nSalvi il suo lavoro, chiuda i programmi e poi continui. In alternativa, salvi il suo lavoro e clicchi su `“Chiudi programmi`”."
                                            }
                                            ExpiryMessage = @{
                                                Install = "Può scegliere di rinviare l'installazione fino alla scadenza del rinvio:"
                                                Repair = "Può scegliere di rinviare la riparazione fino alla scadenza del rinvio:"
                                                Uninstall = "Può scegliere di rinviare la disinstallazione fino alla scadenza del rinvio:"
                                            }
                                            DeferralsRemaining = 'Rinvii rimanenti:'
                                            DeferralDeadline = 'Scadenza:'
                                            ExpiryWarning = 'Una volta scaduto il rinvio, non avrà più la possibilità di rinviare.'
                                            CountdownDefer = @{
                                                Install = "L'installazione continuerà automaticamente tra:"
                                                Repair = "La riparazione continuerà automaticamente in:"
                                                Uninstall = "La disinstallazione continuerà automaticamente tra:"
                                            }
                                            CountdownClose = @{
                                                Install = 'NOTA: I programmi verranno chiusi automaticamente in:'
                                                Repair = 'NOTA: I programmi verranno chiusi automaticamente in:'
                                                Uninstall = 'NOTA: I programmi verranno chiusi automaticamente in:'
                                            }
                                            ButtonClose = 'Chiudere &Programmi'
                                            ButtonDefer = '&Rinviare'
                                            ButtonContinue = '&Continuare'
                                            ButtonContinueTooltip = 'Selezioni “Continua” solo dopo aver chiuso le applicazioni sopra elencate.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Salvi il suo lavoro prima di continuare, perché le applicazioni seguenti verranno chiuse automaticamente.'
                                                Repair = 'Salvi il suo lavoro prima di continuare, perché le applicazioni seguenti verranno chiuse automaticamente.'
                                                Uninstall = 'Salvi il suo lavoro prima di continuare, perché le applicazioni seguenti verranno chiuse automaticamente.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = "Selezioni Install per continuare l'installazione."
                                                Repair = "Selezioni Repair per continuare con la riparazione."
                                                Uninstall = "Selezioni Disinstallazione per proseguire con la disinstallazione."
                                            }
                                            AutomaticStartCountdown = "Conto alla rovescia per l'avvio automatico"
                                            DeferralsRemaining = 'Rimanenti differimenti'
                                            DeferralDeadline = 'Scadenza di rinvio'
                                            ButtonLeftText = @{
                                                Install = "Chiudi le applicazioni e installa."
                                                Repair = "Chiudi applicazioni e ripara."
                                                Uninstall = "Chiudere le applicazioni e disinstallare."
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Installa'
                                                Repair = 'Riparare'
                                                Uninstall = 'Disinstalla'
                                            }
                                            ButtonRightText = 'Rimandare'
                                            Subtitle = @{
                                                Install = "{Toolkit\CompanyName} - Installazione di un'applicazione."
                                                Repair = "{Toolkit\CompanyName} - Riparazione dell'applicazione."
                                                Uninstall = "{Toolkit\CompanyName} - Disinstallazione App."
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'ja' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'インストールが開始されました。'
                                            Repair = '修復が開始されました。'
                                            Uninstall = 'アンインストールが開始されました。'
                                        }
                                        Complete = @{
                                            Install = 'インストールが完了しました。'
                                            Repair = '修復が完了しました。'
                                            Uninstall = 'アンインストールが完了しました。'
                                        }
                                        RestartRequired = @{
                                            Install = 'インストールが完了しました。再起動が必要です。'
                                            Repair = '修復が完了しました。再起動が必要です。'
                                            Uninstall = 'アンインストールが完了しました。再起動が必要です。'
                                        }
                                        FastRetry = @{
                                            Install = 'インストールが完了していません。'
                                            Repair = '修復が完了していません。'
                                            Uninstall = 'アンインストールが完了していません。'
                                        }
                                        Error = @{
                                            Install = 'インストールに失敗しました。'
                                            Repair = '修復に失敗しました。'
                                            Uninstall = 'アンインストールに失敗しました。'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'インストール操作を完了させるため、このアプリケーションの起動が一時的にブロックされました。'
                                            Repair = '修復操作を完了させるため、このアプリケーションの起動が一時的にブロックされました。'
                                            Uninstall = 'アンインストール操作を完了させるため、このアプリケーションの起動が一時的にブロックされました。'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - アプリケーションのインストール'
                                            Repair = '{Toolkit\CompanyName} - アプリケーションの修復'
                                            Uninstall = '{Toolkit\CompanyName} - アプリケーションのアンインストール'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "インストールを完了するには十分なディスク領域がありません。:`n{0}`n`n必要なディスク領域: {1}MB`n使用可能なディスク領域: {2}MB`n`nインストールを続行するには、十分なディスク領域を確保してください。"
                                            Repair = "修復を完了するには十分なディスク領域がありません。`n{0}`n`n必要な容量: {1}MB`n使用可能な容量: {2}MB`n`n修復を実行するには、十分なディスク領域を確保してください。"
                                            Uninstall = "アンインストールを完了するにはディスク容量が不足しています。:`n{0}`n`n必要な容量: {1}MB`n使用可能な容量: {2}MB`n`nアンインストールを実行するには、十分なディスク容量を確保してください。"
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - アプリケーションのインストール'
                                            Repair = '{Toolkit\CompanyName} - アプリケーションの修復'
                                            Uninstall = '{Toolkit\CompanyName} - アプリケーションのアンインストール'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = '項目を選択してください:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'インストール中です。しばらくお待ちください…'
                                            Repair = '修復中です。しばらくお待ちください…'
                                            Uninstall = 'アンインストール中です。しばらくお待ちください…'
                                        }
                                        MessageDetail = @{
                                            Install = 'インストールが完了すると、このウィンドウは自動的に閉じられます。'
                                            Repair = '修復が完了すると、このウィンドウは自動的に閉じられます。'
                                            Uninstall = 'アンインストールが完了すると、このウィンドウは自動的に閉じられます。'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - アプリケーションのインストール'
                                            Repair = '{Toolkit\CompanyName} - アプリケーションの修復'
                                            Uninstall = '{Toolkit\CompanyName} - アプリケーションのアンインストール'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = '最小化'
                                        ButtonRestartNow = '今すぐ再起動'
                                        Message = @{
                                            Install = 'インストールを完了するには、コンピュータを再起動する必要があります。'
                                            Repair = '修復を完了するには、コンピュータを再起動する必要があります。'
                                            Uninstall = 'アンインストールを完了するには、コンピュータを再起動する必要があります。'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'カウントダウンの終了時にコンピュータが自動的に再起動されます。'
                                        MessageTime = '作業内容を保存し、指定時間内に再起動してください。'
                                        TimeRemaining = '残り時間:'
                                        Title = '再起動が必要です'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - アプリケーションのインストール'
                                            Repair = '{Toolkit\CompanyName} - アプリケーションの修復'
                                            Uninstall = '{Toolkit\CompanyName} - アプリケーションのアンインストール'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = '次のアプリケーションがインストールされようとしています。'
                                                Repair = '以下のアプリケーションを修理中です。'
                                                Uninstall = '次のアプリケーションがアンインストールされようとしています。'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "インストールを続行するには、次のプログラムを閉じなければなりません。`n`n作業内容を保存し、プログラムを閉じてから続行してください。または、作業内容を保存し、[プログラムの終了] をクリックしてください。"
                                                Repair = "修復を続行するには、次のプログラムを閉じなければなりません。`n`n作業内容を保存し、プログラムを閉じてから続行してください。または、作業内容を保存し、[プログラムの終了] をクリックしてください。"
                                                Uninstall = "アンインストールを続行するには、次のプログラムを閉じてください。`n`n作業内容を保存し、プログラムを閉じてから続行してください。または、作業内容を保存し、[プログラムの終了] をクリックしてください。"
                                            }
                                            ExpiryMessage = @{
                                                Install = '延期期間が終了するまでインストールを延期することができます。'
                                                Repair = '延期期間が切れるまで修理を延期することもできます。'
                                                Uninstall = '延期期間が切れるまでアンインストールを延期する選択肢もあります。'
                                            }
                                            DeferralsRemaining = '繰り延べ残高：'
                                            DeferralDeadline = '期限:'
                                            ExpiryWarning = '猶予期間が終了すると、猶予のオプションはなくなります。'
                                            CountdownDefer = @{
                                                Install = 'インストールは自動的に続行されます。'
                                                Repair = '修復は自動的に続行されます。'
                                                Uninstall = 'アンインストールは自動的に続行されます。'
                                            }
                                            CountdownClose = @{
                                                Install = '注意: プログラムは、次の時間で自動的に閉じられます:'
                                                Repair = '注意: プログラムは、次の時間で自動的に閉じられます:'
                                                Uninstall = '注意: プログラムは、次の時間で自動的に閉じられます:'
                                            }
                                            ButtonClose = '閉じる &Programs'
                                            ButtonDefer = '&延期'
                                            ButtonContinue = '&続行'
                                            ButtonContinueTooltip = '上記にリストされたアプリケーションをすべて閉じた後にのみ、「続行」を選択してください。'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = '次のアプリケーションが自動的に閉じられるので、作業を保存してから続行してください。'
                                                Repair = '次のアプリケーションが自動的に閉じられるので、作業を保存してから続行してください。'
                                                Uninstall = '次のアプリケーションが自動的に閉じられるので、作業を保存してから続行してください。'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'インストールを選択してインストールを続行してください。'
                                                Repair = '修復を続行するには、[修復] を選択してください。'
                                                Uninstall = 'アンインストールを続行するには、[アンインストール] を選択してください。'
                                            }
                                            AutomaticStartCountdown = '自動スタートカウントダウン'
                                            DeferralsRemaining = '残りの延期'
                                            DeferralDeadline = '延期期限'
                                            ButtonLeftText = @{
                                                Install = 'アプリを終了してインストールします。'
                                                Repair = 'アプリを終了して修理'
                                                Uninstall = 'アプリを終了してアンインストールします。'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'インストールする'
                                                Repair = '修理'
                                                Uninstall = 'アンインストール'
                                            }
                                            ButtonRightText = '延期'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - アプリのインストール'
                                                Repair = '{Toolkit\CompanyName} - アプリケーションの修復'
                                                Uninstall = '{Toolkit\CompanyName} - アプリケーションのアンインストール'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'ko' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = '설치가 시작되었습니다.'
                                            Repair = '복구 시작.'
                                            Uninstall = '제거가 시작되었습니다.'
                                        }
                                        Complete = @{
                                            Install = '설치 완료.'
                                            Repair = '복구 완료.'
                                            Uninstall = '제거 완료.'
                                        }
                                        RestartRequired = @{
                                            Install = '설치 완료. 재부팅이 필요합니다.'
                                            Repair = '복구 완료. 재부팅이 필요합니다.'
                                            Uninstall = '제거 완료. 재부팅이 필요합니다.'
                                        }
                                        FastRetry = @{
                                            Install = '설치가 완료되지 않았습니다.'
                                            Repair = '복구가 완료되지 않았습니다.'
                                            Uninstall = '제거가 완료되지 않았습니다.'
                                        }
                                        Error = @{
                                            Install = '설치에 실패했습니다.'
                                            Repair = '복구에 실패했습니다.'
                                            Uninstall = '제거에 실패했습니다.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = '설치 작업을 완료할 수 있도록 이 애플리케이션 실행이 일시적으로 차단되었습니다.'
                                            Repair = '복구 작업을 완료하기 위해 이 애플리케이션 실행이 일시적으로 차단되었습니다.'
                                            Uninstall = '제거 작업을 완료하기 위해 이 애플리케이션 실행이 일시적으로 차단되었습니다.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 앱 설치'
                                            Repair = '{Toolkit\CompanyName} - 앱 복구'
                                            Uninstall = '{Toolkit\CompanyName} - 앱 제거'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "설치를 완료할 디스크 공간이 부족합니다:`n{0}`n`n공간 필요: {1}MB`n공간 사용 가능: {2}MB`n 설치를 계속하려면 디스크 공간을 충분히 확보하십시오."
                                            Repair = "디스크 공간이 부족하여:`n{0}`n`n복구를 완료하려면 공간이 필요합니다: {1}MB`n사용 가능한 공간: {2}MB`n수리를 계속하려면 디스크 공간을 충분히 확보하십시오."
                                            Uninstall = "디스크 공간이 부족하여:`n{0}`n`n제거를 완료할 공간이 없습니다: {1}MB`n사용 가능한 공간: {2}MB`n제거를 계속하려면 디스크 공간을 충분히 확보하세요."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 앱 설치'
                                            Repair = '{Toolkit\CompanyName} - 앱 복구'
                                            Uninstall = '{Toolkit\CompanyName} - 앱 제거'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = '항목을 선택하세요:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = '설치 중입니다. 잠시만 기다려주세요…'
                                            Repair = '수리 중입니다. 잠시만 기다려주세요…'
                                            Uninstall = '제거 중입니다. 잠시만 기다려주세요…'
                                        }
                                        MessageDetail = @{
                                            Install = '설치가 완료되면 이 창이 자동으로 닫힙니다.'
                                            Repair = '복구가 완료되면 이 창이 자동으로 닫힙니다.'
                                            Uninstall = '제거가 완료되면 이 창이 자동으로 닫힙니다.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 앱 설치'
                                            Repair = '{Toolkit\CompanyName} - 앱 복구'
                                            Uninstall = '{Toolkit\CompanyName} - 앱 제거'
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
                                        CustomMessage = $null
                                        MessageRestart = '카운트다운이 끝나면 컴퓨터가 자동으로 다시 시작됩니다.'
                                        MessageTime = '작업을 저장하고 할당된 시간 내에 다시 시작하세요.'
                                        TimeRemaining = '남은 시간:'
                                        Title = '재시작 필요'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 앱 설치'
                                            Repair = '{Toolkit\CompanyName} - 앱 복구'
                                            Uninstall = '{Toolkit\CompanyName} - 앱 제거'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = '다음 애플리케이션을 설치하려고 합니다.'
                                                Repair = '다음 애플리케이션을 복구하려고 합니다:'
                                                Uninstall = '다음 애플리케이션을 제거하려고 합니다:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "다음 프로그램을 닫아야 설치를 계속할 수 있습니다.`n`n작업을 저장하고 프로그램을 닫은 다음 계속하세요. 또는 작업을 저장하고 ``프로그램 닫기``를 클릭하세요."
                                                Repair = "복구를 진행하려면 다음 프로그램을 닫아야 합니다.`n`n작업을 저장하고 프로그램을 닫은 다음 계속하십시오. 또는 작업을 저장하고 ``프로그램 닫기``를 클릭하세요."
                                                Uninstall = "제거를 진행하려면 다음 프로그램을 닫아야 합니다.`n`n작업을 저장하고 프로그램을 닫은 후 계속하세요. 또는 작업을 저장하고 ``프로그램 닫기``를 클릭하세요."
                                            }
                                            ExpiryMessage = @{
                                                Install = '연기가 만료될 때까지 설치를 연기하도록 선택할 수 있습니다:'
                                                Repair = '연기가 만료될 때까지 수리를 연기하도록 선택할 수 있습니다:'
                                                Uninstall = '유예가 만료될 때까지 제거를 연기하도록 선택할 수 있습니다:'
                                            }
                                            DeferralsRemaining = '남은 연기:'
                                            DeferralDeadline = '마감일:'
                                            ExpiryWarning = '연기가 만료되면 더 이상 연기할 수 있는 옵션이 없습니다.'
                                            CountdownDefer = @{
                                                Install = '설치가 자동으로 다음 위치에서 계속됩니다:'
                                                Repair = '복구는 자동으로 다음에서 계속됩니다:'
                                                Uninstall = '제거는 자동으로 다음 위치에서 계속됩니다:'
                                            }
                                            CountdownClose = @{
                                                Install = '참고: 프로그램이 자동으로 닫히는 위치:'
                                                Repair = '참고: 프로그램이 자동으로 닫히는 위치:'
                                                Uninstall = '참고: 프로그램이 자동으로 닫히는 위치:'
                                            }
                                            ButtonClose = '&프로그램 닫기'
                                            ButtonDefer = '&연기하다'
                                            ButtonContinue = '&계속하기'
                                            ButtonContinueTooltip = "위에 나열된 애플리케이션을 닫은 후에만 '계속'을 선택하세요."
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = '다음 애플리케이션이 자동으로 닫히므로 계속하기 전에 작업을 저장하세요.'
                                                Repair = '다음 애플리케이션이 자동으로 닫히므로 계속하기 전에 작업을 저장하세요.'
                                                Uninstall = '다음 애플리케이션이 자동으로 닫히므로 계속하기 전에 작업을 저장하세요.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = '설치를 계속하려면 설치를 선택하세요.'
                                                Repair = '수리를 계속하려면 수리를 선택하세요.'
                                                Uninstall = '제거를 계속하려면 제거를 선택하세요.'
                                            }
                                            AutomaticStartCountdown = '자동 시작 카운트다운'
                                            DeferralsRemaining = '남은 연기금'
                                            DeferralDeadline = '연기 기한'
                                            ButtonLeftText = @{
                                                Install = '앱 닫기 및 설치'
                                                Repair = '앱 닫기 & 복구'
                                                Uninstall = '앱 닫기 및 제거'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = '설치'
                                                Repair = '복구'
                                                Uninstall = '제거'
                                            }
                                            ButtonRightText = '연기'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - 앱 설치'
                                                Repair = '{Toolkit\CompanyName} - 앱 복구'
                                                Uninstall = '{Toolkit\CompanyName} - 앱 제거'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'lv' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Uzstādīšana uzsākta.'
                                            Repair = 'Uzsākta labošana.'
                                            Uninstall = 'Uzsākta atinstalēšana.'
                                        }
                                        Complete = @{
                                            Install = 'Uzstādīšana pabeigta.'
                                            Repair = 'Labošana pabeigta.'
                                            Uninstall = 'Atinstalēšana pabeigta.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Uzstādīšana pabeigta. Nepieciešams restartēt datoru.'
                                            Repair = 'Labošana pabeigta. Nepieciešams restartēt datoru.'
                                            Uninstall = 'Atinstalēšana pabeigta. Nepieciešams restartēt datoru.'
                                        }
                                        FastRetry = @{
                                            Install = 'Instalēšana nav pabeigta.'
                                            Repair = 'Labošana nav pabeigta.'
                                            Uninstall = 'Atinstalēšana nav pabeigta.'
                                        }
                                        Error = @{
                                            Install = 'Instalēšana neizdevās.'
                                            Repair = 'Labošana neizdevās.'
                                            Uninstall = 'Atinstalēšana neizdevās.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Šīs lietojumprogrammas palaišana ir uz laiku bloķēta, lai varētu pabeigt instalēšanas operāciju.'
                                            Repair = 'Šīs lietojumprogrammas palaišana ir uz laiku bloķēta, lai varētu pabeigt labošanas operāciju.'
                                            Uninstall = 'Šīs lietojumprogrammas palaišana ir uz laiku bloķēta, lai varētu pabeigt atinstalēšanas operāciju.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Lietotņu Instalēšana'
                                            Repair = '{Toolkit\CompanyName} - Lietotņu Labošana'
                                            Uninstall = '{Toolkit\CompanyName} - Lietotņu Atinstalēšana'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Jums nav pietiekami daudz vietas uz diska, lai pabeigtu instalēšanu:`n{0}`n`nNepieciešamā vieta: {1}MB`nPieejamā vieta: {2}MB`n`nLūdzu, atbrīvojiet pietiekami daudz vietas uz diska, lai varētu turpināt instalēšanu."
                                            Repair = "Jums nav pietiekami daudz vietas uz diska, lai pabeigtu labošanu:`n{0}`n`nNepieciešamā vieta: {1}MB`nPieejamā vieta: {2}MB`n`nLūdzu, atbrīvojiet pietiekami daudz vietas uz diska, lai varētu turpināt labošanu."
                                            Uninstall = "Jums nav pietiekami daudz vietas uz diska, lai pabeigtu atinstalēšanu no:`n{0}`n`nNepieciešamā vieta: {1}MB`nPieejamā vieta: {2}MB`n`nLūdzu, atbrīvojiet pietiekami daudz vietas diskā, lai varētu turpināt atinstalēšanu."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Lietotņu Instalēšana'
                                            Repair = '{Toolkit\CompanyName} - Lietotņu Labošana'
                                            Uninstall = '{Toolkit\CompanyName} - Lietotņu Atinstalēšana'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Izvēlieties vienumu:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Notiek instalēšana. Lūdzu, uzgaidiet…'
                                            Repair = 'Notiek labošana. Lūdzu, uzgaidiet…'
                                            Uninstall = 'Notiek atinstalēšana. Lūdzu, uzgaidiet…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Šis logs aizvērsies automātiski, kad instalēšana būs pabeigta.'
                                            Repair = 'Šis logs aizvērsies automātiski, kad labošana būs pabeigta.'
                                            Uninstall = 'Šis logs aizvērsies automātiski, kad būs pabeigta atinstalēšana.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Lietotņu Instalēšana'
                                            Repair = '{Toolkit\CompanyName} - Lietotņu Labošana'
                                            Uninstall = '{Toolkit\CompanyName} - Lietotņu Atinstalēšana'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimizēt'
                                        ButtonRestartNow = 'Restartēt tagad'
                                        Message = @{
                                            Install = 'Lai instalēšana tiktu pabeigta, dators ir jārestartē.'
                                            Repair = 'Lai labošana tiktu pabeigta, dators jārestartē.'
                                            Uninstall = 'Lai pabeigtu atinstalēšanu, dators jārestartē.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Jūsu dators tiks automātiski restartēts pēc laika atksaites beigām.'
                                        MessageTime = 'Lūdzu, saglabājiet savu darbu un restartējiet datoru atļautajā laikā.'
                                        TimeRemaining = 'Atlikušais laiks:'
                                        Title = 'Nepieciešams restartēt datoru'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Lietotņu Instalēšana'
                                            Repair = '{Toolkit\CompanyName} - Lietotņu Labošana'
                                            Uninstall = '{Toolkit\CompanyName} - Lietotņu Atinstalēšana'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Tiks instalēta šāda lietojumprogramma:'
                                                Repair = 'Tiks remontēta šāda lietojumprogramma:'
                                                Uninstall = 'Tiks atinstalēta šāda programma:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Pirms instalēšanas turpināšanas ir jāaizver šādas programmas.`n`nLūdzu, saglabājiet savu darbu, aizveriet programmas un pēc tam turpiniet. Vai arī saglabājiet darbu un noklikšķiniet uz `“Aizvērt programmas`”."
                                                Repair = "Pirms var turpināt labošanu, ir jāaizver šādas programmas.`n`nLūdzu, saglabājiet savu darbu, aizveriet programmas un pēc tam turpiniet. Vai arī saglabājiet darbu un noklikšķiniet uz `“Aizvērt programmas`”."
                                                Uninstall = "Pirms var turpināt atinstalēšanu, ir jāaizver šādas programmas.`n`nLūdzu, saglabājiet savu darbu, aizveriet programmas un pēc tam turpiniet. Vai arī saglabājiet darbu un noklikšķiniet uz `“Aizvērt programmas`”."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Jūs varat izvēlēties atlikt instalēšanu līdz atlikšanas termiņa beigām:'
                                                Repair = 'Jūs varat izvēlēties atlikt labošanu līdz atlikšanas termiņa beigām:'
                                                Uninstall = 'Jūs varat izvēlēties atlikt atinstalēšanu līdz atlikšanas termiņa beigām:'
                                            }
                                            DeferralsRemaining = 'Iespējas atlikt:'
                                            DeferralDeadline = 'Termiņš:'
                                            ExpiryWarning = 'Kad atlikšanas termiņš būs beidzies, vairs nebūs iespējas atlikt.'
                                            CountdownDefer = @{
                                                Install = 'Instalēšana automātiski turpināsies:'
                                                Repair = 'Labošana automātiski turpināsies pēc:'
                                                Uninstall = 'Atinstalēšana automātiski turpināsies pēc:'
                                            }
                                            CountdownClose = @{
                                                Install = 'PIEZĪME: Programma(-as) tiks automātiski aizvērta(-as):'
                                                Repair = 'PIEZĪME: Programma(-as) tiks automātiski aizvērta(-as):'
                                                Uninstall = 'PIEZĪME: Programma(-as) tiks automātiski aizvērta(-as):'
                                            }
                                            ButtonClose = 'Aizvērt &Programmas'
                                            ButtonDefer = '&Atlikt'
                                            ButtonContinue = '&Turpināt'
                                            ButtonContinueTooltip = 'Izvēlieties “Turpināt” tikai pēc iepriekš minētās(-o) programmas(-u) slēgšanas.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Lūdzu, saglabājiet savu darbu pirms turpināšanas, jo šādas lietojumprogrammas tiks automātiski aizvērtas.'
                                                Repair = 'Lūdzu, saglabājiet savu darbu pirms turpināšanas, jo šādas lietojumprogrammas tiks automātiski aizvērtas.'
                                                Uninstall = 'Lūdzu, saglabājiet savu darbu pirms turpināšanas, jo šādas lietojumprogrammas tiks automātiski aizvērtas.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Lūdzu, izvēlieties Instalēt, lai turpinātu instalēšanu.'
                                                Repair = 'Lūdzu, izvēlieties Labot, lai turpinātu labošanu.'
                                                Uninstall = 'Lūdzu, izvēlieties Atinstalēt, lai turpinātu atinstalēšanu.'
                                            }
                                            AutomaticStartCountdown = 'Automātiska sākuma atpakaļskaitīšana'
                                            DeferralsRemaining = 'Atlikušie atlikumi'
                                            DeferralDeadline = 'Atlikšanas termiņš'
                                            ButtonLeftText = @{
                                                Install = 'Aizvērt programmas un Instalēt'
                                                Repair = 'Aizvērt programmas un Labot'
                                                Uninstall = 'Aizvērt programmas un Atinstalēt'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Instalēt'
                                                Repair = 'Labot'
                                                Uninstall = 'Atinstalēt'
                                            }
                                            ButtonRightText = 'Atlikt'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Lietotņu Instalēšana'
                                                Repair = '{Toolkit\CompanyName} - Lietotņu Labošana'
                                                Uninstall = '{Toolkit\CompanyName} - Lietotņu Atinstalēšana'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'nb' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = "Installasjon startet."
                                            Repair = "Reparasjon startet."
                                            Uninstall = "Avinstalleringen er startet."
                                        }
                                        Complete = @{
                                            Install = "Installasjon fullført."
                                            Repair = "Reparasjon fullført."
                                            Uninstall = "Avinstalleringen er fullført."
                                        }
                                        RestartRequired = @{
                                            Install = "Installasjonen er fullført. En omstart er nødvendig."
                                            Repair = "Reparasjon fullført. En omstart er påkrevd."
                                            Uninstall = "Avinstalleringen er fullført. En omstart er påkrevd."
                                        }
                                        FastRetry = @{
                                            Install = "Installasjon ikke fullført."
                                            Repair = "Reparasjon ikke fullført."
                                            Uninstall = "Avinstalleringen er ikke fullført."
                                        }
                                        Error = @{
                                            Install = "Installasjon mislyktes."
                                            Repair = "Reparasjon mislyktes."
                                            Uninstall = "Avinstalleringen mislyktes."
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Start av denne applikasjonen er midlertidig blokkert slik at en installasjonsoperasjon kan fullføres.'
                                            Repair = 'Start av denne applikasjonen er midlertidig blokkert slik at en reparasjonsoperasjon kan fullføres.'
                                            Uninstall = 'Start av dette programmet har blitt midlertidig blokkert slik at en avinstallasjonsoperasjon kan fullføres.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Appinstallasjon'
                                            Repair = '{Toolkit\CompanyName} - Appreparasjon'
                                            Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Du har ikke nok diskplass til å fullføre installasjonen av:`n{0}`n`n`Plass kreves: {1}MB`nTilgjengelig plass: {2}MB`n`nFrigjør nok diskplass for å kunne fortsette med installasjonen."
                                            Repair = "Du har ikke nok diskplass til å fullføre reparasjonen av:`n{0}`n`nPlass kreves: {1}MB`nTilgjengelig plass: {2}MB`n`nFrigjør nok diskplass for å kunne fortsette med reparasjonen."
                                            Uninstall = "Du har ikke nok diskplass til å fullføre avinstalleringen av:`n{0}`n`nPlass kreves: {1}MB`nTilgjengelig plass: {2}MB`n`nFrigjør nok diskplass for å kunne fortsette med avinstallasjonen."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Appinstallasjon'
                                            Repair = '{Toolkit\CompanyName} - Appreparasjon'
                                            Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Velg et element:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Installasjon pågår. Vennligst vent …'
                                            Repair = 'Reparasjon pågår. Vennligst vent…'
                                            Uninstall = 'Avinstalleringen pågår. Vennligst vent…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Dette vinduet lukkes automatisk når installasjonen er fullført.'
                                            Repair = 'Dette vinduet lukkes automatisk når reparasjonen er fullført.'
                                            Uninstall = 'Dette vinduet lukkes automatisk når avinstallasjonen er fullført.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Appinstallasjon'
                                            Repair = '{Toolkit\CompanyName} - Appreparasjon'
                                            Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimer'
                                        ButtonRestartNow = 'Start på nytt nå'
                                        Message = @{
                                            Install = 'For at installasjonen skal fullføres, må du starte datamaskinen på nytt.'
                                            Repair = 'For at reparasjonen skal fullføres, må du starte datamaskinen på nytt.'
                                            Uninstall = 'Du må starte datamaskinen på nytt for at avinstallasjonen skal fullføres.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Datamaskinen startes automatisk på nytt når nedtellingen er over.'
                                        MessageTime = 'Lagre arbeidet ditt og start på nytt innen den tilmålte tiden.'
                                        TimeRemaining = 'Gjenværende tid:'
                                        Title = 'Omstart påkrevd'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Appinstallasjon'
                                            Repair = '{Toolkit\CompanyName} - Appreparasjon'
                                            Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Følgende program er i ferd med å bli installert:'
                                                Repair = 'Følgende applikasjon er i ferd med å bli reparert:'
                                                Uninstall = 'Følgende applikasjon er i ferd med å bli avinstallert:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Følgende programmer må lukkes før installasjonen kan fortsette.»`n`nLagre arbeidet ditt, lukk programmene og fortsett deretter. Alternativt kan du lagre arbeidet og klikke på «Lukk programmer»."
                                                Repair = "Følgende programmer må lukkes før reparasjonen kan fortsette.n`n`nLagre arbeidet, lukk programmene, og fortsett deretter. Alternativt kan du lagre arbeidet og klikke på «Lukk programmer»."
                                                Uninstall = "Følgende programmer må lukkes før avinstallasjonen kan fortsette.n`n`nLagre arbeidet, lukk programmene, og fortsett deretter. Alternativt kan du lagre arbeidet og klikke på «Lukk programmer»."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Du kan velge å utsette installasjonen til utsettelsen utløper:'
                                                Repair = 'Du kan velge å utsette reparasjonen til utsettelsen utløper:'
                                                Uninstall = 'Du kan velge å utsette avinstallasjonen til utsettelsen utløper:'
                                            }
                                            DeferralsRemaining = 'Gjenværende utsettelser:'
                                            DeferralDeadline = 'Frist:'
                                            ExpiryWarning = 'Når utsettelsen har utløpt, har du ikke lenger muligheten til å utsette.'
                                            CountdownDefer = @{
                                                Install = 'Installasjonen fortsetter automatisk om:'
                                                Repair = 'Reparasjonen vil automatisk fortsette i:'
                                                Uninstall = 'Avinstallasjonen vil automatisk fortsette om:'
                                            }
                                            CountdownClose = @{
                                                Install = 'MERK: Programmet/programmene lukkes automatisk om:'
                                                Repair = 'MERK: Programmet/programmene lukkes automatisk om:'
                                                Uninstall = 'MERK: Programmet/programmene lukkes automatisk om:'
                                            }
                                            ButtonClose = 'Lukk &Programmer'
                                            ButtonDefer = '&Utsette'
                                            ButtonContinue = '&Fortsett'
                                            ButtonContinueTooltip = 'Velg bare «Fortsett» etter at du har lukket ovennevnte program(mer).'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Lagre arbeidet ditt før du fortsetter, da følgende programmer vil bli lukket automatisk.'
                                                Repair = 'Lagre arbeidet ditt før du fortsetter, da følgende programmer vil bli lukket automatisk.'
                                                Uninstall = 'Lagre arbeidet ditt før du fortsetter, da følgende programmer vil bli lukket automatisk.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Velg Install for å fortsette med installasjonen.'
                                                Repair = 'Velg Repair for å fortsette med reparasjonen.'
                                                Uninstall = 'Velg Avinstaller for å fortsette med avinstallasjonen.'
                                            }
                                            AutomaticStartCountdown = 'Automatisk nedtelling til start'
                                            DeferralsRemaining = 'Gjenværende utsettelser'
                                            DeferralDeadline = 'Frist for utsettelse'
                                            ButtonLeftText = @{
                                                Install = 'Lukk apper og installer'
                                                Repair = 'Lukk apper og reparer'
                                                Uninstall = 'Lukk apper og avinstaller'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Installere'
                                                Repair = 'Reparer'
                                                Uninstall = 'Avinstaller'
                                            }
                                            ButtonRightText = 'Utsett'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Appinstallasjon'
                                                Repair = '{Toolkit\CompanyName} - Appreparasjon'
                                                Uninstall = '{Toolkit\CompanyName} - Avinstallasjon av app'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'nl' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Installatie gestart.'
                                            Repair = 'Reparatie gestart.'
                                            Uninstall = 'De-installatie gestart.'
                                        }
                                        Complete = @{
                                            Install = 'Installatie voltooid.'
                                            Repair = 'Reparatie voltooid.'
                                            Uninstall = 'De-installatie voltooid.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Installatie voltooid. Opnieuw opstarten is vereist.'
                                            Repair = 'Reparatie voltooid. Opnieuw opstarten is vereist.'
                                            Uninstall = 'De-installatie voltooid. Opnieuw opstarten is vereist.'
                                        }
                                        FastRetry = @{
                                            Install = 'Installatie niet voltooid.'
                                            Repair = 'Reparatie niet voltooid.'
                                            Uninstall = 'De-installatie niet voltooid.'
                                        }
                                        Error = @{
                                            Install = 'Installatie mislukt.'
                                            Repair = 'Reparatie mislukt.'
                                            Uninstall = 'De-installatie mislukt.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = "Het starten van deze applicatie is tijdelijk geblokkeerd zodat een installatie kan worden uitgevoerd."
                                            Repair = "Het starten van deze applicatie is tijdelijk geblokkeerd zodat een reparatie kan worden uitgevoerd."
                                            Uninstall = "Het starten van deze applicatie is tijdelijk geblokkeerd zodat een de-installatie kan worden uitgevoerd."
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - App Installatie'
                                            Repair = '{Toolkit\CompanyName} - App Reparatie'
                                            Uninstall = '{Toolkit\CompanyName} - App De-installatie'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "U hebt niet genoeg schijfruimte om de installatie te voltooien van:`n{0}`n`nAfgeronde ruimte: {1}MB`nRuimte beschikbaar: {2}MB`n`nMaak voldoende schijfruimte vrij om door te gaan met de installatie."
                                            Repair = "U hebt niet genoeg schijfruimte om de reparatie te voltooien van:`n{0}`n`nVerplichte ruimte: {1}MB`nBeschikbare ruimte: {2}MB`n`nMaak voldoende schijfruimte vrij om door te gaan met de reparatie."
                                            Uninstall = "U hebt niet genoeg schijfruimte om de de-installatie te voltooien van:`n{0}`n`nAfgeronde ruimte: {1}MB`nBeschikbare ruimte: {2}MB`n`nMaak alstublieft voldoende schijfruimte vrij om door te gaan met de de-installatie."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - App Installatie'
                                            Repair = '{Toolkit\CompanyName} - App Reparatie'
                                            Uninstall = '{Toolkit\CompanyName} - App De-installatie'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Selecteer een item:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Installatie wordt uitgevoerd. Even geduld a.u.b…'
                                            Repair = 'Reparatie wordt uitgevoerd. Even geduld a.u.b…'
                                            Uninstall = 'De-installatie wordt uitgevoerd. Even geduld a.u.b…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Dit venster wordt automatisch gesloten als de installatie voltooid is.'
                                            Repair = 'Dit venster wordt automatisch gesloten als de reparatie is voltooid.'
                                            Uninstall = 'Dit venster wordt automatisch gesloten als de de-installatie voltooid is.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - App Installatie'
                                            Repair = '{Toolkit\CompanyName} - App Reparatie'
                                            Uninstall = '{Toolkit\CompanyName} - App De-installatie'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimaliseren'
                                        ButtonRestartNow = 'Nu opnieuw opstarten'
                                        Message = @{
                                            Install = 'Om de installatie te voltooien, moet u uw computer opnieuw opstarten.'
                                            Repair = 'Om de reparatie te voltooien, moet u uw computer opnieuw opstarten.'
                                            Uninstall = 'Om de de-installatie te voltooien, moet u uw computer opnieuw opstarten.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Uw computer wordt automatisch opnieuw opgestart aan het einde van het aftellen.'
                                        MessageTime = 'Sla uw werk op en start uw computer binnen de toegewezen tijd opnieuw op.'
                                        TimeRemaining = 'Resterende tijd:'
                                        Title = 'Opnieuw opstarten vereist'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - App Installatie'
                                            Repair = '{Toolkit\CompanyName} - App Reparatie'
                                            Uninstall = '{Toolkit\CompanyName} - App De-installatie'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'De volgende toepassing wordt geïnstalleerd:'
                                                Repair = 'De volgende toepassing wordt gerepareerd:'
                                                Uninstall = 'De volgende toepassing wordt gede-installeerd:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "De volgende applicaties moeten worden afgesloten voordat de installatie kan worden uitgevoerd.`n`nSla uw werk op, sluit de applicaties af en ga dan verder. U kunt ook uw werk opslaan en op `"Applicaties sluiten`" klikken."
                                                Repair = "De volgende applicaties moeten worden afgesloten voordat de reparatie kan worden uitgevoerd.`n`nSla uw werk op, sluit de applicaties af en ga dan verder. U kunt ook uw werk opslaan en op `"Applicaties sluiten`" klikken."
                                                Uninstall = "De volgende applicaties moeten worden afgesloten voordat de de-installatie kan worden uitgevoerd.`n`nSla uw werk op, sluit de applicaties af en ga dan verder. U kunt ook uw werk opslaan en op `"Applicaties sluiten`" klikken."
                                            }
                                            ExpiryMessage = @{
                                                Install = "U kunt ervoor kiezen de installatie uit te stellen totdat het uitstel verloopt:"
                                                Repair = "U kunt ervoor kiezen de reparatie uit te stellen totdat het uitstel verloopt:"
                                                Uninstall = "U kunt ervoor kiezen de de-installatie uit te stellen totdat het uitstel verloopt:"
                                            }
                                            DeferralsRemaining = 'Uitstel keren beschikbaar:'
                                            DeferralDeadline = 'Termijn:'
                                            ExpiryWarning = 'Als het uitstel is verlopen, hebt u niet langer de mogelijkheid om uit te stellen.'
                                            CountdownDefer = @{
                                                Install = 'De installatie gaat automatisch verder na:'
                                                Repair = 'De reparatie gaat automatisch verder na:'
                                                Uninstall = 'De de-installatie wordt automatisch voortgezet na:'
                                            }
                                            CountdownClose = @{
                                                Install = 'OPMERKING: De applicaties worden automatisch afgesloten na:'
                                                Repair = 'OPMERKING: De applicaties worden automatisch afgesloten na:'
                                                Uninstall = 'OPMERKING: De applicaties worden automatisch afgesloten na:'
                                            }
                                            ButtonClose = "Sluit &Applicaties"
                                            ButtonDefer = '&Verwijderen'
                                            ButtonContinue = '&Doorgaan'
                                            ButtonContinueTooltip = "Selecteer alleen ‘Doorgaan’ na het sluiten van de bovengenoemde applicatie(s)."
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Sla uw werk op voordat u verder gaat, omdat de volgende applicaties automatisch worden gesloten. Als u nog uitstel heeft, kunt u de installatie ook uitstellen.'
                                                Repair = 'Sla uw werk op voordat u verder gaat, omdat de volgende applicaties automatisch worden gesloten. Als u nog uitstel heeft, kunt u de reparatie ook uitstellen.'
                                                Uninstall = 'Sla uw werk op voordat u verder gaat, omdat de volgende applicaties automatisch worden gesloten. Als u nog uitstel heeft, kunt u de de-installatie ook uitstellen.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Selecteer Installeren om door te gaan met de installatie.'
                                                Repair = 'Selecteer Repareren om door te gaan met de reparatie.'
                                                Uninstall = 'Selecteer De-installeren om door te gaan met de de-installatie.'
                                            }
                                            AutomaticStartCountdown = 'Automatische start aftellen'
                                            DeferralsRemaining = 'Resterende uitstel'
                                            DeferralDeadline = 'Uitsteltermijn'
                                            ButtonLeftText = @{
                                                Install = 'Sluit Apps en Installeer'
                                                Repair = 'Sluit Apps en Repareer'
                                                Uninstall = 'Sluit Apps en De-installeer'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Installeren'
                                                Repair = 'Repareren'
                                                Uninstall = 'De-installeren'
                                            }
                                            ButtonRightText = 'Uitstellen'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - App Installatie'
                                                Repair = '{Toolkit\CompanyName} - App Reparatie'
                                                Uninstall = '{Toolkit\CompanyName} - App De-installatie'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'pl' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Rozpoczęto instalację.'
                                            Repair = 'Rozpoczęto naprawę.'
                                            Uninstall = 'Rozpoczęto dezinstalację.'
                                        }
                                        Complete = @{
                                            Install = 'Instalacja zakończona.'
                                            Repair = 'Naprawa zakończona.'
                                            Uninstall = 'Dezinstalacja zakończona.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Instalacja zakończona. Wymagany jest restart.'
                                            Repair = 'Naprawa zakończona. Wymagany jest restart.'
                                            Uninstall = 'Dezinstalacja zakończona. Wymagane jest ponowne uruchomienie komputera.'
                                        }
                                        FastRetry = @{
                                            Install = 'Instalacja nie została ukończona.'
                                            Repair = 'Naprawa nie została zakończona.'
                                            Uninstall = 'Dezinstalacja nie została ukończona.'
                                        }
                                        Error = @{
                                            Install = 'Instalacja nie powiodła się.'
                                            Repair = 'Naprawa nie powiodła się.'
                                            Uninstall = 'Dezinstalacja nie powiodła się.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Uruchamianie tej aplikacji zostało tymczasowo zablokowane, aby można było ukończyć operację instalacji.'
                                            Repair = 'Uruchomienie tej aplikacji zostało tymczasowo zablokowane, aby umożliwić zakończenie operacji naprawy.'
                                            Uninstall = 'Uruchamianie tej aplikacji zostało tymczasowo zablokowane, aby można było zakończyć operację dezinstalacji.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalacja Aplikacji'
                                            Repair = '{Toolkit\CompanyName} - Naprawa Aplikacji'
                                            Uninstall = '{Toolkit\CompanyName} - Dezinstalacja Aplikacji'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Nie mają Państwo wystarczającej ilości miejsca na dysku, aby ukończyć instalację:`n{0}`n`nWymagane miejsce: {1}MB`nDostępne miejsce: {2}MB`n`nProszę zwolnić wystarczającą ilość miejsca na dysku, aby kontynuować instalację."
                                            Repair = "Nie ma wystarczającej ilości miejsca na dysku, aby dokończyć naprawę:`n{0}`n`nWymagane miejsce: {1}MB`nDostępne miejsce: {2}MB`n`nProszę zwolnić wystarczającą ilość miejsca na dysku, aby kontynuować naprawę."
                                            Uninstall = "Nie ma wystarczającej ilości miejsca na dysku, aby ukończyć dezinstalację:`n{0}`n`nWymagane miejsce: {1}MB`nDostępne miejsce: {2}MB`n`nProszę zwolnić wystarczającą ilość miejsca na dysku, aby kontynuować dezinstalację."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalacja Aplikacji'
                                            Repair = '{Toolkit\CompanyName} - Naprawa Aplikacji'
                                            Uninstall = '{Toolkit\CompanyName} - Dezinstalacja Aplikacji'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Wybierz element:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Instalacja w toku. Proszę czekać…'
                                            Repair = 'Trwa naprawa. Proszę czekać…'
                                            Uninstall = 'Trwa dezinstalacja. Proszę czekać…'
                                        }
                                        MessageDetail = @{
                                            Install = 'To okno zamknie się automatycznie po zakończeniu instalacji.'
                                            Repair = 'To okno zostanie zamknięte automatycznie po zakończeniu naprawy.'
                                            Uninstall = 'To okno zostanie zamknięte automatycznie po zakończeniu dezinstalacji.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalacja Aplikacji'
                                            Repair = '{Toolkit\CompanyName} - Naprawa Aplikacji'
                                            Uninstall = '{Toolkit\CompanyName} - Dezinstalacja Aplikacji'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimalizuj'
                                        ButtonRestartNow = 'Uruchom ponownie teraz'
                                        Message = @{
                                            Install = 'Aby zakończyć instalację, należy ponownie uruchomić komputer.'
                                            Repair = 'Aby zakończyć naprawę, należy ponownie uruchomić komputer.'
                                            Uninstall = 'Aby zakończyć dezinstalację, należy ponownie uruchomić komputer.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Państwa komputer zostanie automatycznie uruchomiony ponownie po zakończeniu odliczania.'
                                        MessageTime = 'Proszę zapisać swoją pracę i ponownie uruchomić komputer w wyznaczonym czasie.'
                                        TimeRemaining = 'Pozostały czas:'
                                        Title = 'Proszę uruchomić ponownie'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Instalacja Aplikacji'
                                            Repair = '{Toolkit\CompanyName} - Naprawa Aplikacji'
                                            Uninstall = '{Toolkit\CompanyName} - Dezinstalacja Aplikacji'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Następująca aplikacja zostanie wkrótce zainstalowana:'
                                                Repair = 'Następująca aplikacja ma zostać naprawiona:'
                                                Uninstall = 'Następująca aplikacja ma zostać odinstalowana:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Następujące programy muszą zostać zamknięte przed kontynuowaniem instalacji.`n`nProszę zapisać swoją pracę, zamknąć programy, a następnie kontynuować. Alternatywnie, proszę zapisać pracę i kliknąć `„Zamknij programy`”."
                                                Repair = "Następujące programy muszą zostać zamknięte przed kontynuowaniem naprawy.`n`nProszę zapisać swoją pracę, zamknąć programy, a następnie kontynuować. Alternatywnie, proszę zapisać swoją pracę i kliknąć `„Zamknij programy`”."
                                                Uninstall = "Następujące programy muszą zostać zamknięte przed przystąpieniem do dezinstalacji.`n`nProszę zapisać pracę, zamknąć programy, a następnie kontynuować. Alternatywnie, proszę zapisać pracę i kliknąć `„Zamknij programy`”."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Mogą Państwo wybrać odroczenie instalacji do czasu wygaśnięcia odroczenia:'
                                                Repair = 'Mogą Państwo wybrać opcję odroczenia naprawy do momentu wygaśnięcia odroczenia:'
                                                Uninstall = 'Mogą Państwo wybrać odroczenie deinstalacji do czasu wygaśnięcia odroczenia:'
                                            }
                                            DeferralsRemaining = 'Pozostałe odroczenia:'
                                            DeferralDeadline = 'Termin:'
                                            ExpiryWarning = 'Po wygaśnięciu odroczenia nie będzie już możliwości odroczenia.'
                                            CountdownDefer = @{
                                                Install = 'Instalacja będzie automatycznie kontynuowana w:'
                                                Repair = 'Naprawa będzie automatycznie kontynuowana w:'
                                                Uninstall = 'Deinstalacja będzie automatycznie kontynuowana w:'
                                            }
                                            CountdownClose = @{
                                                Install = 'UWAGA: Program(y) zostanie(ą) automatycznie zamknięty(e) w:'
                                                Repair = 'UWAGA: Program(y) zostanie(ą) automatycznie zamknięty(e) w:'
                                                Uninstall = 'UWAGA: Program(y) zostanie(ą) automatycznie zamknięty(e) w:'
                                            }
                                            ButtonClose = 'Zamknij &Programy'
                                            ButtonDefer = '&Odroczyć'
                                            ButtonContinue = '&Kontynuuj'
                                            ButtonContinueTooltip = 'Proszę wybrać »Kontynuuj« tylko po zamknięciu wyżej wymienionych aplikacji.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Proszę zapisać pracę przed kontynuowaniem, ponieważ następujące aplikacje zostaną automatycznie zamknięte.'
                                                Repair = 'Proszę zapisać pracę przed kontynuowaniem, ponieważ następujące aplikacje zostaną automatycznie zamknięte.'
                                                Uninstall = 'Proszę zapisać pracę przed kontynuowaniem, ponieważ następujące aplikacje zostaną automatycznie zamknięte.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Proszę wybrać Install, aby kontynuować instalację.'
                                                Repair = 'Proszę wybrać Repair, aby kontynuować naprawę.'
                                                Uninstall = 'Proszę wybrać Uninstall, aby kontynuować dezinstalację.'
                                            }
                                            AutomaticStartCountdown = 'Automatyczne odliczanie do rozpoczęcia'
                                            DeferralsRemaining = 'Pozostałe odroczenia'
                                            DeferralDeadline = 'Termin odroczenia'
                                            ButtonLeftText = @{
                                                Install = 'Zamknij aplikacje i zainstaluj'
                                                Repair = 'Zamknij aplikacje i napraw'
                                                Uninstall = 'Zamknij aplikacje i odinstaluj'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Zainstaluj'
                                                Repair = 'Napraw'
                                                Uninstall = 'Odinstaluj'
                                            }
                                            ButtonRightText = 'Odroczyć'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Instalacja Aplikacji'
                                                Repair = '{Toolkit\CompanyName} - Naprawa Aplikacji'
                                                Uninstall = '{Toolkit\CompanyName} - Dezinstalacja Aplikacji'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'pt' = {
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
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Selecione um item:'
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
                                        CustomMessage = $null
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
                                            CountdownClose = @{
                                                Install = 'NOTA: O(s) programa(s) será(ão) automaticamente encerrado(s) em:'
                                                Repair = 'NOTA: O(s) programa(s) será(ão) automaticamente encerrado(s) em:'
                                                Uninstall = 'NOTA: O(s) programa(s) será(ão) automaticamente encerrado(s) em:'
                                            }
                                            ButtonClose = 'Fechar &Programas'
                                            ButtonDefer = '&Deferir'
                                            ButtonContinue = '&Continuar'
                                            ButtonContinueTooltip = 'Selecione “Continuar” apenas depois de fechar a(s) aplicação(ões) acima indicada(s).'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Por favor, guarde o seu trabalho antes de continuar, pois as seguintes aplicações serão fechadas automaticamente.'
                                                Repair = 'Por favor, guarde o seu trabalho antes de continuar, pois as seguintes aplicações serão fechadas automaticamente.'
                                                Uninstall = 'Por favor, guarde o seu trabalho antes de continuar, pois as seguintes aplicações serão fechadas automaticamente.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Selecione Install para continuar com a instalação.'
                                                Repair = 'Selecione Reparar para continuar com a reparação.'
                                                Uninstall = 'Selecione Desinstalar para continuar com a desinstalação.'
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
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'pt-BR' = {
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
                                            Repair = "Não há espaço em disco suficiente para concluir o reparo de:`n{0}`n`nEspaço necessário: {1}MB`nEspaço disponível: {2}MB`n`nPor favor, libere espaço suficiente em disco para prosseguir com a reparação."
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
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Selecione um item:'
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
                                        CustomMessage = $null
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
                                                Install = 'Selecione Instalar para continuar com a instalação.'
                                                Repair = 'Selecione Reparar para continuar com o reparo.'
                                                Uninstall = 'Selecione Desinstalar para continuar com a desinstalação.'
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
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'ru' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Установка начата.'
                                            Repair = 'Восстановление начато.'
                                            Uninstall = 'Началась деинсталляция.'
                                        }
                                        Complete = @{
                                            Install = 'Установка завершена.'
                                            Repair = 'Восстановление завершено.'
                                            Uninstall = 'Деинсталляция завершена.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Установка завершена. Требуется перезагрузка.'
                                            Repair = 'Восстановление завершено. Требуется перезагрузка.'
                                            Uninstall = 'Деинсталляция завершена. Требуется перезагрузка.'
                                        }
                                        FastRetry = @{
                                            Install = 'Установка не завершена.'
                                            Repair = 'Восстановление не завершено.'
                                            Uninstall = 'Деинсталляция не завершена.'
                                        }
                                        Error = @{
                                            Install = 'Установка не удалась.'
                                            Repair = 'Восстановление не удалось.'
                                            Uninstall = 'Не удалось выполнить деинсталляцию.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Запуск этого приложения был временно заблокирован, чтобы операция установки могла завершиться.'
                                            Repair = 'Запуск этого приложения был временно заблокирован, чтобы можно было завершить операцию восстановления.'
                                            Uninstall = 'Запуск этого приложения был временно заблокирован, чтобы операция деинсталляции могла завершиться.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Установка приложений'
                                            Repair = '{Toolkit\CompanyName} - Восстановление приложения'
                                            Uninstall = '{Toolkit\CompanyName} - Деинсталляция приложений'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "У Вас недостаточно места на диске для завершения установки:`n{0}`n`n Требуется место: {1}MB`n`nСвободное место: {2}MB`n`nПожалуйста, освободите достаточно места на диске, чтобы продолжить установку."
                                            Repair = "У Вас недостаточно места на диске для завершения восстановления:`n{0}`n`n Требуется место: {1}MB`nСвободное место: {2}MB`n`nПожалуйста, освободите достаточно места на диске, чтобы продолжить восстановление."
                                            Uninstall = "У Вас недостаточно места на диске для завершения деинсталляции:`n{0}`n`nSpace required: {1}MB`nСвободное место: {2}MB`n`nПожалуйста, освободите достаточно места на диске, чтобы продолжить деинсталляцию."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Установка приложений'
                                            Repair = '{Toolkit\CompanyName} - Восстановление приложений'
                                            Uninstall = '{Toolkit\CompanyName} - Деинсталляция приложений'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Выберите элемент:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Идет установка. Пожалуйста, подождите…'
                                            Repair = 'Выполняется восстановление. Пожалуйста, подождите…'
                                            Uninstall = 'Выполняется деинсталляция. Пожалуйста, подождите…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Это окно автоматически закроется, когда установка будет завершена.'
                                            Repair = 'Это окно закроется автоматически, когда ремонт будет завершен.'
                                            Uninstall = 'Это окно закроется автоматически после завершения деинсталляции.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - установка приложений'
                                            Repair = '{Toolkit\CompanyName} - Ремонт приложений'
                                            Uninstall = '{Toolkit\CompanyName} - Деинсталляция приложений'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Минимизировать'
                                        ButtonRestartNow = 'Перезапустить сейчас'
                                        Message = @{
                                            Install = 'Чтобы установка была завершена, Вы должны перезагрузить компьютер.'
                                            Repair = 'Для завершения восстановления Вам необходимо перезагрузить компьютер.'
                                            Uninstall = 'Для завершения деинсталляции Вам необходимо перезагрузить компьютер.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Ваш компьютер будет автоматически перезагружен по окончании обратного отсчета.'
                                        MessageTime = 'Пожалуйста, сохраните свою работу и перезагрузите компьютер в течение отведенного времени.'
                                        TimeRemaining = 'Осталось времени:'
                                        Title = 'Требуется перезагрузка'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Установка приложений'
                                            Repair = '{Toolkit\CompanyName} - Восстановление приложений'
                                            Uninstall = '{Toolkit\CompanyName} - Деинсталляция приложений'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Сейчас будет установлено следующее приложение:'
                                                Repair = 'Следующее приложение должно быть восстановлено:'
                                                Uninstall = 'Следующее приложение должно быть удалено:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Следующие программы должны быть закрыты, прежде чем установка продолжится.`n`nПожалуйста, сохраните свою работу, закройте программы, а затем продолжите. В качестве альтернативы, сохраните свою работу и нажмите «Закрыть программы»."
                                                Repair = "Следующие программы должны быть закрыты, чтобы ремонт мог быть продолжен.`n`nПожалуйста, сохраните свою работу, закройте программы и продолжите. Альтернативно, сохраните свою работу и нажмите «Закрыть программы»."
                                                Uninstall = "Следующие программы должны быть закрыты, прежде чем начнется деинсталляция.`n`nПожалуйста, сохраните свою работу, закройте программы и затем продолжите. Или же сохраните свою работу и нажмите «Закрыть программы»."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Вы можете выбрать отсрочку установки до истечения срока отсрочки:'
                                                Repair = 'Вы можете выбрать отсрочку восстановления до истечения срока отсрочки:'
                                                Uninstall = 'Вы можете отложить деинсталляцию до истечения срока отсрочки:'
                                            }
                                            DeferralsRemaining = 'Оставшиеся отсрочки:'
                                            DeferralDeadline = 'Крайний срок:'
                                            ExpiryWarning = 'По истечении срока действия отсрочки у Вас больше не будет возможности ее отложить.'
                                            CountdownDefer = @{
                                                Install = 'Установка будет автоматически продолжена через:'
                                                Repair = 'Восстановление будет автоматически продолжено через:'
                                                Uninstall = 'Деинсталляция автоматически продолжится через:'
                                            }
                                            CountdownClose = @{
                                                Install = 'ПРИМЕЧАНИЕ: Программа(ы) будут автоматически закрыты через:'
                                                Repair = 'ПРИМЕЧАНИЕ: Программа(ы) будут автоматически закрыты через:'
                                                Uninstall = 'ПРИМЕЧАНИЕ: Программа(ы) будут автоматически закрыты через:'
                                            }
                                            ButtonClose = 'Закрыть Програ'
                                            ButtonDefer = '&Отложить'
                                            ButtonContinue = '&Продолжить'
                                            ButtonContinueTooltip = 'Выбирайте «Continue» только после закрытия перечисленных выше приложений.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Пожалуйста, сохраните свою работу, прежде чем продолжить, так как следующие приложения будут закрыты автоматически.'
                                                Repair = 'Пожалуйста, сохраните свою работу, прежде чем продолжить, так как следующие приложения будут закрыты автоматически.'
                                                Uninstall = 'Пожалуйста, сохраните свою работу, прежде чем продолжить, так как следующие приложения будут закрыты автоматически.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Пожалуйста, выберите Install, чтобы продолжить установку.'
                                                Repair = 'Пожалуйста, выберите Repair, чтобы продолжить восстановление.'
                                                Uninstall = 'Пожалуйста, выберите Деинсталляция, чтобы продолжить деинсталляцию.'
                                            }
                                            AutomaticStartCountdown = 'Автоматический обратный отсчет до начала'
                                            DeferralsRemaining = 'Оставшиеся отсрочки'
                                            DeferralDeadline = 'Крайний срок отсрочки'
                                            ButtonLeftText = @{
                                                Install = 'Закрыть приложения и установить'
                                                Repair = 'Закрыть приложения и восстановить'
                                                Uninstall = 'Закройте приложения и удалите их'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Установить'
                                                Repair = 'Восстановление'
                                                Uninstall = 'Удалить'
                                            }
                                            ButtonRightText = 'Отложить'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Установка приложений'
                                                Repair = '{Toolkit\CompanyName} - Восстановление приложений'
                                                Uninstall = '{Toolkit\CompanyName} - Деинсталляция приложений'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'sk' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Inštalácia sa začala.'
                                            Repair = 'Začala sa oprava.'
                                            Uninstall = 'Začala sa odinštalácia.'
                                        }
                                        Complete = @{
                                            Install = 'Inštalácia dokončená.'
                                            Repair = 'Oprava dokončená.'
                                            Uninstall = 'Odinštalovanie dokončené.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Inštalácia dokončená. Vyžaduje sa reštart.'
                                            Repair = 'Oprava dokončená. Vyžaduje sa reštart.'
                                            Uninstall = 'Odinštalovanie dokončené. Vyžaduje sa reštart.'
                                        }
                                        FastRetry = @{
                                            Install = 'Inštalácia nebola dokončená.'
                                            Repair = 'Oprava nebola dokončená.'
                                            Uninstall = 'Odinštalovanie nebolo dokončené.'
                                        }
                                        Error = @{
                                            Install = 'Inštalácia zlyhala.'
                                            Repair = 'Oprava zlyhala.'
                                            Uninstall = 'Odinštalovanie zlyhalo.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Spustenie tejto aplikácie bolo dočasne zablokované, aby sa mohla dokončiť inštalačná operácia.'
                                            Repair = 'Spustenie tejto aplikácie bolo dočasne zablokované, aby sa mohla dokončiť operácia opravy.'
                                            Uninstall = 'Spustenie tejto aplikácie bolo dočasne zablokované, aby sa mohla dokončiť operácia odinštalovania.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Inštalácia Aplikácie'
                                            Repair = '{Toolkit\CompanyName} - Oprava Aplikácií'
                                            Uninstall = '{Toolkit\CompanyName} - Odinštalovanie Aplikácie'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Nemáte dostatok miesta na disku na dokončenie inštalácie:`n{0}`n`nPotrebné miesto: {1}MB`nDostupné miesto: {2}`n`nUvoľnite, prosím, dostatok miesta na disku, aby ste mohli pokračovať v inštalácii."
                                            Repair = "Nemáte dostatok miesta na disku na dokončenie opravy:`n{0}`n`Potrebné miesto: {1}MB`nPriestor je k dispozícii: {2}`n`nUvoľnite, prosím, dostatok miesta na disku, aby ste mohli pokračovať v oprave."
                                            Uninstall = "You do not have enough disk space to complete the uninstallation of:`n{0}`n`nSpace required: {1}MB`nPriestor je k dispozícii: {2}`n`nUvoľnite, prosím, dostatok miesta na disku, aby ste mohli pokračovať v odinštalácii."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Inštalácia Aplikácie'
                                            Repair = '{Toolkit\CompanyName} - Oprava Aplikácií'
                                            Uninstall = '{Toolkit\CompanyName} - Odinštalovanie Aplikácie'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Vyberte položku:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Inštalácia prebieha. Počkajte prosím…'
                                            Repair = 'Prebieha oprava. Počkajte prosím…'
                                            Uninstall = 'Prebieha odinštalovanie. Prosím, počkajte…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Toto okno sa po dokončení inštalácie automaticky zatvorí.'
                                            Repair = 'Toto okno sa automaticky zatvorí po dokončení opravy.'
                                            Uninstall = 'Toto okno sa automaticky zatvorí po dokončení odinštalovania.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Inštalácia Aplikácie'
                                            Repair = '{Toolkit\CompanyName} - Oprava Aplikácií'
                                            Uninstall = '{Toolkit\CompanyName} - Odinštalovanie Aplikácie'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimalizovať'
                                        ButtonRestartNow = 'Reštartovať Teraz'
                                        Message = @{
                                            Install = 'Aby sa inštalácia dokončila, musíte reštartovať počítač.'
                                            Repair = 'Aby sa oprava dokončila, musíte reštartovať počítač.'
                                            Uninstall = 'Aby sa odinštalovanie dokončilo, musíte reštartovať počítač.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Váš počítač sa automaticky reštartuje na konci odpočítavania.'
                                        MessageTime = 'Uložte si svoju prácu a reštartujte ju v stanovenom čase.'
                                        TimeRemaining = 'Zostávajúci čas:'
                                        Title = 'Vyžaduje sa Reštart'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Inštalácia Aplikácie'
                                            Repair = '{Toolkit\CompanyName} - Oprava Aplikácií'
                                            Uninstall = '{Toolkit\CompanyName} - Odinštalovanie Aplikácie'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Nasledujúca aplikácia sa práve inštaluje:'
                                                Repair = 'Nasledujúca aplikácia bude opravená:'
                                                Uninstall = 'Nasledujúca aplikácia bude odinštalovaná:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Pred pokračovaním inštalácie je potrebné zatvoriť nasledujúce programy.`n`nProsím, uložte si prácu, zatvorte programy a potom pokračujte. Prípadne uložte svoju prácu a kliknite na `„Zatvoriť programy`“."
                                                Repair = "Pred pokračovaním opravy musia byť nasledujúce programy zatvorené.`n`nProsím, uložte svoju prácu, zatvorte programy a potom pokračujte. Prípadne uložte svoju prácu a kliknite na `„Zatvoriť programy`“."
                                                Uninstall = "Predtým, ako bude možné pokračovať v odinštalovaní, musia byť nasledujúce programy zatvorené.`n`nProsím, uložte svoju prácu, zatvorte programy a potom pokračujte. Prípadne uložte svoju prácu a kliknite na `„Zavrieť programy`“."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Môžete sa rozhodnúť odložiť inštaláciu až do uplynutia odkladu:'
                                                Repair = 'Môžete sa rozhodnúť odložiť opravu až do uplynutia odkladu:'
                                                Uninstall = 'Môžete sa rozhodnúť odložiť odinštalovanie až do uplynutia odkladu:'
                                            }
                                            DeferralsRemaining = 'Zostávajúce odklady:'
                                            DeferralDeadline = 'Termín:'
                                            ExpiryWarning = 'Po uplynutí odkladu už nebudete mať možnosť odložiť.'
                                            CountdownDefer = @{
                                                Install = 'Inštalácia bude automaticky pokračovať v:'
                                                Repair = 'Oprava bude automaticky pokračovať za:'
                                                Uninstall = 'Odinštalovanie bude automaticky pokračovať za:'
                                            }
                                            CountdownClose = @{
                                                Install = 'POZNÁMKA: Program(-y) sa automaticky ukončí(-ú) v:'
                                                Repair = 'POZNÁMKA: Program(-y) sa automaticky ukončí(-ú) v:'
                                                Uninstall = 'POZNÁMKA: Program(-y) sa automaticky ukončí(-ú) v:'
                                            }
                                            ButtonClose = 'Zatvoriť &Programy'
                                            ButtonDefer = '&Odloženie'
                                            ButtonContinue = '&Pokračovať'
                                            ButtonContinueTooltip = 'Vyberte „Pokračovať“ až po zatvorení vyššie uvedených aplikácií.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Pred pokračovaním uložte svoju prácu, pretože nasledujúce aplikácie budú automaticky ukončené.'
                                                Repair = 'Pred pokračovaním uložte svoju prácu, pretože nasledujúce aplikácie budú automaticky ukončené.'
                                                Uninstall = 'Pred pokračovaním uložte svoju prácu, pretože nasledujúce aplikácie budú automaticky ukončené.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Prosím, vyberte Install (Inštalovať), aby ste mohli pokračovať v inštalácii.'
                                                Repair = 'Prosím, vyberte Repair (Opraviť), ak chcete pokračovať v oprave.'
                                                Uninstall = 'Vyberte Uninstall (Odinštalovať), ak chcete pokračovať v odinštalovaní.'
                                            }
                                            AutomaticStartCountdown = 'Automatické odpočítavanie začiatku'
                                            DeferralsRemaining = 'Zostávajúce odklady'
                                            DeferralDeadline = 'Lehota odkladu'
                                            ButtonLeftText = @{
                                                Install = 'Zavrieť aplikácie a Nainštalovať'
                                                Repair = 'Zatvoriť aplikácie a Opraviť'
                                                Uninstall = 'Zatvoriť aplikácie a Odinštalovať'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Inštalovať'
                                                Repair = 'Opraviť'
                                                Uninstall = 'Odinštalovať'
                                            }
                                            ButtonRightText = 'Odložiť'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Inštalácia Aplikácie'
                                                Repair = '{Toolkit\CompanyName} - Oprava Aplikácií'
                                                Uninstall = '{Toolkit\CompanyName} - Odinštalovanie Aplikácie'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'sv' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Installation påbörjad.'
                                            Repair = 'Reparation påbörjad.'
                                            Uninstall = 'Avinstallation påbörjad.'
                                        }
                                        Complete = @{
                                            Install = 'Installation slutförd.'
                                            Repair = 'Reparation slutförd.'
                                            Uninstall = 'Avinstallation slutförd.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Installationen slutförd. En omstart krävs.'
                                            Repair = 'Reparation slutförd. En omstart krävs.'
                                            Uninstall = 'Avinstallation slutförd. En omstart krävs.'
                                        }
                                        FastRetry = @{
                                            Install = 'Installationen slutfördes inte.'
                                            Repair = 'Reparationen slutfördes inte.'
                                            Uninstall = 'Avinstallationen slutfördes inte.'
                                        }
                                        Error = @{
                                            Install = 'Installationen misslyckades.'
                                            Repair = 'Reparationen misslyckades.'
                                            Uninstall = 'Avinstallationen misslyckades.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Den här applikationen har temporärt blockerats så att en installation kan slutföras.'
                                            Repair = 'Den här applikationen har temporärt blockerats så att en reparation kan slutföras.'
                                            Uninstall = 'Den här applikationen har temporärt blockerats så att en avinstallation kan slutföras.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation av App'
                                            Repair = '{Toolkit\CompanyName} - Reparation av App'
                                            Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Du har inte tillräckligt med ledigt diskutrymme för att kunna installera:`n{0}`n`nDiskutrymme som krävs: {1}MB`nLedigt diskutrymme: {2}MB`n`nVar vänlig frigör tillräckligt med diskutrymme för att kunna fortsätta med installationen."
                                            Repair = "Du har inte tillräckligt med ledigt diskutrymme för att kunna reparera:`n{0}`n`nDiskutrymme som krävs: {1}MB`nLedigt diskutrymmee: {2}MB`n`nVar vänlig frigör tillräckligt med diskutrymme för att kunna fortsätta med reparationen."
                                            Uninstall = "Du har inte tillräckligt med ledigt diskutrymme för att kunna avinstallera:`n{0}`n`nDiskutrymme som krävs: {1}MB`nLedigt diskutrymmee: {2}MB`n`nVar vänlig frigör tillräckligt med diskutrymme för att kunna fortsätta med avinstallationen."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation av App'
                                            Repair = '{Toolkit\CompanyName} - Reparation av App'
                                            Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Välj ett objekt:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Installation pågår. Var god vänta…'
                                            Repair = 'Reparation pågår. Var god vänta…'
                                            Uninstall = 'Avinstallation pågår. Var god vänta…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Detta fönster stängs automatiskt när installationen är klar.'
                                            Repair = 'Detta fönster stängs automatiskt när reparationen är klar.'
                                            Uninstall = 'Detta fönster stängs automatiskt när avinstallationen är klar.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation av App'
                                            Repair = '{Toolkit\CompanyName} - Reparation av App'
                                            Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Minimera'
                                        ButtonRestartNow = 'Starta om nu'
                                        Message = @{
                                            Install = 'För att installationen ska kunna slutföras måste du starta om datorn.'
                                            Repair = 'För att reparationen ska kunna slutföras måste du starta om datorn.'
                                            Uninstall = 'För att avinstallationen ska kunna slutföras måste du starta om datorn.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Din dator kommer att startas om automatiskt när nedräkningen är slut.'
                                        MessageTime = 'Var vänlig spara ditt arbete och starta om datorn innan tiden går ut.'
                                        TimeRemaining = 'Återstående tid:'
                                        Title = 'Omstart Krävs'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Installation av App'
                                            Repair = '{Toolkit\CompanyName} - Reparation av App'
                                            Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Följande applikation kommer att installeras:'
                                                Repair = 'Följande applikation kommer att repareras:'
                                                Uninstall = 'Följande applikation kommer att avinstalleras:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Följande program måste stängas innan installationen kan fortsätta.`n`nVar vänlig spara ditt arbete, stäng de öppna programmen och`nklicka sedan på `"Fortsätt`".`nAlternativt, spara ditt arbete och klicka sedan på `"Stäng Program`"."
                                                Repair = "Följande program måste stängas innan reparationen kan fortsätta.`n`nVar vänlig spara ditt arbete, stäng de öppna programmen och`nklicka sedan på `"Fortsätt`".`nAlternativt, spara ditt arbete och klicka sedan på `"Stäng Program`"."
                                                Uninstall = "Följande program måste stängas innan avinstallationen kan fortsätta.`n`nVar vänlig spara ditt arbete, stäng de öppna programmen och`nklicka sedan på `"Fortsätt`".`nAlternativt, spara ditt arbete och klicka sedan på `"Stäng Program`"."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Du kan välja att fördröja installationen ett begränsat antal gånger under en begränsad tid:'
                                                Repair = 'Du kan välja att fördröja reparationen ett begränsat antal gånger under en begränsad tid:'
                                                Uninstall = 'Du kan välja att fördröja avinstallationen ett begränsat antal gånger under en begränsad tid:'
                                            }
                                            DeferralsRemaining = 'Antal återstående fördröjningar:'
                                            DeferralDeadline = 'Tidsfrist:'
                                            ExpiryWarning = 'När antalet fördröjningar är slut eller deadline inträffat är detta alternativ inte längre tillgängligt.'
                                            CountdownDefer = @{
                                                Install = 'Installationen kommer automatiskt att fortsätta om:'
                                                Repair = 'Reparationen kommer automatiskt att fortsätta om:'
                                                Uninstall = 'Avinstallationen kommer automatiskt att fortsätta om:'
                                            }
                                            CountdownClose = @{
                                                Install = 'OBS: Program stängs automatisk om:'
                                                Repair = 'OBS: Program stängs automatisk om:'
                                                Uninstall = 'OBS: Program stängs automatisk om:'
                                            }
                                            ButtonClose = 'Stäng &Program'
                                            ButtonDefer = '&Skjut Upp'
                                            ButtonContinue = '&Fortsätt'
                                            ButtonContinueTooltip = 'Välj "Fortsätt" först efter att du har stängt ovanstående program.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Följande program måste stängas. Var vänlig spara ditt arbete och stäng sedan de öppna programmen.'
                                                Repair = 'Följande program måste stängas. Var vänlig spara ditt arbete och stäng sedan de öppna programmen.'
                                                Uninstall = 'Följande program måste stängas. Var vänlig spara ditt arbete och stäng sedan de öppna programmen.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = 'Välj Installera för att fortsätta med installationen.'
                                                Repair = 'Välj Reparera för att fortsätta med reparationen.'
                                                Uninstall = 'Välj Avinstallera för att fortsätta med avinstallationen.'
                                            }
                                            AutomaticStartCountdown = 'Fortsätter automatisk om:'
                                            DeferralsRemaining = 'Antal återstående fördröjningar'
                                            DeferralDeadline = 'Deadline:'
                                            ButtonLeftText = @{
                                                Install = 'Stäng Appar & Installera'
                                                Repair = 'Stäng Appar & Reparera'
                                                Uninstall = 'Stäng Appar & Avinstallera'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Installera'
                                                Repair = 'Reparera'
                                                Uninstall = 'Avinstallera'
                                            }
                                            ButtonRightText = 'Skjut upp'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Installation av App'
                                                Repair = '{Toolkit\CompanyName} - Reparation av App'
                                                Uninstall = '{Toolkit\CompanyName} - Avinstallation av App'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'tr' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = 'Kurulum başlatıldı.'
                                            Repair = 'Onarım başlatıldı.'
                                            Uninstall = 'Kaldırma işlemi başladı.'
                                        }
                                        Complete = @{
                                            Install = 'Kurulum tamamlandı.'
                                            Repair = 'Onarım tamamlandı.'
                                            Uninstall = 'Kaldırma işlemi tamamlandı.'
                                        }
                                        RestartRequired = @{
                                            Install = 'Kurulum tamamlandı. Yeniden başlatma gerekli.'
                                            Repair = 'Onarım tamamlandı. Yeniden başlatma gerekli.'
                                            Uninstall = 'Kaldırma tamamlandı. Yeniden başlatma gereklidir.'
                                        }
                                        FastRetry = @{
                                            Install = 'Kurulum tamamlanmadı.'
                                            Repair = 'Onarım tamamlanmadı.'
                                            Uninstall = 'Kaldırma tamamlanmadı.'
                                        }
                                        Error = @{
                                            Install = 'Kurulum başarısız oldu.'
                                            Repair = 'Onarım başarısız oldu.'
                                            Uninstall = 'Kaldırma başarısız oldu.'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = 'Bir yükleme işleminin tamamlanabilmesi için bu uygulamanın başlatılması geçici olarak engellendi.'
                                            Repair = 'Bir onarım işleminin tamamlanabilmesi için bu uygulamanın başlatılması geçici olarak engellendi.'
                                            Uninstall = 'Bir kaldırma işleminin tamamlanabilmesi için bu uygulamanın başlatılması geçici olarak engellendi.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Uygulama Yükleme'
                                            Repair = '{Toolkit\CompanyName} - Uygulama Onarımı'
                                            Uninstall = '{Toolkit\CompanyName} - Uygulama Kaldırma'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "Şunun kurulumunu tamamlamak için yeterli disk alanınız yok:`n{0}`n`nSpace required: {1}MB`nMevcut alan: {2}MB`n`nLütfen yüklemeye devam etmek için yeterli disk alanı boşaltın."
                                            Repair = "Şunun onarımını tamamlamak için yeterli disk alanınız yok:`n{0}`n`nSpace required: {1}MB`nMevcut alan: {2}MB`n`nOnarım işlemine devam etmek için lütfen yeterli disk alanını boşaltın."
                                            Uninstall = "Kaldırma işlemini tamamlamak için yeterli disk alanınız yok:`n{0}`n`nSpace required: {1}MB`nKullanılabilir alan: {2}MB`n`nKaldırma işlemine devam etmek için lütfen yeterli disk alanı boşaltın."
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Uygulama Yükleme'
                                            Repair = '{Toolkit\CompanyName} - Uygulama Onarımı'
                                            Uninstall = '{Toolkit\CompanyName} - Uygulama Kaldırma'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = 'Bir öğe seçin:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = 'Kurulum devam ediyor. Lütfen bekleyin…'
                                            Repair = 'Onarım devam ediyor. Lütfen bekleyin…'
                                            Uninstall = 'Kaldırma işlemi devam ediyor. Lütfen bekleyin…'
                                        }
                                        MessageDetail = @{
                                            Install = 'Yükleme tamamlandığında bu pencere otomatik olarak kapanacaktır.'
                                            Repair = 'Onarım tamamlandığında bu pencere otomatik olarak kapanacaktır.'
                                            Uninstall = 'Kaldırma işlemi tamamlandığında bu pencere otomatik olarak kapanacaktır.'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Uygulama Yükleme'
                                            Repair = '{Toolkit\CompanyName} - Uygulama Onarımı'
                                            Uninstall = '{Toolkit\CompanyName} - Uygulama Kaldırma'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = 'Küçült'
                                        ButtonRestartNow = 'Şimdi Yeniden Başlat'
                                        Message = @{
                                            Install = 'Yüklemenin tamamlanması için bilgisayarınızı yeniden başlatmanız gerekir.'
                                            Repair = 'Onarımın tamamlanması için bilgisayarınızı yeniden başlatmanız gerekir.'
                                            Uninstall = 'Kaldırma işleminin tamamlanması için bilgisayarınızı yeniden başlatmanız gerekir.'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = 'Geri sayımın sonunda bilgisayarınız otomatik olarak yeniden başlatılacaktır.'
                                        MessageTime = 'Lütfen çalışmanızı kaydedin ve ayrılan süre içinde yeniden başlatın.'
                                        TimeRemaining = 'Kalan süre:'
                                        Title = 'Yeniden Başlatma Gerekli'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - Uygulama Yükleme'
                                            Repair = '{Toolkit\CompanyName} - Uygulama Onarımı'
                                            Uninstall = '{Toolkit\CompanyName} - Uygulama Kaldırma'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = 'Aşağıdaki uygulama yüklenmek üzere:'
                                                Repair = 'Aşağıdaki uygulama onarılmak üzere:'
                                                Uninstall = 'Aşağıdaki uygulama kaldırılmak üzere:'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "Kurulumun devam edebilmesi için aşağıdaki programların kapatılması gerekir.`n`nLütfen çalışmanızı kaydedin, programları kapatın ve sonra devam edin. Alternatif olarak, çalışmanızı kaydedin ve `“Programları Kapat`” a tıklayın."
                                                Repair = "Onarımın devam edebilmesi için aşağıdaki programların kapatılması gerekir.`n`nLütfen çalışmanızı kaydedin, programları kapatın ve devam edin. Alternatif olarak, çalışmanızı kaydedin ve `“Programları Kapat`” a tıklayın."
                                                Uninstall = "Kaldırma işleminin devam edebilmesi için aşağıdaki programların kapatılması gerekir.`n`nLütfen çalışmanızı kaydedin, programları kapatın ve devam edin. Alternatif olarak, çalışmanızı kaydedin ve `“Programları Kapat`” a tıklayın."
                                            }
                                            ExpiryMessage = @{
                                                Install = 'Erteleme süresi dolana kadar yüklemeyi ertelemeyi seçebilirsiniz:'
                                                Repair = 'Erteleme süresi dolana kadar onarımı ertelemeyi seçebilirsiniz:'
                                                Uninstall = 'Erteleme süresi dolana kadar kaldırma işlemini ertelemeyi seçebilirsiniz:'
                                            }
                                            DeferralsRemaining = 'Kalan Ertelemeler:'
                                            DeferralDeadline = 'Son Tarih:'
                                            ExpiryWarning = 'Erteleme süresi sona erdiğinde, artık erteleme seçeneğiniz olmayacaktır.'
                                            CountdownDefer = @{
                                                Install = 'Kurulum otomatik olarak şu şekilde devam edecektir:'
                                                Repair = 'Onarım otomatik olarak şu şekilde devam edecek:'
                                                Uninstall = 'Kaldırma işlemi otomatik olarak şu şekilde devam edecektir:'
                                            }
                                            CountdownClose = @{
                                                Install = 'NOT: Program(lar) şu süre içinde otomatik olarak kapatılacaktır:'
                                                Repair = 'NOT: Program(lar) şu süre içinde otomatik olarak kapatılacaktır:'
                                                Uninstall = 'NOT: Program(lar) şu süre içinde otomatik olarak kapatılacaktır:'
                                            }
                                            ButtonClose = '&Programları Kapat'
                                            ButtonDefer = '&Ertele'
                                            ButtonContinue = '&Devam etmek'
                                            ButtonContinueTooltip = 'Yalnızca yukarıda listelenen uygulama(lar)ı kapattıktan sonra “Devam ”ı seçin.'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = 'Aşağıdaki uygulamalar otomatik olarak kapatılacağı için devam etmeden önce lütfen çalışmanızı kaydedin.'
                                                Repair = 'Aşağıdaki uygulamalar otomatik olarak kapatılacağı için devam etmeden önce lütfen çalışmanızı kaydedin.'
                                                Uninstall = 'Aşağıdaki uygulamalar otomatik olarak kapatılacağı için devam etmeden önce lütfen çalışmanızı kaydedin.'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = "Lütfen yüklemeye devam etmek için Yükle'yi seçin."
                                                Repair = "Onarıma devam etmek için lütfen Onar'ı seçin."
                                                Uninstall = "Kaldırma işlemine devam etmek için lütfen Kaldır'ı seçin."
                                            }
                                            AutomaticStartCountdown = 'Otomatik Başlatma Geri Sayımı'
                                            DeferralsRemaining = 'Kalan Ertelemeler'
                                            DeferralDeadline = 'Erteleme Son Tarihi'
                                            ButtonLeftText = @{
                                                Install = 'Uygulamaları Kapat ve Yükle'
                                                Repair = 'Uygulamaları Kapat ve Onar'
                                                Uninstall = 'Uygulamaları Kapat ve Kaldır'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = 'Yükle'
                                                Repair = 'Onarım'
                                                Uninstall = 'Kaldır'
                                            }
                                            ButtonRightText = 'Ertele'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - Uygulama Yükleme'
                                                Repair = '{Toolkit\CompanyName} - Uygulama Onarımı'
                                                Uninstall = '{Toolkit\CompanyName} - Uygulama Kaldırma'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'zh-CN' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = '安装开始。'
                                            Repair = '修复开始。'
                                            Uninstall = '卸载开始。'
                                        }
                                        Complete = @{
                                            Install = '安装完成。'
                                            Repair = '修复完成。'
                                            Uninstall = '卸载完成。'
                                        }
                                        RestartRequired = @{
                                            Install = '安装完成。需要重启。'
                                            Repair = '修复完成。需要重启。'
                                            Uninstall = '卸载完成。需要重启。'
                                        }
                                        FastRetry = @{
                                            Install = '安装未完成。'
                                            Repair = '修复未完成。'
                                            Uninstall = '卸载未完成。'
                                        }
                                        Error = @{
                                            Install = '安装失败。'
                                            Repair = '修复失败。'
                                            Uninstall = '卸载失败。'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = '启动此应用程序已被临时阻止，以便完成安装操作。'
                                            Repair = '启动此应用程序已被暂时阻止，以便完成修复操作。'
                                            Uninstall = '启动此应用程序已被暂时阻止，以便完成卸载操作。'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 应用程序安装'
                                            Repair = '{Toolkit\CompanyName} - 应用程序修复'
                                            Uninstall = '{Toolkit\CompanyName} - 应用程序卸载'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "您没有足够的磁盘空间来完成以下安装：`n{0}`n`n所需空间：{1}MB`n可用空间：{2}MB`n`n请释放足够的磁盘空间，以便继续安装。"
                                            Repair = "您没有足够的磁盘空间来修复：`n{0}`n`n所需空间：{1}MB`n可用空间：{2}MB`n`n请释放足够的磁盘空间以继续修复。"
                                            Uninstall = "您没有足够的磁盘空间来完成卸载：`n{0}`n`n所需空间：{1}MB`n可用空间：{2}MB`n`n请释放足够的磁盘空间，以便继续卸载。"
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 应用程序安装'
                                            Repair = '{Toolkit\CompanyName} - 应用程序修复'
                                            Uninstall = '{Toolkit\CompanyName} - 应用程序卸载'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = '选择一个项目:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = '正在安装。请稍候……'
                                            Repair = '修复中。请稍候…'
                                            Uninstall = '卸载中。请稍候…'
                                        }
                                        MessageDetail = @{
                                            Install = '安装完成后，此窗口将自动关闭。'
                                            Repair = '修复完成后，此窗口将自动关闭。'
                                            Uninstall = '卸载完成后，此窗口将自动关闭。'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 应用程序安装'
                                            Repair = '{Toolkit\CompanyName} - 应用程序修复'
                                            Uninstall = '{Toolkit\CompanyName} - 应用程序卸载'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = '最小化'
                                        ButtonRestartNow = '立即重启'
                                        Message = @{
                                            Install = '为了完成安装，您必须重启计算机。'
                                            Repair = '为了完成修复，您必须重新启动计算机。'
                                            Uninstall = '为了完成卸载，您必须重新启动计算机。'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = '倒计时结束后，您的计算机将自动重启。'
                                        MessageTime = '请保存您的工作并在指定时间内重新启动。'
                                        TimeRemaining = '剩余时间：'
                                        Title = '需要重新启动'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 应用程序安装'
                                            Repair = '{Toolkit\CompanyName} - 应用程序修复'
                                            Uninstall = '{Toolkit\CompanyName} - 应用程序卸载'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = '以下应用程序即将安装：'
                                                Repair = '以下应用程序即将修复：'
                                                Uninstall = '以下应用程序即将卸载：'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "在继续安装前必须关闭以下程序。`n`n请保存您的工作，关闭程序，然后继续。或者，保存您的工作并点击 `“关闭程序`”。"
                                                Repair = "在继续修复前必须关闭以下程序。`n`n请保存您的工作，关闭程序，然后继续。或者，保存您的工作并点击 `“关闭程序`”。"
                                                Uninstall = "在卸载前必须关闭以下程序。`n`n请保存您的工作，关闭程序，然后继续。或者，保存您的工作并点击 `“关闭程序`”。"
                                            }
                                            ExpiryMessage = @{
                                                Install = '您可以选择推迟安装，直到过期：'
                                                Repair = '您可以选择推迟修复，直到延期过期：'
                                                Uninstall = '您可以选择推迟卸载，直到延期过期：'
                                            }
                                            DeferralsRemaining = '剩余延期：'
                                            DeferralDeadline = '截止日期：'
                                            ExpiryWarning = '一旦延期过期，您将不再有推迟选项。'
                                            CountdownDefer = @{
                                                Install = '安装将在以下时间自动继续：'
                                                Repair = '修复将在以下时间自动继续：'
                                                Uninstall = '卸载将在以下时间自动继续：'
                                            }
                                            CountdownClose = @{
                                                Install = '注意：程序将在以下时间自动关闭：'
                                                Repair = '注意：程序将在以下时间自动关闭：'
                                                Uninstall = '注意：程序将在以下时间自动关闭：'
                                            }
                                            ButtonClose = '关闭 &程序'
                                            ButtonDefer = '&推迟'
                                            ButtonContinue = '&继续'
                                            ButtonContinueTooltip = '关闭以上列出的应用程序后，仅选择“继续”。'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = '请保存您的工作，然后继续，因为以下应用程序将自动关闭。'
                                                Repair = '请保存您的工作，然后继续，因为以下应用程序将自动关闭。'
                                                Uninstall = '请保存您的工作，然后继续，因为以下应用程序将自动关闭。'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = '请选择安装以继续安装。如果您还有任何延迟，也可以选择延迟安装。'
                                                Repair = '请选择修复以继续修复。'
                                                Uninstall = '请选择卸载以继续卸载。'
                                            }
                                            AutomaticStartCountdown = '自动启动倒计时'
                                            DeferralsRemaining = '剩余延期'
                                            DeferralDeadline = '延期截止日期'
                                            ButtonLeftText = @{
                                                Install = '关闭应用程序并安装'
                                                Repair = '关闭应用程序并修复'
                                                Uninstall = '关闭应用程序并卸载'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = '安装'
                                                Repair = '修复'
                                                Uninstall = '卸载'
                                            }
                                            ButtonRightText = '延迟'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - 应用程序安装'
                                                Repair = '{Toolkit\CompanyName} - 应用程序修复'
                                                Uninstall = '{Toolkit\CompanyName} - 应用程序卸载'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                            'zh-HK' = {
                                @{
                                    BalloonTip = @{
                                        Start = @{
                                            Install = '安裝開始。'
                                            Repair = '修復開始。'
                                            Uninstall = '卸載開始。'
                                        }
                                        Complete = @{
                                            Install = '安裝完成。'
                                            Repair = '修復完成。'
                                            Uninstall = '解除安裝完成。'
                                        }
                                        RestartRequired = @{
                                            Install = '安裝完成。需要重新啟動。'
                                            Repair = '修復完成。需要重新啟動。'
                                            Uninstall = '卸載完成。需要重新啟動。'
                                        }
                                        FastRetry = @{
                                            Install = '安裝未完成。'
                                            Repair = '修復未完成。'
                                            Uninstall = '解除安裝未完成。'
                                        }
                                        Error = @{
                                            Install = '安裝失敗。'
                                            Repair = '維修失敗。'
                                            Uninstall = '解除安裝失敗。'
                                        }
                                    }
                                    BlockExecutionText = @{
                                        Message = @{
                                            Install = '啟動此應用程式已被暫時阻止，以便完成安裝作業。'
                                            Repair = '啟動此應用程式已被暫時阻止，以便完成修復操作。'
                                            Uninstall = '啟動此應用程式已暫時受阻，以便完成卸載作業。'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 應用程式安裝'
                                            Repair = '{Toolkit\CompanyName} - 應用程式維修'
                                            Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
                                        }
                                    }
                                    DiskSpaceText = @{
                                        Message = @{
                                            Install = "您沒有足夠的磁碟空間來完成安裝：`n{0}`n`n所需空間： {1}MB `n 可用空間： {2}MB`n`n請釋放足夠的磁碟空間，以便繼續安裝。"
                                            Repair = "您沒有足夠的磁碟空間來完成修復:`n{0}`n`n所需空間： {1}MB`n可用空間： {2}MB`n`n請釋放足夠的磁碟空間，以便繼續進行修復。"
                                            Uninstall = "您沒有足夠的磁碟空間來完成卸載:`n{0}`n`n所需空間： {1}MB`n可用空間： {2}MB`n`n請釋放足夠的磁碟空間，以便繼續卸載。"
                                        }
                                    }
                                    InstallationPrompt = @{
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 應用程式安裝'
                                            Repair = '{Toolkit\CompanyName} - 應用程式維修'
                                            Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
                                        }
                                    }
                                    ListSelectionPrompt = @{
                                        ListSelectionMessage = '選擇一個項目:'
                                    }
                                    ProgressPrompt = @{
                                        Message = @{
                                            Install = '安裝中。 請稍候…'
                                            Repair = '維修中。 請稍候…'
                                            Uninstall = '正在卸載。 請稍候…'
                                        }
                                        MessageDetail = @{
                                            Install = '安裝完成後，此視窗將自動關閉。'
                                            Repair = '維修完成後，此視窗將自動關閉。'
                                            Uninstall = '卸載完成後，此視窗將自動關閉。'
                                        }
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 應用程式安裝'
                                            Repair = '{Toolkit\CompanyName} - 應用程式維修'
                                            Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
                                        }
                                    }
                                    RestartPrompt = @{
                                        ButtonRestartLater = '最小化'
                                        ButtonRestartNow = '現在重新啟動'
                                        Message = @{
                                            Install = '為了完成安裝，您必須重新啟動電腦。'
                                            Repair = '為了完成維修，您必須重新啟動電腦。'
                                            Uninstall = '為了讓卸載完成，您必須重新啟動電腦。'
                                        }
                                        CustomMessage = $null
                                        MessageRestart = '您的電腦會在倒數計時結束時自動重新啟動。'
                                        MessageTime = '請儲存您的工作，並在指定時間內重新啟動。'
                                        TimeRemaining = '剩餘時間：'
                                        Title = '需要重新啟動'
                                        Subtitle = @{
                                            Install = '{Toolkit\CompanyName} - 應用程式安裝'
                                            Repair = '{Toolkit\CompanyName} - 應用程式維修'
                                            Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
                                        }
                                    }
                                    CloseAppsPrompt = @{
                                        Classic = @{
                                            WelcomeMessage = @{
                                                Install = '下列應用程式即將安裝：'
                                                Repair = '下列應用程式即將被修復：'
                                                Uninstall = '下列應用程式即將被解除安裝：'
                                            }
                                            CloseAppsMessage = @{
                                                Install = "在繼續安裝之前，必須關閉下列程式。`n`n請儲存您的工作，關閉程式，然後繼續。或者，保存您的工作，然後按一下`「關閉程式`」。"
                                                Repair = "在進行修復之前，必須關閉下列程式。`n`n請儲存您的工作，關閉程式，然後繼續。或者，保存您的工作并单击 「关闭程序」。"
                                                Uninstall = "卸載程式前，必須先關閉下列程式。或者，保存您的工作並點擊 「關閉程式」。"
                                            }
                                            ExpiryMessage = @{
                                                Install = '您可以選擇延遲安裝，直到延遲到期:'
                                                Repair = '您可以選擇延遲修復，直到延遲到期：'
                                                Uninstall = '您可以選擇延遲卸載，直到延遲期限到期：'
                                            }
                                            DeferralsRemaining = '剩餘的延遲：'
                                            DeferralDeadline = '截止日期:'
                                            ExpiryWarning = '一旦延遲到期，您將無法再選擇延遲。'
                                            CountdownDefer = @{
                                                Install = '安裝會自動繼續：'
                                                Repair = '維修會自動繼續進行：'
                                                Uninstall = '卸載將自動繼續中：'
                                            }
                                            CountdownClose = @{
                                                Install = '注意：程式會自動關閉：'
                                                Repair = '注意：程式會自動關閉：'
                                                Uninstall = '注意：程式會自動關閉：'
                                            }
                                            ButtonClose = '關閉程式'
                                            ButtonDefer = '延遲'
                                            ButtonContinue = '繼續'
                                            ButtonContinueTooltip = '僅在關閉上述列出的應用程式後選擇 「繼續」。'
                                        }
                                        Fluent = @{
                                            DialogMessage = @{
                                                Install = '請先保存您的工作再繼續，因為下列應用程式會自動關閉。'
                                                Repair = '請先保存您的工作再繼續，因為下列應用程式會自動關閉。'
                                                Uninstall = '請先保存您的工作再繼續，因為下列應用程式會自動關閉。'
                                            }
                                            DialogMessageNoProcesses = @{
                                                Install = '請選擇「安裝」繼續安裝。'
                                                Repair = '請選擇「維修」繼續進行維修。'
                                                Uninstall = '請選擇「卸載」繼續進行卸載。'
                                            }
                                            AutomaticStartCountdown = '自動啟動倒數計時'
                                            DeferralsRemaining = '剩餘延期'
                                            DeferralDeadline = '延期截止日期'
                                            ButtonLeftText = @{
                                                Install = '關閉應用程式並安裝'
                                                Repair = '關閉應用程式與修復'
                                                Uninstall = '關閉應用程式並卸載'
                                            }
                                            ButtonLeftNoProcessesText = @{
                                                Install = '安裝'
                                                Repair = '修復'
                                                Uninstall = '解除安裝'
                                            }
                                            ButtonRightText = '延遲'
                                            Subtitle = @{
                                                Install = '{Toolkit\CompanyName} - 應用程式安裝'
                                                Repair = '{Toolkit\CompanyName} - 應用程式維修'
                                                Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
                                            }
                                        }
                                        CustomMessage = $null
                                    }
                                }
                            }
                        }).AsReadOnly()
                    Config = ([ordered]@{
                            '' = {
                                @{
                                    Assets = @{
                                        # Specify filename or Base64 string of the logo.
                                        Logo = '..\Assets\AppIcon.png'

                                        # Specify filename or Base64 string of the logo (for dark mode).
                                        LogoDark = '..\Assets\AppIcon.png'

                                        # Specify filename or Base64 string of the banner (Classic-only).
                                        Banner = '..\Assets\Banner.Classic.png'

                                        # Specify optional filename or Base64 string of the tray icon.
                                        TaskbarIcon = $null
                                    }

                                    MSI = @{
                                        # MSI install parameters used in interactive mode.
                                        InstallParams = 'REBOOT=ReallySuppress /QN'

                                        # Logging level used for MSI logging.
                                        LoggingOptions = '/L*V'

                                        # Log path used for MSI logging. Uses the same path as Toolkit when null or empty.
                                        LogPath = $null

                                        # Log path used for MSI logging when RequireAdmin is False. Uses the same path as Toolkit when null or empty.
                                        LogPathNoAdminRights = $null

                                        # The length of time in seconds to wait for the MSI installer service to become available. Default is 600 seconds (10 minutes).
                                        MutexWaitTime = 600

                                        # MSI install parameters used in silent mode.
                                        SilentParams = 'REBOOT=ReallySuppress /QN'

                                        # MSI uninstall parameters.
                                        UninstallParams = 'REBOOT=ReallySuppress /QN'
                                    }

                                    Toolkit = @{
                                        # Specify the path for the cache folder.
                                        CachePath = '$envProgramData\SoftwareCache'

                                        # The name to show by default for dialog subtitles, balloon notifications, etc.
                                        CompanyName = 'PSAppDeployToolkit'

                                        # Specify if the log files should be bundled together in a compressed zip file.
                                        CompressLogs = $false

                                        # Choose from either 'Native' for native PowerShell file copy via Copy-ADTFile, or 'Robocopy' to use robocopy.exe.
                                        FileCopyMode = 'Native'

                                        # Specify if an existing log file should be appended to.
                                        LogAppend = $true

                                        # Specify if debug messages such as bound parameters passed to a function should be logged.
                                        LogDebugMessage = $false

                                        # Specify the maximum amount of hierarchical structures to maintain when LogToHierarchy is true.
                                        LogMaxHierarchy = 3

                                        # Specify maximum number of previous log files to retain.
                                        LogMaxHistory = 10

                                        # Specify maximum file size limit for log file in megabytes (MB).
                                        LogMaxSize = 10

                                        # Log path used for Toolkit logging.
                                        LogPath = '$envWinDir\Logs\Software'

                                        # Same as LogPath but used when RequireAdmin is False.
                                        LogPathNoAdminRights = '$envProgramData\Logs\Software'

                                        # Specifies that logging should be to a hierarchical structure of AppVendor\AppName\AppVersion. Takes precident over "LogToSubfolder" if both are set.
                                        LogToHierarchy = $false

                                        # Specifies that a subfolder based on InstallName should be used for all log capturing.
                                        LogToSubfolder = $false

                                        # Specify if log file should be a CMTrace compatible log file or a Legacy text log file.
                                        LogStyle = 'CMTrace'

                                        # Specify if log messages should be written to the console.
                                        LogWriteToHost = $true

                                        # Specify if console log messages should bypass PowerShell's subsystems and be sent direct to stdout/stderr.
                                        # This only applies if "LogWriteToHost" is true, and the script is being ran in a ConsoleHost (not the ISE, or another host).
                                        LogHostOutputToStdStreams = $false

                                        # Registry key used to store toolkit information (with PSAppDeployToolkit as child registry key), e.g. deferral history.
                                        RegPath = 'HKLM:\SOFTWARE'

                                        # Same as RegPath but used when RequireAdmin is False. Bear in mind that since this Registry Key should be writable without admin permission, regular users can modify it also.
                                        RegPathNoAdminRights = 'HKCU:\SOFTWARE'

                                        # Path used to store temporary Toolkit files (with PSAppDeployToolkit as subdirectory), e.g. cache toolkit for cleaning up blocked apps. Normally you don't want this set to a path that is writable by regular users, this might lead to a security vulnerability. The default Temp variable for the LocalSystem account is C:\Windows\Temp.
                                        TempPath = '$envTemp'

                                        # Same as TempPath but used when RequireAdmin is False.
                                        TempPathNoAdminRights = '$envTemp'
                                    }

                                    UI = @{
                                        # Used to turn automatic balloon notifications on or off.
                                        BalloonNotifications = $true

                                        # Choose from either 'Fluent' for contemporary dialogs, or 'Classic' for PSAppDeployToolkit 3.x WinForms dialogs.
                                        DialogStyle = 'Fluent'

                                        # Specify the Accent Color in hex (with the first two characters for transparency, 00 = 0%, FF = 100%), e.g. 0xFF0078D7.
                                        # The value specified here should be literally typed (i.e. `FluentAccentColor = 0xFF0078D7`) and not wrapped in quotes.
                                        FluentAccentColor = $null

                                        # Exit code used when a UI prompt times out.
                                        DefaultExitCode = 1618

                                        # Time in seconds after which the prompt should be repositioned centre screen when the -PersistPrompt parameter is used. Default is 60 seconds.
                                        DefaultPromptPersistInterval = 60

                                        # Time in seconds to automatically timeout installation dialogs. Default is 55 minutes so that dialogs timeout before Intune times out.
                                        DefaultTimeout = 3300

                                        # Exit code used when a user opts to defer.
                                        DeferExitCode = 1602

                                        <# Specify a static UI language using the one of the Language Codes listed below to override the language culture detected on the system.
                                            Language Code    Language
                                            =============    ========
                                            ar               Arabic
                                            bg               Bulgarian
                                            cs               Czech
                                            da               Danish
                                            de               German
                                            en               English
                                            el               Greek
                                            es               Spanish
                                            fi               Finnish
                                            fr               French
                                            he               Hebrew
                                            hu               Hungarian
                                            it               Italian
                                            ja               Japanese
                                            ko               Korean
                                            lv               Latvian
                                            nl               Dutch
                                            nb               Norwegian (Bokmål)
                                            pl               Polish
                                            pt               Portuguese (Portugal)
                                            pt-BR            Portuguese (Brazil)
                                            ru               Russian
                                            sk               Slovak
                                            sv               Swedish
                                            tr               Turkish
                                            zh-CN            Chinese (Simplified)
                                            zh-HK            Chinese (Traditional)
                                        #>
                                        LanguageOverride = $null

                                        # Time in seconds after which to re-prompt the user to close applications in case they ignore the prompt or they cancel the application's save prompt.
                                        PromptToSaveTimeout = 120

                                        # Time in seconds after which the restart prompt should be re-displayed/repositioned when the -NoCountdown parameter is specified. Default is 600 seconds.
                                        RestartPromptPersistInterval = 600
                                    }
                                }
                            }
                        }).AsReadOnly()
                }).AsReadOnly()
            Callbacks = ([ordered]@{
                    [PSAppDeployToolkit.Foundation.CallbackType]::OnInit = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Foundation.CallbackType]::OnStart = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Foundation.CallbackType]::PreOpen = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Foundation.CallbackType]::PostOpen = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Foundation.CallbackType]::OnDefer = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Foundation.CallbackType]::PreClose = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Foundation.CallbackType]::PostClose = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Foundation.CallbackType]::OnFinish = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Foundation.CallbackType]::OnExit = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                }).AsReadOnly()
            Directories = [pscustomobject]@{
                Script = $null
                Config = $null
                Strings = $null
            }
            Durations = [pscustomobject]@{
                ModuleImport = $null
                ModuleInit = $null
            }
            ProcessExitEvent = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -SupportEvent -Action {
                if ($Script:ADT.ClientServerProcess)
                {
                    Close-ADTClientServerProcess
                }
            }
            SessionState = $ExecutionContext.get_SessionState()
            RestartOnExitCountdown = $null
            ClientServerProcess = $null
            Sessions = [System.Collections.Generic.List[PSAppDeployToolkit.Foundation.DeploymentSession]]::new()
            Environment = $null
            Language = $null
            Config = $null
            Strings = $null
            LastExitCode = 0
            Initialized = $false
        })

    # Registry path transformation constants used within Convert-ADTRegistryPath.
    New-Variable -Name Registry -Option Constant -Value ([ordered]@{
            PathMatches = [System.Collections.ObjectModel.ReadOnlyCollection[System.String]]$(
                ':\\'
                ':'
                '\\'
            )
            PathReplacements = ([ordered]@{
                    '^HKLM' = 'HKEY_LOCAL_MACHINE\'
                    '^HKCR' = 'HKEY_CLASSES_ROOT\'
                    '^HKCU' = 'HKEY_CURRENT_USER\'
                    '^HKU' = 'HKEY_USERS\'
                    '^HKCC' = 'HKEY_CURRENT_CONFIG\'
                    '^HKPD' = 'HKEY_PERFORMANCE_DATA\'
                }).AsReadOnly()
            WOW64Replacements = ([ordered]@{
                    '^(HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\|HKEY_CURRENT_USER\\SOFTWARE\\Classes\\|HKEY_CLASSES_ROOT\\)(AppID\\|CLSID\\|DirectShow\\|Interface\\|Media Type\\|MediaFoundation\\|PROTOCOLS\\|TypeLib\\)' = '$1Wow6432Node\$2'
                    '^HKEY_LOCAL_MACHINE\\SOFTWARE\\' = 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\'
                    '^HKEY_LOCAL_MACHINE\\SOFTWARE$' = 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node'
                    '^HKEY_CURRENT_USER\\Software\\Microsoft\\Active Setup\\Installed Components\\' = 'HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\'
                }).AsReadOnly()
        }).AsReadOnly()

    # Array of all PowerShell common parameter names.
    New-Variable -Name PowerShellCommonParameters -Option Constant -Value ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]]$([System.Management.Automation.PSCmdlet]::get_CommonParameters(); [System.Management.Automation.PSCmdlet]::get_OptionalCommonParameters()))

    # Lookup table for preference variables and their associated CommonParameter name.
    New-Variable -Name PreferenceVariableTable -Option Constant -Value ([ordered]@{
            'InformationAction' = 'InformationPreference'
            'ProgressAction' = 'ProgressPreference'
            'WarningAction' = 'WarningPreference'
            'Confirm' = 'ConfirmPreference'
            'Verbose' = 'VerbosePreference'
            'WhatIf' = 'WhatIfPreference'
            'Debug' = 'DebugPreference'
        }).AsReadOnly()

    # Send the module's database into the C# code for internal access.
    [PSAppDeployToolkit.Foundation.ModuleDatabase]::Init($ADT)
}
catch
{
    throw
}

# Ensure that the client/server process is closed on module remove.
$ModuleInfo.set_OnRemove({
        if ($Script:ADT.ClientServerProcess)
        {
            Close-ADTClientServerProcess
        }
        if ($Script:ADT.ProcessExitEvent)
        {
            Unregister-Event -SubscriptionId $Script:ADT.ProcessExitEvent.get_Id()
        }
        [PSAppDeployToolkit.Foundation.ModuleDatabase]::Clear()
    })

# Determine how long the import took.
$ADT.Durations.ModuleImport = [System.DateTime]::get_Now() - $ModuleImportStart
Remove-Variable -Name ModuleImportStart -Force -Confirm:$false
