//----------------------------------------------------------------------------------------------------
// EDUCSharp.cs - Extended Disk Usage (edu) for .NET - Copyright(c) 1993-2022 - Kenneth L. DeGrant II 
//----------------------------------------------------------------------------------------------------
// 
//  Author:  Kenneth L. DeGrant II
//           ken.degrant@gmail.com
// 
//----------------------------------------------------------------------------------------------------
//  In short, EDU calculates disk usage by directory.
//
//  EDU was intended to be a better form to display disk usage for a directory than the UNIX command
//  'du'.  The UNIX 'du' command displays output in the form of either 512 or 1024 byte BLOCKS
// (depending on the version of du), Since disk space is measured in Megabytes, and people relate
//  better to the term "MEGABYTE", EDU displays the output in megabytes.  
//
//  If you want to find where the large directories are (disk 'hogs') try:
//
//     C:\> edu | sort
//
//  There are UNIX, VMS, and even OS/2 versions of this.  It was once included in Linux distributions.
//  That code was written in C with #ifdef statements for the various operating systems.  This version
//  of EDU has been written in C# for .NET and was compiled in Visual Studio 2019.
//
//  Note:  The '/' and '-' option start characters are synonymous and here for various operating
//         systems that use different ones by convention.
//
//  Usage: 
//         edu [/total_only] [/help] [dirname]
//
//         /total_only  
//         /t                Displays only the grand total for the directory tree.
//
//         /help
//         /h                Displays the usage message.
//
//         /suppress 
//         /s                Suppress error messages about not being able to enter a directory.
//
//         /level=1..999     Level which to display directories:
//                           ;   .     = 1
//                           ;   ./a   = 2
//                           ;   ./a/a = 3
//
//         dirname           Path from which to start the listing.
//
//  EDU displays output of the form:
//
//  XXX.XX Megabytes in DIRECTORYNAME
//
//  An example of EDU.
//
//  C:\emacs> edu
//    3.20 Megabytes in .\19.27\bin
//    0.85 Megabytes in .\19.27\data
//    0.21 Megabytes in .\19.27\emx
//    2.18 Megabytes in .\19.27\etc
//    0.02 Megabytes in .\19.27\lib-src\os2
//    0.40 Megabytes in .\19.27\lib-src
//    0.20 Megabytes in .\19.27\lisp\term
//   11.54 Megabytes in .\19.27\lisp
//    1.88 Megabytes in .\19.27\man
//    0.00 Megabytes in .\19.27\src\m
//    0.00 Megabytes in .\19.27\src\s
//    3.41 Megabytes in .\19.27\src
//   23.74 Megabytes in .\19.27
//   23.74 Megabytes in .
//
//----------------------------------------------------------------------------------------------------
//  Revision | Date       | Comments
//----------------------------------------------------------------------------------------------------
//  1.0      | 07/xx/1993 | Original version developed for MS-DOS.
//  1.1      | 01/xx/1995 | Ported to SCO-UNIX.
//  1.2      | 01/xx/1995 | Ported to OS2 using the EMX 0.9a development system.
//  1.3      | 05/xx/1996 | Ported to Windows 95 with Microsoft V-C++ 2.0
//  1.4      | 04/02/2021 | Ported to C# and the .NET platform.
//  1.5      | 10/07/2022 | Cleaned up for publishing to GitHub in a public manner.
//----------------------------------------------------------------------------------------------------
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
//----------------------------------------------------------------------------------------------------
//<summary>
//   EDUCSharpNamespace - Namespace for the C# .NET version of EDU.
//</summary>
//----------------------------------------------------------------------------------------------------
namespace EDUCSharpNamespace
{
    ///----------------------------------------------------------------------------------------------------
    ///<summary>
    ///   EDU_CSharp_Class - This is the main class for the program.
    ///</summary>
    ///----------------------------------------------------------------------------------------------------
    class EDU_CSharp_Class
    {
        //
        // Record bytes the way EDU used to do.  Now with unsigned 64-bit numbers, this works even better!
        //
        public const int MEGABYTE                = 1048576;

        //
        // Default recursion limit in which to print directory levels.
        //
        public const int RECUSRION_LIMIT_DEFAULT = 999;

