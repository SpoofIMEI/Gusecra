# Gusecra (Guerrilla Secure Radio)
[![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
[![Linux](https://img.shields.io/badge/Linux-FCC624?style=for-the-badge&logo=linux&logoColor=black)](https://img.shields.io/badge/Linux-FCC624?style=for-the-badge&logo=linux&logoColor=black)
[![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)
[![macOS](https://img.shields.io/badge/mac%20os-000000?style=for-the-badge&logo=macos&logoColor=F0F0F0)](https://img.shields.io/badge/mac%20os-000000?style=for-the-badge&logo=macos&logoColor=F0F0F0)

### Encrypted protocol/wrapper for fldigi with cross platform GUI.
<img src="https://github.com/user-attachments/assets/b5ed217b-ab6e-4562-8c83-a4fd3760d310" width=575>


## Installation 
1. Make sure you have dotnet installed `sudo apt install dotnet9` (or download dotnet9 as an installer for windows)
2. Cd to GusecraGui and `dotnet run` or `dotnet build`
I might add prebuilt binaries later

## Usage
1. Start fldigi
2. Connect Gusecra
3. Set password if you want
4. Either wait for messages to appear at the bottom or send a message (will be sent through fldigi)

## For developers
Not going to write documentation for the lib since it's very simple and small. Just look at how I used it in GusecraGui/Views/MainWindow.axaml.cs.
