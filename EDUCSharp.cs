//------------------------------------------------------------------------------------------------------------------------------------------------------
// EDUCSharp.cs - Extended Disk Usage (edu) for .NET - Copyright(c) 1993-2023 - Kenneth L. DeGrant II 
//------------------------------------------------------------------------------------------------------------------------------------------------------
// 
//  Author:  Kenneth L. DeGrant II
//           ken.degrant@gmail.com
// 
//------------------------------------------------------------------------------------------------------------------------------------------------------
//  In short, EDU calculates disk usage by directory.
//------------------------------------------------------------------------------------------------------------------------------------------------------
//
//  EDU was intended to be a better form to display disk usage for a directory than the UNIX command 'du'.  The UNIX 'du' command displays output in the
//  form of either 512 or 1024 byte BLOCKS (depending on the version of du).  The newer versions of du have the "-h" (human readable) option, but that
//  mixes "K" and "M".  Since disk space 30 years later is measured in Megabytes, and people relate better to the term "MEGABYTE", EDU displays the
//  output in megabytes.  
//
//  Since the output statement is fixed (megabytes), if you want to find where the large directories are, try:
//
//     C:\> edu | sort
//
//  The original was for DOS, then UNIX (SCO), then ported to VMS, and even OS/2 (like anyone remembers OS/2).  It was once included in
//  Linux distributions.  That code was written in C with #ifdef statements for the various operating systems.  This version of EDU has been written
//  in C# for .NET and was compiled in Visual Studio starting/ending with 2019/2022.
//
//  The '/' and '-' option start characters are synonymous and here for various operating systems that use different ones by convention.
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
//         /level=1..999     Recursion level which to display directories:
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
//------------------------------------------------------------------------------------------------------------------------------------------------------
//  Revision | Date       | Comments
//------------------------------------------------------------------------------------------------------------------------------------------------------
//  1.0      | 07-xx-1993 | Original version developed for MS-DOS.
//  1.1      | 01-xx-1995 | Ported to SCO-UNIX.
//  1.2      | 01-xx-1995 | Ported to OS2 using the EMX 0.9a development system.
//  1.3      | 05-xx-1996 | Ported to Windows 95 with Microsoft V-C++ 2.0
//  1.4      | 04-02-2021 | Ported to C# and the .NET platform.
//  1.5      | 10-07-2022 | Cleaned up for publishing to GitHub in a public manner.
//  1.6      | 08-17-2022 | Cleaned up for publishing to GitHub in a public manner.
//------------------------------------------------------------------------------------------------------------------------------------------------------
//
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
//
//------------------------------------------------------------------------------------------------------------------------------------------------------
//<summary>
//   EDUCSharpNamespace - Namespace for the C# .NET version of EDU.
//</summary>
//------------------------------------------------------------------------------------------------------------------------------------------------------
namespace EDUCSharpNamespace
{
    ///------------------------------------------------------------------------------------------------------------------------------------------------------
    ///<summary>
    ///   EDU_CSharp_Class - This is the main class for the program.
    ///</summary>
    ///------------------------------------------------------------------------------------------------------------------------------------------------------
    class EDU_CSharp_Class
    {
        //
        // Record bytes the way EDU used to do.  Now with unsigned 64-bit numbers, this works even better!
        //
        public const int MEGABYTE                = 1048576;

        //
        // Default recursion limit in which to print directory levels.
        //
        public const int RECURSION_LIMIT = 999;

        //
        // Declare options variables.
        //
        public bool      bTotalOnly              = false;
        public int       iRecursionLimit         = RECURSION_LIMIT;
        public string    szStartDirectory        = ".";
        public bool      bShowErrors             = false;

        ///------------------------------------------------------------------------------------------------------------------------------------------------------
        ///<summary>
        ///   sTotal - Structure for maintaining the total number of bytes encountered.  Note that this could have been much better as a class, with the add
        ///            command inside.  I did it this way to keep with the "old" (1993) mainline code in C...  However I found a better variable type
        ///            in "ulong (64-bit)" where the old variants only handled 32-bit unsigned ints.
        ///</summary>
        ///------------------------------------------------------------------------------------------------------------------------------------------------------
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

        ///------------------------------------------------------------------------------------------------------------------------------------------------------
        ///<summary>
        ///   Add - Add megabytes and bytes to an existing sTotal structure.  I did not put this into the struct itself.  
        ///</summary>
        ///------------------------------------------------------------------------------------------------------------------------------------------------------
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

