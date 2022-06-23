
FUNCTION ReadLaunchpadIniToFindRoleQuery() 

     DIM path, strLine, strParameter, fso, split0, split1
	 SET fso=CreateObject("Scripting.FileSystemObject")
	 path= "C:\ProgramData\Launchpad\LaunchPad.ini"
	 Set objFileToRead = fso.OpenTextFile(path, 1, False, -1)

	 do Until objFileToRead.AtEndOfStream
		strLine = objFileToRead.ReadLine()
		If InStr(strLine, "=")>0 Then
			strParameter = Split(strLine, "=")
			split0 = Trim(strParameter(0))
			split1 = Trim(strParameter(1))
			IF UCase(split0) = "ROLEQUERY" THEN
				roleQuery = split1
			END IF
		End If
	 loop
		objFileToRead.Close
		
    Wscript.Echo "roleQuery = " & roleQuery  
	ReadLaunchpadIniToFindRoleQuery	= roleQuery
END FUNCTION	
   
FUNCTION ReadSSOGroups(userName, roleQuery)

	Dim LPPath, roleFile, rQueryLen

	Set d = CreateObject("Scripting.Dictionary")
	Const ADS_NAME_TYPE_NT4 = 3
	Const ADS_NAME_INITTYPE_GC = 3
	Const ADS_NAME_TYPE_1779 = 1
	Const ADS_NAME_INITTYPE_SERVER = 2
	Const ADS_NAME_INITTYPE_DOMAIN = 1

	Set objNetwork = WScript.CreateObject("WScript.Network")
	Set FSO = CreateObject("Scripting.FileSystemObject")
	'strUserName =objNetwork.Username
	strUserName =userName

	'strComputerName = objNetwork.ComputerName
	strUserDomain = objNetwork.UserDomain
	Set objUser = GetObject("WinNT://" & strUserDomain & "/" & strUserName & ",user")
	'LPPath = "C:\ProgramData\Launchpad\"


	For Each objGroup In objUser.Groups

		strGroupName=objGroup.Name
		Set objTrans = CreateObject("NameTranslate")
		objTrans.Init ADS_NAME_INITTYPE_DOMAIN, strUserDomain
		strNTName = strUserDomain & "\" & strGroupName
		objTrans.Set ADS_NAME_TYPE_NT4, strNTName
		strGroupDN = objTrans.Get(ADS_NAME_TYPE_1779)
		strGroupDN = Replace(strGroupDN, "/", "\/")

		Set objGroup = GetObject("LDAP://" & strGroupDN)
		rQueryLen = Len(roleQuery)
		IF InStr(objGroup.CN, roleQuery) >0 Then
			RoleFileName = Mid(objGroup.CN, rQueryLen + 1)
			roleFile = "C:\ProgramData\Launchpad\" & RoleFileName & ".ini"
			If FSO.FileExists(roleFile) Then
				userRole = objGroup.CN
				userRoleFile = RoleFileName & ".ini"
			End If
			
		End IF
	   strgroupname=objGroup.CN
 
	next
	
	Wscript.Echo "userRole = " & userRole    
	Wscript.Echo "userRoleFile = " & userRoleFile   
	ReadSSOGroups=userRoleFile
    
END FUNCTION

