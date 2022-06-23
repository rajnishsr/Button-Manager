//$Id: CORP_SsoIniTool.js,v 1.3, 2012-02-01 17:36:38Z, Thompson Michael $
//This bridge simply reads the launchpad.ini file and pulls out the user's RoleQuery value,
//which the bridge then uses to pull the user's SSO A/D group from the vault.
// $LogUTC:
//  3    IT-Single_Sign_On 1.2         2011-07-26 17:36:38Z Thompson Michael
//       Updated to support both old and new code
//  2    IT-Single_Sign_On 1.1         2011-07-22 20:03:39Z Thompson Michael
//       Updated to write config.ini to both root C:\ and Auth directory to
//       support both versions of the tool.
//  1    IT-Single_Sign_On 1.0         2011-06-09 21:32:57Z Thompson Michael
//       Updated for use in Dev and QA
// $
function GetId( ) { return "$Id: CORP_SsoIniTool.js,v 1.2, 2011-07-26 17:36:38Z, Thompson Michael $"; }
// $NoKeywords$

// HCA - INFORMATION TECHNOLOGY & SERVICES, INC. 
//            PROPRIETARY SOFTWARE 
//
// THIS SOFTWARE IS PROPRIETARY AND A TRADE SECRET OF 
// HCA - INFORMATION TECHNOLOGY & SERVICES, INC. 
// ANY AND ALL RIGHTS OF OWNERSHIP, INCLUDING BUT NOT 
// LIMITED TO ANY COPYRIGHTS AND PATENT RIGHTS, BELONG TO 
// HCA - INFORMATION TECHNOLOGY & SERVICES, INC. 
// 
// THIS SOFTWARE HAS BEEN PROVIDED PURSUANT TO A LICENSE 
// AGREEMENT CONTAINING RESTRICTIONS ON ITS USE. IT MAY 
// NOT BE COPIED OR DISTRIBUTED IN ANY FORM OR MEDIUM, 
// DISCLOSED TO THIRD PARTIES, OR USED IN ANY MANNER NOT 
// PROVIDED FOR IN SAID LICENSE AGREEMENT EXCEPT WITH THE
// PRIOR AUTHORIZATION OF HCA - INFORMATION TECHNOLOGY & 
// SERVICES, INC. 
// 
// COPYRIGHT 2010 HCA - INFORMATION TECHNOLOGY & SERVICES, INC.
//
// Initial version written by Michael Thompson
//
@import ".\Sentillion Performance Framework\2.2\spf.js"
@import ".\Sentillion Performance Framework\2.2\sbmPlugin.js"
@import ".\Sentillion Performance Framework\2.2\tracePlugin.js"
@extend ".\Sentillion Performance Framework\2.2\ParseCommandLine.js"

var g_DetectOS;

function DetectOS() {

	var wbemFlagReturnImmediately = 0x10;
	var wbemFlagForwardOnly = 0x20;

	var OSVersion = "";
	
   var objWMIService = GetObject("winmgmts:\\\\.\\root\\CIMV2");
   var colItems = objWMIService.ExecQuery("SELECT * FROM Win32_OperatingSystem", "WQL",
                                          wbemFlagReturnImmediately | wbemFlagForwardOnly);

   var enumItems = new Enumerator(colItems);
   for (; !enumItems.atEnd(); enumItems.moveNext()) {
      var objItem = enumItems.item();

      BWSystem.ShowTraceMessage("Version: " + objItem.Version);
	  OSVersion = objItem.Version;
   }
   
   var sub = OSVersion.substring(0,3);
   
	if (sub >= "6.1") {
		//search specifically for Windows 7, and if we find it, 
		return true;
	}
	else {
		//this means we are assuming Win XP
		return false;
	}

}

//deprecated
function DetectIfLocationExists(path) {

		BWSystem.ShowTraceMessage("Entered DetectIfLocationExists method ");
		BWSystem.ShowTraceMessage("Entered with Path = " + path);
		FSO = new ActiveXObject("Scripting.FileSystemObject");
		
		BWSystem.ShowTraceMessage(path);
		
		if (FSO.FolderExists(path)) {
		
			BWSystem.ShowTraceMessage("Returning True!");
			return true;
		
		}
		else {
		
			BWSystem.ShowTraceMessage("Returning False!");
			return false;
		
		}

		BWSystem.ShowTraceMessage("Leaving DetectIfLocationExists method - should never really see this!");
		
}

