@ECHO OFF
:: This file can now be deleted!
:: It was used when setting up the package solution (using https://github.com/LottePitcher/opinionated-package-starter)

:: set up git
git init
git branch -M main
git remote add origin https://github.com/Matthew-Wise/Umbraco-CSP-manager.git

:: ensure latest Umbraco templates used
dotnet new install Umbraco.Templates --force

:: use the umbraco-extension dotnet template to add the package project
cd src
dotnet new umbraco-extension -n "Umbraco.Community.CSPManager" --site-domain "https://localhost:44370" --include-example --allow-scripts Yes

:: replace package .csproj with the one from the template so has nuget info
cd Umbraco.Community.CSPManager
del Umbraco.Community.CSPManager.csproj
ren Umbraco.Community.CSPManager_nuget.csproj Umbraco.Community.CSPManager.csproj

:: add project to solution
cd..
dotnet sln add "Umbraco.Community.CSPManager"

:: add reference to project from test site
dotnet add "Umbraco.Community.CSPManager.TestSite/Umbraco.Community.CSPManager.TestSite.csproj" reference "Umbraco.Community.CSPManager/Umbraco.Community.CSPManager.csproj"