        //
        // Declare options variables.
        //
        public bool      bTotalOnly              = false;
        public int       iRecursionLimit         = RECUSRION_LIMIT_DEFAULT;
        public string    szStartDirectory        = ".";
        public bool      bShowErrors             = false;

        ///----------------------------------------------------------------------------------------------------
        ///<summary>
        ///   sTotal - Structure for maintaining the total number of bytes encountered.  Note that this could
        ///            have been much better as a class, with the add command inside.  I did it this way to
        ///            keep with the "old" (1993) mainline code in C...  However I found a better variable
        ///            type in "ulong (64-bit)" where the old variants only handled 32-bit unsigned ints.
        ///</summary>
        ///----------------------------------------------------------------------------------------------------
        public struct sTotal
        {
            //
            // Unsigned 64-bit numbers representing megabytes and bytes.
            //
            public ulong qwMegabytes ;
            public ulong qwBytes     ;

            //
            // Structure initialization function.
            //
            public sTotal( int iUnused = 0 )
            {
                qwMegabytes = (ulong) 0;
                qwBytes     = (ulong) 0;
                
            }//sTotal
            
        }// public struct Total

        ///----------------------------------------------------------------------------------------------------
        ///<summary>
        ///   Add - Add megabytes and bytes to an existing sTotal structure.  I did not put this into the
        ///         struct itself.  
        ///</summary>
        ///----------------------------------------------------------------------------------------------------
        public void Add( ulong qwMegs, ulong qwBytes, ref sTotal sNumber )
        {
            //
            // Add the number of megabytes and bytes to the number.
            //
            sNumber.qwMegabytes += qwMegs;
            sNumber.qwBytes     += qwBytes;

            //
            // Increase the number of megabytes based on the number of bytes.
            //
            while( sNumber.qwBytes > MEGABYTE )
            {
                sNumber.qwMegabytes = sNumber.qwMegabytes + 1       ;
                sNumber.qwBytes     = sNumber.qwBytes     - MEGABYTE;
            }
            
        }//Add

        ///----------------------------------------------------------------------------------------------------
        ///<summary>
        ///   DirectoryTotal - This is the engine for the edu command.  It is recursive.
        ///</summary>
        ///----------------------------------------------------------------------------------------------------
        sTotal DirectoryTotal( string szStartDirectory, bool bTotalOnly, int RecursionLevel, int RecursionLimit )
        { 
            sTotal sDirTotal  = new sTotal(0);
            sTotal sTempTotal = new sTotal(0);

            //****************************************************************************************************
            //****************************************************************************************************
            //****************************************************************************************************
            //
            // Read/add all of the regular files in the starting directory, then move on to the sub-directories.
            //
            //****************************************************************************************************
            //****************************************************************************************************
            //****************************************************************************************************
            try
            {
                //
                // Get a list of all the files in the starting directory.
                //
                List<string> lFiles = new List<string> ( Directory.EnumerateFiles( szStartDirectory ) );

                //
                // For each file, add to the directory total.
                //
                foreach( string szCurrentFile in lFiles )  
                {
                    //
                    // Get the file information for the file found.
                    //
                    FileInfo fiFileInfo = new FileInfo( szCurrentFile );  
                    
                    //
                    // Add the file length in bytes to the directory total.
                    //
                    Add( (ulong) 0, (ulong) fiFileInfo.Length, ref sDirTotal );
                }
            }
            catch( Exception ex )
            {
                //
                // Error
                //
                if( bShowErrors )
                {
                    Console.Write( String.Format( "edu: Error getting files list in [{0}].  OS Message was [{1}].\r\n", szStartDirectory, ex.Message ) );
                }
            }
                
            //****************************************************************************************************
            //****************************************************************************************************
            //****************************************************************************************************
            //
            // Recursively go into each subdirectory and add to the overall total.
            //
            //****************************************************************************************************
            //****************************************************************************************************
            //****************************************************************************************************
            try
            {
                //
                // Get a list of all subdirectories in the start directory and recurse through them.
                //
                List<string> lDirectories = new List<string> ( Directory.EnumerateDirectories( szStartDirectory ) );
                
                //
                // For each directory, recurse.  Then add to the directory total.
                //
                foreach( string szDirectoryFile in lDirectories )  
                {  
                    sTempTotal = DirectoryTotal( szDirectoryFile, bTotalOnly, RecursionLevel + 1, RecursionLimit );
                    
                    //
                    // Add the file length in megabytes and bytes to the directory total.
                    //
                    Add( (ulong) sTempTotal.qwMegabytes, (ulong) sTempTotal.qwBytes, ref sDirTotal );
                }

            }
            catch( Exception ex )
            {
                //
                // Error
                //
                if( bShowErrors )
                {
                    Console.Write( String.Format( "edu: Error getting directory list in [{0}].  OS Message was [{1}].\r\n", szStartDirectory, ex.Message ) );
                }
            }
            
            //****************************************************************************************************
            //****************************************************************************************************
            //****************************************************************************************************
            //
            // If we are in total only mode, we don't print the total in the directory.  If it is false, then
            // we print the individual directory total ONLY if we are under the recursion level.  This allows
            // the function to just print 1 or two levels (keeping from displaying hundreds of directories).
            //
            //****************************************************************************************************
            //****************************************************************************************************
            //****************************************************************************************************
            if( bTotalOnly == false )
            {
                //
                // This really helps keep down the output and make it easier for the end user.
                //
                if( RecursionLevel < RecursionLimit )
                {
                    double dMegabytes = (double)( (double) sDirTotal.qwMegabytes + ( (double) sDirTotal.qwBytes / (double) MEGABYTE ) );

                    string szOutput = String.Format( "{0,12:##0.00} Megabytes in {1}.\n", dMegabytes, szStartDirectory );
                    
                    Console.Write( szOutput );
                }
            }

            //
            // Return the total for this specific directory.
            //
            return ( sDirTotal );
            
        }//DirectoryTotal