//START WORK FOR A/D GROUP

function ReadLaunchpadIniToFindRoleQuery() {

	var strReadLine, strParameter, strRoleQuery, strWholeRoleQuery, equalsIndex, location, FSO, oFileObj;

	try {
	
		BWSystem.ShowTraceMessage("Entered ReadLaunchPadIni method ");
		BWSystem.ShowTraceMessage("g_DetectOS = " + g_DetectOS);
		if (g_DetectOS === false) {
		location = "C:\\Program Files\\Sentillion\\Vergence Authenticator\\Launchpad.ini";
		}
		else if (g_DetectOS === true) {
		location = "C:\\ProgramData\\Sentillion\\Vergence\\Launchpad.ini";
		}
		BWSystem.ShowTraceMessage("location = " + location);
		FSO = new ActiveXObject("Scripting.FileSystemObject");
		
		BWSystem.ShowTraceMessage("fso created");
		oFileObj = FSO.OpenTextFile(location, 1);
		BWSystem.ShowTraceMessage("opened text file successfully");
				
		BWSystem.ShowTraceMessage("going into loop");
		
		while (!oFileObj.AtEndOfStream)	{
		
			BWSystem.ShowTraceMessage("in loop");
			strReadLine = oFileObj.ReadLine();
			BWSystem.ShowTraceMessage("Line Read: " + strReadLine);
			
			if (strReadLine.substring(0,1) != ";" && strReadLine.substring(0,1) != ":" && strReadLine.search("=") != -1)
			{
				BWSystem.ShowTraceMessage("in 1st if");
				
				try	{
				
					BWSystem.ShowTraceMessage("try splitting");
					equalsIndex = strReadLine.search("=");
					BWSystem.ShowTraceMessage("equals index: " + equalsIndex);
					strParameter = strReadLine.substring(0, equalsIndex - 1);
					BWSystem.ShowTraceMessage("strParameter: " + strParameter);
					
					if (strParameter.toUpperCase() == "ROLEQUERY") {
					
						BWSystem.ShowTraceMessage("in 2nd if");
						strWholeRoleQuery = strReadLine.substring(equalsIndex + 1, strReadLine.length);
						strRoleQuery = strWholeRoleQuery.substring(15, strWholeRoleQuery.length);
						BWSystem.ShowTraceMessage("RoleQuery is: " + strRoleQuery);
						break;
						
					}
				}
				catch (exception) {
				
					BWSystem.ShowTraceMessage("Exception Reading Launchpad: " + exception.name + " message: " + exception.message);
					
				}
				
			}
		
		}
		BWSystem.ShowTraceMessage("trying to close object");
		oFileObj.Close();
		BWSystem.ShowTraceMessage("closed object");
		
		return strRoleQuery;
		
		
	}
	catch (exception)
	{
		BWSystem.ShowTraceMessage("Exception: " + exception.name + " message: " + exception.message);
	}
	finally {
	
	oFileObj.Close();
	
	}
}


function GetVaultADGroupStatic( ) {
	
	var vaultADGroup, roleQuery;
		
	try {
	
		roleQuery = ReadLaunchpadIniToFindRoleQuery();
		
		vaultADGroup = BWContext.GetItemValue("user.id.logon." + roleQuery);
		BWSystem.ShowTraceMessage("roleQuery and vaultADGroup are: '" + roleQuery + "' and " + vaultADGroup);
	
		return vaultADGroup;
	
	}
	catch (exception)
	{
		BWSystem.ShowTraceMessage("Exception: " + exception.name + " message: " + exception.message);
	}
	
}

