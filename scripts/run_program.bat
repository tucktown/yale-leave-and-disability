@echo off
echo Updating scenarios in bin/Debug directory...
copy /Y "..\ESLFeeder\Config\scenarios.json" "..\ESLFeeder\bin\Debug\net7.0\Config\scenarios.json"

echo Building project...
dotnet build "..\ESLFeeder\ESLFeeder.csproj"

echo Running program...
cd "..\ESLFeeder\bin\Debug\net7.0"
ESLFeeder.exe

cd "..\..\..\..\..\scripts" 