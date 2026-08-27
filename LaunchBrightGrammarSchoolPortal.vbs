Option Explicit
Dim WshShell, Fso, AppFolder, AppExe, AppPath
Set WshShell = CreateObject("WScript.Shell")
Set Fso = CreateObject("Scripting.FileSystemObject")
AppFolder = Fso.GetParentFolderName(WScript.ScriptFullName)
AppExe = "BrightGrammarSchoolPortal.exe"
AppPath = AppFolder & "\" & AppExe

' Start the backend if it is not already running. The backend itself
' opens the default browser after Kestrel reports ApplicationStarted.
Dim Wmi, Processes, Process, IsRunning
IsRunning = False
On Error Resume Next
Set Wmi = GetObject("winmgmts:\\.\root\cimv2")
Set Processes = Wmi.ExecQuery("Select * from Win32_Process where Name = '" & AppExe & "'")
For Each Process In Processes
  IsRunning = True
Next
On Error GoTo 0
If Not IsRunning Then
  WshShell.Run Chr(34) & AppPath & Chr(34), 7, False
Else
  WshShell.Run "http://localhost:5000", 1, False
End If
