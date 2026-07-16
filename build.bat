@echo off
dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true -c Release -o publish