Sub Main

	DIM objShell, objFileToRead, strFolder, loggedinUserCmd, FMBMFolder, FMBMFile, strLine, iCounter, userRoleFileAtShare, userRoleFileAtShareLM, userRoleFileLM, a, b, c, LPRefreshCmd
	DIM fso, loggedinUser, shareLocation, roleQuery, userRole, userRoleFile
	
	SET fso=CreateObject("Scripting.FileSystemObject")
	
	'Check for logged in user 3/4
	strFolder= "C:\Program Files (x86)\Imprivata\OneSign Agent\x64\"
	Set objShell = CreateObject("WScript.Shell")
	'objShell.CurrentDirectory = strFolder
	strPath = """C:\Program Files (x86)\Imprivata\OneSign Agent\x64\ISXMenu.exe"" getloggedinuser"
	Set loggedinUserCmd = objShell.Exec (strPath)
	loggedinUser = Left( (loggedinUserCmd.StdOut.ReadAll), 7)

	Wscript.Echo "Loggedin User = " & loggedinUser
	LPRefreshCmd = """C:\Program Files (x86)\Imprivata\OneSign Agent\x64\LaunchpadRefresh.cmd"""
	
	FMBMFile = "C:\ProgramData\Launchpad\INIToolBackup\FMBM\" &"FMBM_" & loggedinUser & ".txt"
	'Check if the FMBM file for the user exists
	If fso.FileExists(FMBMFile) Then
	'If file exists read shareLocation, roleQuery, userRole and userRoleFile from the file
		Set objFileToRead = fso.OpenTextFile(FMBMFile,1)
		iCounter = 1
		do while not objFileToRead.AtEndOfStream
			strLine = objFileToRead.ReadLine()
			strLine = Trim(strLine)
		 
			If Left(strLine, 1) =";" or Left(strLine, 1) = "#" Then
				'Yep, this string is commented!
			Else 
				Select Case iCounter
					case 1 
						shareLocation = strLine
					case 2 
						roleQuery = strLine
					case 3 
						userRoleFile = strLine
				End Select		
				iCounter= iCounter + 1
			End If
		loop
	Else 
		'If FMBM file don't exist, find shareLocation, roleQuery, userRole and userRoleFile values and enter in FMBM_User file
	
		'Find Roleshare location
		If fso.FileExists("C:\ProgramData\Launchpad\Roleshare.ini") Then
			Set objFileToRead = fso.OpenTextFile("C:\ProgramData\Launchpad\Roleshare.ini",1)
			do while not objFileToRead.AtEndOfStream
				strLine = objFileToRead.ReadLine()
				strLine = Trim(strLine)
				IF Right(strLine, 1) <> "\" Then
					strLine = strLine & "\"
				End If
				If Left(strLine, 1) =";" or Left(strLine, 1) = "#" Then
					'Yep, this string is commented!
				Else
					IF fso.FolderExists(strLine) Then
						shareLocation=strLine
					End IF
				End If
			loop
			
			Wscript.Echo "shareLocation = " & shareLocation
			
			objFileToRead.Close
			Set objFileToRead = Nothing
			Else
				Wscript.Quit
		End If
		
		roleQuery= ReadLaunchpadIniToFindRoleQuery()
		userRoleFile= ReadSSOGroups(loggedinUser, roleQuery)
	'
		'Copy role file to FMBM folder
		IF Not fso.FolderExists("C:\ProgramData\Launchpad\INIToolBackup\FMBM\") Then
			FMBMfolder=fso.CreateFolder("C:\ProgramData\Launchpad\INIToolBackup\FMBM\")
		END IF	
		
		Set a = fso.CreateTextFile(FMBMFile, true)

		a.WriteLine(shareLocation)
		a.WriteLine(roleQuery)
			a.writeLine(userRoleFile)
		a.Close
		Set a = nothing
	End If
	

	
	Call fso.CopyFile (("C:\ProgramData\Launchpad\"& userRoleFile), ("C:\ProgramData\Launchpad\INIToolBackup\FMBM\"), True)
	Set objFileToRead = fso.OpenTextFile("C:\ProgramData\Launchpad\INIToolBackup\FMBM\" & userRoleFile, 1, False, -1)
	objFileToRead.close
	set objFileToRead = nothing
	
	userRoleFileAtShare = shareLocation & loggedinUser &"_" & userRoleFile

	If fso.FileExists(userRoleFileAtShare) Then 
		Set b = fso.GetFile(userRoleFileAtShare)
		userRoleFileAtShareLM = b.DateLastModified
		Set c = fso.GetFile(userRoleFile)
		userRoleFileLM = c.DateLastModified
		If(userRoleFileAtShareLM>userRoleFileLM) Then
			Call fso.CopyFile ((userRoleFileAtShare), ("C:\ProgramData\Launchpad\" & userRoleFile), True)
			Wscript.Echo "File at share is newer, copying it to Launchpad folder and refreshing Launchpad"
			Set objFileToRead = fso.OpenTextFile("C:\ProgramData\Launchpad\" & userRoleFile, 1, False, -1)
			objFileToRead.close
			set objFileToRead = nothing
			objShell.run LPRefreshCmd
		End If
		If(userRoleFileAtShareLM<userRoleFileLM) Then 
			Call fso.CopyFile (("C:\ProgramData\Launchpad\" & userRoleFile), (userRoleFileAtShare),  True)
			Wscript.Echo "File at share is old, copying from Launchpad folder"
			Set objFileToRead = fso.OpenTextFile(userRoleFileAtShare, 1, False, -1)
			objFileToRead.close
			set objFileToRead = nothing
			objShell.run LPRefreshCmd
		End If
	Else
		Call fso.CopyFile (("C:\ProgramData\Launchpad\" & userRoleFile), (userRoleFileAtShare),  True)
		Wscript.Echo "No file at share, copying from Launchpad folder"
		Set objFileToRead = fso.OpenTextFile(userRoleFileAtShare, 1, False, -1)
		objFileToRead.close
		set objFileToRead = nothing
	End If
END SUB

Call Main()