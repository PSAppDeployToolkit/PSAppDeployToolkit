The toolkit has a zero-config MSI install feature which allows you to quickly execute an installation with zero configuration of the Deploy-Application.ps1 file.

To use this feature:

1)  Place your MSI file into the “Files” directory of the toolkit. This method only support the installation of one MSI, so if more than one MSI is found, then only the first one is selected.

2)  If you have an MST file, then place it into the “Files” directory of the toolkit. The MST file must have the same name as the MSI file. For example, if your MSI file name is test01.msi, then the MST file must be named test01.mst.

3)  If you have any MSP files, then place it into the “Files” directory of the toolkit. You can place more than one MSP file in the folder, but you must name the files in alphabetical order to control the order in which they are installed. MSP file will be installed in alphabetical order.