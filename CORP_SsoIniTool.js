// $Id: CORP_SsoIniTool.js,v 1.2, 2011-07-26 17:36:38Z, Thompson Michael $
// This bridge simply reads the launchpad.ini file and pulls out the user's RoleQuery value,
// which the bridge then uses to pull the user's SSO A/D group from the vault.
// $LogUTC:
//	4    IT-Single_Sign_On 1.3		   2016-12-9  08:05:00  Duke Ethan
//		 Updated to use "Authenticator.exe /refresh" command to update Launchpad 
//       if Vergence Client version is > 6.3.
//  3    IT-Single_Sign_On 1.2         2011-07-26 17:36:38Z Thompson Michael
//       Updated to support both old and new code
//  2    IT-Single_Sign_On 1.1         2011-07-22 20:03:39Z Thompson Michael
//       Updated to write config.ini to both root C:\ and Auth directory to
//       support both versions of the tool.
//  1    IT-Single_Sign_On 1.0         2011-06-09 21:32:57Z Thompson Michael
//       Updated for use in Dev and QA
// $
function GetId( ) { return "$Id: CORP_SsoIniTool.js, v1.3, 2016-12-5, Ethan Duke $"; }
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
//Scott Bailey's trace function
function trace( message, traceCaller ) {
	
	var bridgeName;
	
	try {
		
		if ( !traceCaller ) {
			
			traceCaller = arguments.callee.caller;
		}
		bridgeName = BWSystem.BridgeName;
		
		BWSystem.ShowTraceMessage( "[" + bridgeName + "] " + GetFunctionName( traceCaller ) + "-->" + message );
	}
	catch ( exception ) {
		
		// ignore
	}
}


function HcaCommonFixUNCPathProblem( ) {

	trace( "Entered" );
	try {
		spf.FixSlashes = function ( text ) {
			if ( text ) {
				text = text.replace( /\//g, '\\' ).replace( /(.)\\\\+/g, '$1\\' );
			}
			return text;
		};
	}
	catch ( exception ) {

		trace( "Caught: " + exception.name + " - " + exception.message );
	}
	trace( "Leaving" );
}

function errorMessageBox(msg, foreground)
{
	if ( undefined == foreground )
	{
		foreground = false;
		 }
	BWSystem.ModalMessageBox(0,
							 msg,
							 BWSystem.BridgeName,
							 sbmPlugin.MessageType.Exclamation, 
							 sbmPlugin.ButtonType.Ok, 
							 foreground);
}

function main( ) {

	trace("Entered Main");
	try 
	{
			HcaCommonFixUNCPathProblem();
			var objShell = new ActiveXObject("Shell.Application");
			var commandLine_orig = BWSystem.CommandLine;
			AppExe = "\\\\hca\\initool\\BM_051519\\ButtonManager.exe";
			BWSystem.StartApplication(AppExe, commandLine_orig);
			trace("Ready to exit");
			BWSystem.ExitBridglet( );
			trace("Successfull exit");
		}
		catch ( exception ) {
			
			trace("Exception: " + exception.name + " message: " + exception.message);
			trace( "Exiting" );
			BWSystem.ExitBridglet( );
		}
	}


main( );