        ///------------------------------------------------------------------------------------------------------------------------------------------------------
        ///<summary>
        ///   DirectoryTotal - This is the engine for the edu command.  It is recursive.
        ///</summary>
        ///------------------------------------------------------------------------------------------------------------------------------------------------------
        sTotal DirectoryTotal( string szStartDirectory, bool bTotalOnly, int RecursionLevel, int RecursionLimit )
        { 
            sTotal sDirTotal  = new sTotal( 0 );
            sTotal sTempTotal = new sTotal( 0 );

            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //
            // Read/add all of the regular files in the starting directory, then move on to the sub-directories.
            //
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            try
            {
                //
                // Get a list of all the files in the starting directory.
                //
                List< string > lFiles = new List< string > ( Directory.EnumerateFiles( szStartDirectory ) );

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
                
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //
            // Recursively go into each subdirectory and add to the overall total.
            //
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            try
            {
                //
                // Get a list of all subdirectories in the start directory and recurse through them.
                //
                List< string > lDirectories = new List< string > ( Directory.EnumerateDirectories( szStartDirectory ) );
                
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
            
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //
            // If we are in total only mode, we don't print the total in the directory.  If it is false, then we print the individual directory total ONLY if we are
            // under the recursion level.  This allows the function to just print 1 or two levels (keeping from displaying hundreds of directories).
            //
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            if( bTotalOnly == false )
            {
                //
                // This really helps keep down the output and make it easier for the end user.
                //
                if( RecursionLevel < RecursionLimit )
                {
                    double dMegabytes = (double)( (double) sDirTotal.qwMegabytes + ( (double) sDirTotal.qwBytes / (double) MEGABYTE ) );

                    string szOutput = String.Format( "{0,12:##0.00} Megabytes in {1}\n", dMegabytes, szStartDirectory );
                    
                    Console.Write( szOutput );
                }
            }

            //
            // Return the total for this specific directory.
            //
            return ( sDirTotal );
            
        }//DirectoryTotal

        ///------------------------------------------------------------------------------------------------------------------------------------------------------
        ///<summary>
        ///   bIsOptionChar - A legacy from the past.  Is a character a command line option indicator.
        ///</summary>
        ///------------------------------------------------------------------------------------------------------------------------------------------------------
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
        
        ///------------------------------------------------------------------------------------------------------------------------------------------------------
        ///<summary>
        ///   Main - Main entry point for the program.  Note we take command line options (szArgsList).
        ///</summary>
        ///------------------------------------------------------------------------------------------------------------------------------------------------------
        static void Main( string[] szArgsList )
        {
            //
            // Allows calling functions declared in the class.
            //
            EDU_CSharp_Class P = new EDU_CSharp_Class();
            
            //
            // For each command line argument.
            //
            foreach( string szArgv in szArgsList )
            {
                //
                // If we are looking at a '/' or a '-' (option character).
                //
                if( P.bIsOptionChar( szArgv[ 0 ] ) )
                {
                    //
                    // Is the user looking for the help statement?  If so, display and then exit.
                    //
                    if( ( char.ToUpper( szArgv[ 1 ] ) == 'H' ) || ( szArgv[ 1 ] == '?' ) )
                    {
                        Console.Write( "----------------------------------------------------------------- \r\n" );
                        Console.Write( " Extended Disk Usage (edu) - Version 1.6 - Copyright(c) 1993-2023 \r\n" );
                        Console.Write( "----------------------------------------------------------------- \r\n" );
                        Console.Write( "   *** Kenneth L. DeGrant II - ken.degrant@gmail.com ***          \r\n" );
                        Console.Write( "----------------------------------------------------------------- \r\n" );
                        Console.Write( "                                                                  \r\n" );
                        Console.Write( "edu [/h] [/?] [/total] [/level=XXX] [/s] [startdir]               \r\n" );
                        Console.Write( "edu [-h] [-?] [-total] [-level=XXX] [-s] [startdir]               \r\n" );
                        Console.Write( "                                                                  \r\n" );
                        Console.Write( "[/h]         Displays this help message.                          \r\n" );
                        Console.Write( "[/?]         Displays this help message.                          \r\n" );
                        Console.Write( "[/total]     Displays the overall total only.                     \r\n" );
                        Console.Write( "[/level=XXX] Level to display directories:                        \r\n" );
                        Console.Write( "               .       = 1                                        \r\n" );
                        Console.Write( "               ./a     = 2                                        \r\n" );
                        Console.Write( "               ./a/b   = 3                                        \r\n" );
                        Console.Write( "               ./a/b/c = 4                                        \r\n" );
                        Console.Write( "[/s]         Show error messages.                                 \r\n" ); 
                        Console.Write( "[startdir]   What directory to start with.  Default is \".\".     \r\n" );
                        Console.Write( "                                                                  \r\n" );

                        //
                        // Display help will exit the application.
                        //
                        Environment.Exit( 0 );
                        
                    }// if user wants to display the help message.
                    
                    //
                    // Only print the overall total.
                    //
                    else if( Char.ToUpper( szArgv[ 1 ] ) == 'T' )
                    {
                        P.bTotalOnly = true;
                        
                        //
                        // Go to next argument.
                        //
                        continue;
                        
                    }// if user wants to display the total only.
                    
                    //
                    // Show errors.
                    //
                    else if( Char.ToUpper( szArgv[ 1 ] ) == 'S' ) 
                    {
                        P.bShowErrors = true;
                        
                        //
                        // Go to next argument.
                        //
                        continue;
                        
                    }// if user wants to show errors.
                    
                    //
                    // To what level will we print directory levels.  /L=XXXX
                    //
                    else if( Char.ToUpper( szArgv[ 1 ] ) == 'L' ) 
                    {
                        //
                        // Find the '=' character.
                        //
                        int iIndexOfEqualSign = szArgv.IndexOf( '=' );
                        
                        //
                        // If the '=' character was not found, default to RECURSION_LIMIT.
                        //
                        if( -1 == iIndexOfEqualSign )
                        {
                            P.iRecursionLimit = RECURSION_LIMIT;
                            
                            Console.Write( String.Format( "edu: Invalid level statement [/l=XXX].  No '=' found, defaulting to {0}.\r\n", RECURSION_LIMIT ) );

                            //
                            // Go to next argument.
                            //
                            continue;
                            
                        }//if no equal sign
                        
                        //
                        // We have a '='. Convert the number behind it.  An exception means there was an error, so default to RECURSION_LIMIT.
                        //
                        try
                        { 
                            P.iRecursionLimit = Convert.ToInt32( szArgv.Substring( iIndexOfEqualSign + 1 ) );
                        }
                        catch
                        {
                            P.iRecursionLimit = RECURSION_LIMIT;
                            
                            Console.Write( String.Format( "edu: Invalid level statement [/l=XXX].  Error converting number, defaulting to {0}.\r\n", RECURSION_LIMIT ) );
                            
                            //
                            // Go to next argument.
                            //
                            continue;
                            
                        }//catch on converting number
                        
                        //
                        // If recursion set was less than or equal to zero.  
                        //
                        if( P.iRecursionLimit <= 0 ) 
                        {
                            P.iRecursionLimit = RECURSION_LIMIT;
                            
                            Console.Write( String.Format( "edu: Invalid level statement [/l=XXX].  Level cannot be less than zero, defaulting to {0}.\r\n", RECURSION_LIMIT ) );
                            
                            //
                            // Go to next argument.
                            //
                            continue;
                            
                        }// if recursion was less than zero
                        
                        //
                        // If recursion set was greater than recursion limit.
                        //
                        if( P.iRecursionLimit > RECURSION_LIMIT ) 
                        {
                            P.iRecursionLimit = RECURSION_LIMIT;
                            
                            Console.Write( String.Format( "edu: Invalid level statement [/l=XXX].  Level cannot be less than zero, defaulting to {0}.\r\n", RECURSION_LIMIT ) );
                            
                            //
                            // Go to next argument.
                            //
                            continue;
                            
                        }// if recursion was less than zero
                        
                    }// Level=X
                    
                }//if an option character
                
                //
                // The only option without an option character is the starting directory name.
                //
                else
                {
                    P.szStartDirectory = szArgv;
                }
                
            }// foreach command line argument
            
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //
            // Call the recursive routine to get and print directory totals.  Then print overall total.
            //
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

            sTotal sTotalBytes = P.DirectoryTotal( P.szStartDirectory, P.bTotalOnly, 0, P.iRecursionLimit );

            //
            // Nothing is printed by DirectoryTotal in "TotalsOnly" mode, so we have to print the final total.
            //
            if( P.bTotalOnly )
            { 
                double dMegabytes = (double)( (double) sTotalBytes.qwMegabytes + ( (double) sTotalBytes.qwBytes / (double) MEGABYTE ) );
                    
                string szOutput = String.Format( "{0,12:##0.00} Megabytes in {1}\n", dMegabytes, P.szStartDirectory );
                    
                Console.Write( szOutput );
            }
            
        }// Main
        
    }// class EDU_CSharp_Class
    
}//namespace EDUCSharpNamespace
