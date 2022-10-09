//----------------------------------------------------------------------------------------------------
// EDU.cpp - Extended Disk Usage C++ - Copyright(c) 2021-2022 - Kenneth L. DeGrant II 
//----------------------------------------------------------------------------------------------------
// 
//    Author:  Ken DeGrant
//             ken.degrant@gmail.com
//    
//    This is the original version of EDU for UNIX, MS-DOS, Windows 95, and OS/2 that I wrote in 1993
//    while working in an SCO-UNIX environment.
//
//----------------------------------------------------------------------------------------------------
//
//    In short, EDU calculates disk usage by directory.
//  
//    EDU was intended to be a better form to display disk usage for a directory than the UNIX
//    command 'du'.  The UNIX 'du' displays output in the form of either 512 or 1024 byte BLOCKS
//    (depending on the version of du), Since disk space is measured in Megabytes, and people relate
//    better to the term "MEGABYTE", EDU displays its' output in megabytes.  
//    
//    If you want to find where the large directories are (disk 'hogs') try:
//
//    $ edu | sort
//  
//    This program really never had a native DOS equivalent, and I think that it is an invaluable tool
//    for most any OS environment.  So, I wrote this for MS-DOS, Windows 95, and OS/2 in addition to
//    using it under the SCO-UNIX platform I was developing under.
//  
//    If you plan on using EDU in a UNIX environment, the source file will compile using gcc.  It will
//    also compile 
//  
//    Note:  The '/' and '-' option start characters are synonymous and here for
//           various operating systems that use different ones by convention.
//  
//    Usage: 
//           edu [/total_only] [/help] [dirname]
//  
//           /total_only  
//           /t                Displays only the grand total for the directory tree.
//  
//           /help
//           /h                Displays the usage message.
//  
//           /level=1..999     Level which to display directories:
//                             ;   .     = 1
//                             ;   ./a   = 2
//                             ;   ./a/a = 3
//  
//           dirname           Path from which to start the listing.
//   
//    EDU displays output of the form:
//  
//    XXX.XX Megabytes in DIRECTORYNAME
//  
//  An example of EDU.  Wow, as of this edit Emacs was version 26.2!
//  
//  C:\> cd emacs
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
//  Building EDU using Cygwin on Windows:
//        gcc -edu.c
//  
//----------------------------------------------------------------------------------------------------
//  Revision | Date       | Comments
//----------------------------------------------------------------------------------------------------
//  1.0      | 1993       | Original
//  1.4      | 10-09-2022 | Cleaned up and compiled with Cygwin.
//----------------------------------------------------------------------------------------------------
//
#include<stdio.h>
#include<stdlib.h>
#include<ctype.h>
#include<string.h>
#include<sys/types.h>
#include<sys/stat.h>
#include<windows.h>
#include<io.h>

//
// TRUE/FALSE
//
#ifndef TRUE
#define TRUE     1
#define FALSE    0
#endif

//
// Define what a megabyte is.
//
#define MEGABYTE 1048576L

///----------------------------------------------------------------------------------------------------
///<summary>
///   Total - This is a structure representing a directory total in megabytes and bytes.
///</summary>
///----------------------------------------------------------------------------------------------------
typedef struct _Total
{
    DWORD64 qwMegabytes;
    DWORD64 qwBytes;
    
} Total;

///----------------------------------------------------------------------------------------------------
///<summary>
///   Add - Add Megabytes and bytes to an existing structure.
///</summary>
///----------------------------------------------------------------------------------------------------
void Add( DWORD64 qwMegs, DWORD64 qwBytes, Total *sNumber )
{
    //
    // Add the megabytes and bytes.
    //
    sNumber->qwMegabytes += qwMegs;
    sNumber->qwBytes     += qwBytes;

    //
    // Convert the bytes into megabytes.
    //
    while( sNumber->qwBytes > MEGABYTE )
    {
        sNumber->qwMegabytes = sNumber->qwMegabytes + 1       ;
        sNumber->qwBytes     = sNumber->qwBytes     - MEGABYTE;
    }
    
}//Add

