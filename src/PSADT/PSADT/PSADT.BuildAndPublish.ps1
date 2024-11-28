# Build and publish for win-x64
dotnet publish -c Release -r win-x64 --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:PublishTrimmed=true `
    /p:PublishDir=.\publish\x64\

# Build and publish for win-x86
dotnet publish -c Release -r win-x86 --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:PublishTrimmed=true `
    /p:PublishDir=.\publish\x86\
