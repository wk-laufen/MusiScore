mkdir .\.tools -Force | Out-Null
Invoke-WebRequest https://github.com/tailwindlabs/tailwindcss/releases/download/v3.1.8/tailwindcss-windows-x64.exe -OutFile .\.tools\tailwindcss.exe