function ExecuteProgram(vaultADGroup) {
	 
	try {
	
		FSO = new ActiveXObject("Scripting.FileSystemObject");
		var AppExe, AppParams;
		
		var fileLocation;
		if (g_DetectOS === false) {
		fileLocation = "C:\\Program Files\\Sentillion\\Vergence Authenticator\\INIToolBackup\\" + vaultADGroup + ".txt";
		}
		else if (g_DetectOS === true) {
		fileLocation = "C:\\ProgramData\\Sentillion\\Vergence\\INIToolBackup\\" + vaultADGroup + ".txt";
		}
		
		BWSystem.ShowTraceMessage(fileLocation);
		
		if (FSO.FileExists(fileLocation)) {
		
			BWSystem.ShowTraceMessage("file exists **********************************************");
			//current app location: \\corpdpt02\DFS\Certification\Application Builds\Stage\SSO\DEV\INIToolDev\Deploy\setup.exe
			//AppExe = "\\\\HCA\\INITool\\INITool.application";           \\\\corpdpt02\\DFS\\Certification\\Application Builds\\Stage\\SSO\\DEV\\ButtonManagerVersion2\\INITool.application
			//\\corpdpt02\\DFS\Certification\Application Builds\Stage\SSO\DEV\INIToolDev\Deploy\INITool.application
			AppExe = "\\\\corpdpt02\\DFS\\Certification\\Application Builds\\Stage\\SSO\\DEV\\ButtonManagerQA\\Deploy\\INITool.application";
			
		}
		else {
		
			BWSystem.ShowTraceMessage("FILE DOES NOT EXIST **************************************************");
			//AppExe = "\\\\corpdpt02\\DFS\\Certification\\Application Builds\\Stage\\SSO\\DEV\\INIToolDev\\Deploy\\setup.exe"             
			AppExe = "\\\\corpdpt02\\DFS\\Certification\\Application Builds\\Stage\\SSO\\DEV\\ButtonManagerQA\\Deploy\\setup.exe";
			
		}
		AppParams = "";
		
		
		BWSystem.StartApplication(AppExe, AppParams);
		
		
	}
	catch (exception)
	{
		BWSystem.ShowTraceMessage("Error in Execute Program: " + exception.name + " message: " + exception.message);
	}


}

function ProcessCommandLine( ) {

	var commandLine;

	try {

		commandLine = BWSystem.CommandLine;
		
	}
	catch (exception) {
	
		BWSystem.ShowTraceMessage("Exception getting command line: " + exception.name + " message: " + exception.message);
	
	}

	WriteLineToFile(commandLine, 8);

}

function WriteLineToFile(LineToWrite, NewOrEdit) {
	
	var fso, s, path;
		try {
		
		   var fso, s, path;
		   if (g_DetectOS === false) {
				path = "C:\\Program Files\\Sentillion\\Vergence Authenticator\\config.ini";
		   }
		   else if (g_DetectOS === true) {
				path = "C:\\ProgramData\\Sentillion\\Vergence\\config.ini";
		   }
		   fso = new ActiveXObject("Scripting.FileSystemObject");
		   s = fso.OpenTextFile(path, NewOrEdit, 1, -2);
		   s.writeline(LineToWrite);
		   
		}
		catch(exception) {
		  
		  BWSystem.ShowTraceMessage("Exception: " + exception.name + " message: " + exception.message);
		  
		}
		finally {
		
		s.Close()
		
		}

}

function HcaCommonFixUNCPathProblem( ) {

	BWSystem.ShowTraceMessage( "Entered" );
	try {
		spf.FixSlashes = function ( text ) {
			if ( text ) {
				text = text.replace( /\//g, '\\' ).replace( /(.)\\\\+/g, '$1\\' );
			}
			return text;
		};
	}
	catch ( exception ) {

		BWSystem.ShowTraceMessage( "Caught: " + exception.name + " - " + exception.message );
	}
	BWSystem.ShowTraceMessage( "Leaving" );
}

function main( ) {
	
	BWSystem.ShowTraceMessage("Entered Main");
	var vaultADGroup;
	BWSystem.ShowTraceMessage("Just Initialized vaultADGroup");
	
		try {
			BWSystem.ShowTraceMessage("Entered try, set global var");
			g_DetectOS = DetectOS();
			HcaCommonFixUNCPathProblem();
			BWSystem.ShowTraceMessage("About to try defining vaultADGroup");
			vaultADGroup = GetVaultADGroupStatic();
			BWSystem.ShowTraceMessage("Trying ExecuteProgram");
			WriteLineToFile(vaultADGroup, 2);
			BWSystem.ShowTraceMessage("About to process command line");
			ProcessCommandLine();
			BWSystem.ShowTraceMessage("Just processed command line");
			ExecuteProgram(vaultADGroup);
			BWSystem.ShowTraceMessage("Exiting Bridglet");
			BWSystem.ExitBridglet( );
			
		}
		catch ( exception ) {
			
			BWSystem.ShowTraceMessage("Exception: " + exception.name + " message: " + exception.message);
			BWSystem.ShowTraceMessage( "Caught exception: Exiting" );
			BWSystem.ExitBridglet( );
		}
}

main( );

