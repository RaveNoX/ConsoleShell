md %~dp0artifacts
%~dp0util\nuget.exe pack %~dp0src\SharpShell\SharpShell.csproj -OutputDirectory %~dp0artifacts -Properties Configuration=Release