///----------------------------------------------------------------------------------------------------
///<summary>
///   DirectoryTotal - Totalling engine, recursively called.
///</summary>
///----------------------------------------------------------------------------------------------------
Total DirectoryTotal( char *szDirectoryName, int bTotalOnly, int RecursionLevel, int RecursionLimit )
{ 
    HANDLE           hFind                                         =  0   ;
    WIN32_FIND_DATA  sFileInfo                                     = {0}  ;
    DWORD64          qwFileSize                                    =  0   ;
    BOOL             ucStatus                                      = TRUE ;
    Total            sDirTotal                                     = {0,0};
    Total            sTempTotal                                    = {0,0};
    char             szEffectivePath [MAX_PATH + FILENAME_MAX + 1] = {0}  ;
    char             szFileSpec      [MAX_PATH + FILENAME_MAX + 1] = {0}  ;
    char             szNewDir        [MAX_PATH + FILENAME_MAX + 1] = {0}  ;

    //
    // Since this is recursive, we start with current path, plus the new directory name.
    //
    sprintf( szEffectivePath, "%s\\*.*", szDirectoryName );

    //
    // Find the first file in the directory.
    //
    hFind = FindFirstFile( szEffectivePath, &sFileInfo );
    
    if( hFind == INVALID_HANDLE_VALUE )
    {
        return sDirTotal;
    }
    else
    {
        ucStatus = TRUE;
    }

    //
    //
    //
    while( ucStatus )
    {
        if( ( sFileInfo.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY ) == FILE_ATTRIBUTE_DIRECTORY )
        {
            if( ( 0 != strcmp( sFileInfo.cFileName , "." )  ) && ( 0 != strcmp( sFileInfo.cFileName, "..") ) )
            {
                sprintf( szNewDir,"%s\\%s", szDirectoryName, sFileInfo.cFileName );
                
                sTempTotal = DirectoryTotal( szNewDir, bTotalOnly, RecursionLevel + 1, RecursionLimit );
                
                Add( sTempTotal.qwMegabytes, (unsigned long) sTempTotal.qwBytes, &sDirTotal );
            }
        }
        else 
        {
            qwFileSize = ( sFileInfo.nFileSizeHigh * (MAXDWORD) ) + sFileInfo.nFileSizeLow;
            
            Add( 0, qwFileSize, &sDirTotal );
        }
        
        ucStatus = FindNextFile( hFind, &sFileInfo );
        
        sprintf( szFileSpec, "%-s\\%-s", szDirectoryName, sFileInfo.cFileName );
        
    }//while
    
    if( bTotalOnly == FALSE )
    {
        if( RecursionLevel  <= RecursionLimit )
        {
            printf( "%12.2lf Megabytes in %-s\n", 
                    (double)( (double) sDirTotal.qwMegabytes + ( (double) sDirTotal.qwBytes/ (double) MEGABYTE ) ), szDirectoryName);
        }
    }
    
    FindClose( hFind );
    
    return ( sDirTotal );
    
}//DirectoryTotal

///----------------------------------------------------------------------------------------------------
///<summary>
///   isOptionChar - Allow a / and - for option characters.
///</summary>
///----------------------------------------------------------------------------------------------------
int isOptionChar( char c )
{
    if( c == '/' || c == '-' )
    {
        return TRUE;
    }
    else
    {
        return FALSE;
    }
    
}//isOptionChar

///----------------------------------------------------------------------------------------------------
///<summary>
///   main - Program entry point.
///</summary>
///----------------------------------------------------------------------------------------------------
int main( int argc, char *argv[] )
{
    //
    // Where are we starting?  Default to current directory.
    //
    char          szPath              [MAX_PATH + FILENAME_MAX + 1] = "."  ;

    //
    // By default we show all directories, not just "totals".
    //
    unsigned char ucTotalOnly                                       = FALSE;

    //
    // This is the overall total.
    //
    Total         sOverallTotal                                     = {0}  ;

    //
    // Allow going this many levels of directory structures.
    //
    int           iRecursionLimit                                   = 999  ;

    //
    // Parse command line arguments.
    //
    while( --argc )
    {
        //
        // Help /h or /?
        //
        if( isOptionChar( argv[ argc ][ 0 ] )  && ( ( toupper( argv[ argc ][ 1 ] ) == 'H' ) || ( argv[ argc ][ 1 ] == '?' ) ) )
        {
            printf( "                                                                    \n" );
            printf( "Extended Disk Usage Version 1.4 1993-2022 Kenneth L. DeGrant II     \n" );
            printf( "                                                                    \n" );
            printf( "edu [/total_only]         Displays the overall total only.          \n" );
            printf( "    [/help]               Displays this help message.               \n" );
            printf( "    [/?]                  Displays this help message.               \n" );
            printf( "    [/level=1..999]       Level to display directories:             \n" );
            printf( "                            .     = 1                               \n" );
            printf( "                            ./a   = 2                               \n" );
            printf( "                            ./a/a = 3                               \n" );
            printf( "                            Default = all levels                    \n" );
            printf( "    [dirname]                                                       \n" );
            printf( "                                                                    \n" );
            exit(0);
        }
        //
        // Totals only mode /t.
        //
        else if( isOptionChar( argv[ argc ][ 0 ] ) && toupper( argv[ argc ][ 1 ] ) == 'T' )
        {
            ucTotalOnly = TRUE;
        }
        //
        // Setting recursion limit.
        //
        else if( isOptionChar( argv[ argc ][ 0 ] ) && toupper( argv[ argc ][ 1 ] ) == 'L' )
        {
            char *p = strchr( argv[ argc ], '=' );
            
            if( p == NULL )
            {
                iRecursionLimit = 999;
            }
            else
            {
                iRecursionLimit = atoi( p + 1 );
            }
            
            if( iRecursionLimit <= 0 || iRecursionLimit > 999 )
            {
                fprintf(stderr,"edu: Invalid directory display limit of %d.\n", iRecursionLimit );
                exit(1);
            }
        }
        else
        {
            strcpy( szPath, argv[ argc ] );
        }
        
    }//while( --argc )

    //
    // Call the totaling engine.
    //
    sOverallTotal =   DirectoryTotal( szPath, ucTotalOnly, 1, iRecursionLimit ) ;

    //
    // If totals only.
    //
    if( ucTotalOnly )
    {
        printf( "%12.2lf Megabytes\n",
                (double)((double) sOverallTotal.qwMegabytes+((double)sOverallTotal.qwBytes/(double)MEGABYTE)));
    }
    
    return 0;
    
}//main