        ///----------------------------------------------------------------------------------------------------
        ///<summary>
        ///   bIsOptionChar - A legacy from the past.  Is a character a command line option indicator.
        ///</summary>
        ///----------------------------------------------------------------------------------------------------
        public bool bIsOptionChar( char c )
        {
            if( ( c == '/' ) || ( c == '-' ) )
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }//bIsOptionChar
        
        ///----------------------------------------------------------------------------------------------------
        ///<summary>
        ///   Main - Main entry point for the program.  Note we take command line options (szArgsList).
        ///</summary>
        ///----------------------------------------------------------------------------------------------------
        static void Main( string[] szArgsList )
        {
            //
            // Allows calling functions in the class.
            //
            EDU_CSharp_Class P = new EDU_CSharp_Class();
            
            //
            // For each command line argument.
            //
            foreach( string szArgv in szArgsList )
            {
                //
                // Help screen.
                //
                if( P.bIsOptionChar( szArgv[0] ) && ( ( char.ToUpper( szArgv[1] ) == 'H' ) || ( szArgv[1] == '?' ) ) )
                {
                    string szOut = "";
                    
                    szOut += String.Format( "-------------------------------------------------------------------------------------------\r\n" );
                    szOut += String.Format( "  Extended Disk Usage (edu) Version 1.1 - Copyright(c) 1993-2022 - Kenneth L. DeGrant II   \r\n" );
                    szOut += String.Format( "-------------------------------------------------------------------------------------------\r\n" );
                    szOut += String.Format( "                                                                                           \r\n" );
                    szOut += String.Format( "  Kenneth L. DeGrant II - ken.degrant@gmail.com                                            \r\n" );
                    szOut += String.Format( "                                                                                           \r\n" );
                    szOut += String.Format( "  edu [/total_only]         Displays the overall total only.                               \r\n" );
                    szOut += String.Format( "      [/help]               Displays this help message.                                    \r\n" );
                    szOut += String.Format( "      [/?]                  Displays this help message.                                    \r\n" );
                    szOut += String.Format( "      [/level=XXX]          Level to display directories:                                  \r\n" );
                    szOut += String.Format( "                              .       = 1                                                  \r\n" );
                    szOut += String.Format( "                              ./a     = 2                                                  \r\n" );
                    szOut += String.Format( "                              ./a/b   = 3                                                  \r\n" );
                    szOut += String.Format( "                              ./a/b/c = 4                                                  \r\n" );
                    szOut += String.Format( "                              Default = 999                                                \r\n" );
                    szOut += String.Format( "                                                                                           \r\n" );
                    szOut += String.Format( "      [/showerrors]         Show error messages when not being able to enter a directory.  \r\n" ); 
                    szOut += String.Format( "                                                                                           \r\n" );
                    szOut += String.Format( "      [dirname]                                                                            \r\n" );
                    szOut += String.Format( "                                                                                           \r\n" );

                    Console.WriteLine( szOut );
                    
                    Environment.Exit( 0 );
                    
                }// if /h or /?
                
                //
                // Only print the overall total.
                //
                else if( P.bIsOptionChar( szArgv[0] ) && ( Char.ToUpper( szArgv[1] ) == 'T' ) )
                {
                    P.bTotalOnly = true;
                    
                }// if Total_Only
                
                //
                // Show errors.
                //
                else if( P.bIsOptionChar( szArgv[0] ) && ( Char.ToUpper( szArgv[1] ) == 'S' ) )
                {
                    P.bShowErrors = true;
                    
                }// if Total_Only
                
                //
                // To what level will we print directory levels.  /L=XXXX
                //
                else if( P.bIsOptionChar( szArgv[0] ) && ( Char.ToUpper( szArgv[1] ) == 'L' ) )
                {
                    int iIndexOfEqualSign = szArgv.IndexOf( '=' );

                    //
                    // If the = was not found, default to RECUSRION_LIMIT_DEFAULT.
                    //
                    if( -1 == iIndexOfEqualSign )
                    {
                        P.iRecursionLimit = RECUSRION_LIMIT_DEFAULT;

                        Console.Write( "edu: Invalid level statement (no equals sign).  Setting recursion level to default.\r\n" );
                    }

                    //
                    // We have an equal sign, try to convert the number behind it.  An exception
                    // means there was an error parsing the number, so default to RECUSRION_LIMIT_DEFAULT.
                    //
                    try
                    { 
                        P.iRecursionLimit = Convert.ToInt32( szArgv.Substring( iIndexOfEqualSign + 1 ) );
                    }
                    catch
                    {
                        P.iRecursionLimit = RECUSRION_LIMIT_DEFAULT;
                        
                        Console.Write( "edu: Invalid level statement (bad number conversion).  Setting recursion level to default.\r\n" );
                    }

                    //
                    // If less than 0.  
                    //
                    if( P.iRecursionLimit <= 0 ) 
                    {
                        P.iRecursionLimit = RECUSRION_LIMIT_DEFAULT;

                        Console.Write( "edu: Invalid level statement (recursion level less than zero).  Setting recursion level to default.\r\n" );
                    }
                    
                }// Level=X
                
                //
                // The only option without an option character is the starting directory name.
                //
                else
                {
                    P.szStartDirectory = szArgv;
                }
                
            }// foreach command line argument

            //****************************************************************************************************
            //****************************************************************************************************
            //****************************************************************************************************
            //
            // Call the recursive routine to get and print directory totals.  Then print overall total.
            //
            //****************************************************************************************************
            //****************************************************************************************************
            //****************************************************************************************************

            sTotal sTotalBytes = P.DirectoryTotal( P.szStartDirectory, P.bTotalOnly, 0, P.iRecursionLimit );

            //
            // Nothing is printed by DirectoryTotal in "TotalsOnly" mode, so we have to print the final total.
            //
            if( P.bTotalOnly )
            { 
                double dMegabytes = (double)( (double) sTotalBytes.qwMegabytes + ( (double) sTotalBytes.qwBytes / (double) MEGABYTE ) );
                    
                string szOutput = String.Format( "{0,12:##0.00} Megabytes in {1}.\n", dMegabytes, P.szStartDirectory );
                    
                Console.Write( szOutput );
            }
            
        }// Main
        
    }// class EDU_CSharp_Class
    
}//namespace EDUCSharpNamespace
