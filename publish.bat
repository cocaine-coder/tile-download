cd ./src/TileDownload.CLI
set output=./bin/publish
if exist "%output%" rd /S /Q "%output%"
dotnet publish -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -f net6.0 --self-contained -r win-x64 -o "%output%/TileDownload_win10-x64"
dotnet publish -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -f net6.0 --self-contained -r linux-x64 -o "%output%/TileDownload_linux-x64"
dotnet publish -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -f net6.0 --self-contained -r osx-x64 -o "%output%/TileDownload_osx-x64"