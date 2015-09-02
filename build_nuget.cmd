md %~dp0artifacts
%~dp0util\nuget.exe pack %~dp0src\ConsoleShell\ConsoleShell.csproj -OutputDirectory %~dp0artifacts -Properties Configuration=Release