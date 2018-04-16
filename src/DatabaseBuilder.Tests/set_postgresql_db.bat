@echo off
copy connectionStrings.config.postgresql connectionStrings.config
copy appsettings.json.postgresql appsettings.json

rem The next command update the time stamp so visual studio can detect changed file - https://superuser.com/a/764721/68199
copy connectionStrings.config+,,
copy appsettings.json+,,