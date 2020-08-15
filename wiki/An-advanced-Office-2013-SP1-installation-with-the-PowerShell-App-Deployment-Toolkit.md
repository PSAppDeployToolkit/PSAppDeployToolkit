<!-- order:17 -->
### An advanced Office 2013 SP1 installation with the PowerShell App Deployment Toolkit

This example is provided as a script with the toolkit, in the “Examples” folder. This provides a number of benefits over the standard Microsoft Office Setup Bootstrapper:

  - A component based architecture so that core products can be installed, and subsequent components can be installed using the same package with different command-line switches

  - The ability to defer the installation up to 3 times

  - The ability to close any applications that could cause errors during the installation

  - Verification that the required disk space is available

  - Full removal of any previous version of Microsoft Office 2007, 2010 or 2013

  - Installation of any subsequent patches required after the base installation

  - Activation of Microsoft Office components

**Note:** Office requires a number of modifications in order to install. Please refer to Microsoft’s documentation on configuration. This installation script tries to take a lot of work out of the process for you, but you still need to know what you’re doing in order to set it up correctly.

The folder structure is laid out as follows:

  - Files
    
      - Office installation files should be placed here
        
          - Office Configuration MSP created with the Office Customisation Tool should be placed in the “Config” subfolder and be named Office2013ProPlus.MSP. Modify the script accordingly if you wish to change. For a basic MSP, you should probably configure Access, Word, Excel and PowerPoint to be the only core applications to install. We can add everything else as components.
        
          - Customised Config.xml file should be edited in “ProPlus.WW” subfolder. At a minimum, you should modify the settings as follows:
            
              - \<Display Level="none" CompletionNotice="no" SuppressModal="yes" NoCancel="yes" AcceptEula="yes" /\>
        
          - Security updates and service pack extracted MSPs should be placed in the “Updates” subfolder

  - SupportFiles
    
      - Contains custom Config.XML files which are used to add specific components that might be considered unnecessary in a standard Office install, but could be added later using command-line switches
    
      - Contains Office Scrub tools for Office 2007, 2010 and 2013

Once the folder structure is laid out correctly and the custom Deploy-Application.ps1 is added (as well as the AppDeployToolkit files themselves), the following command-lines are valid:

  - Deploy-Application.exe
    
      - Installs Office 2010 with core products

  - Deploy-Application.exe -AddInfoPath
    
      - Installs Office 2010 with core products and InfoPath

  - Deploy-Application.exe -AddComponentsOnly -AddInfoPath
    
      - Installs InfoPath to an existing Office 2013 installation
