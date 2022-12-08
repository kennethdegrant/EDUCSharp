/***************************************************************************

  EDU - Extended Disk Usage                                 ( A better 'du' )

  Author: Kenneth DeGrant 
          ken.degrant@gmail.com

  In short, EDU calculates disk usage by directory.

  EDU was intended to be a better form to display disk usage for a directory 
  than the UNIX command ``du''.  The UNIX 'du' displays output in the form 
  of either 512 or 1024 byte BLOCKS (depending on the version of du), Since 
  disk space is measured in Megabytes, and people relate better to the term 
  "MEGABYTE", EDU displays its' output in megabytes.  
  
  If you want to find where the large directories are (disk 'hogs') try:
                           edu | sort

  This program really never had a native DOS equivalent, and I think that
  it is an invaluable tool for most any environment.

DOS Users:
  Copy EDUDOS.EXE executable file to a place in your PATH statement as 
  EDU.EXE or whatever you wish to call it.  

WINDOWS 95 Users:
  Copy EDU95.EXE executable file to a place in your PATH statement as 
  EDU.EXE or whatever you wish to call it.  

OS/2 Users:
  Copy EDUOS2.EXE executable file to a place in your PATH statement as 
  EDU.EXE or whatever you wish to call it.  

UNIX Users:
  If you plan on using EDU in a UNIX environment, the source file will 
  compile using gcc.  As far as other compilers, I have tested only SCO-UNIX
  cc (Microsoft) on both versions 3 and 5.  Linux with gcc also works.
  Gcc on SUN also compiles nicely.

  This program may be freely modified and distributed under the condition 
  that the author be notified of changes made, and the following files are 
  distributed with it:
        README
        eduos2.exe
        edu95.exe
        edudos.exe
        edu.c
  This way, your modifications will be incorporated into future versions 
  of EDU.

  If you like EDU, you are encouraged to Register EDU for a cost of 2 minutes.
  In this period, you should send a quick E-mail message stating that you find it 
  useful.  You should also include your E-mail address, so that I can notify 
  you of changes to and ports of the program to different operating systems, 
  and windowing environments.  I will mail notices of new version changes 
  when they happen.

  Note:  The '/' and '-' option start characters are synonymous and here for
         various operating systems that use different ones by convention.

  Usage: 
         edu [/total_only] [/help] [dirname]

         /total_only  
         /t                Displays only the grand total for the directory tree.

         /help
         /h                Displays the usage message.

         /level=1..999     Level which to display directories:
                           ;   .     = 1
                           ;   ./a   = 2
                           ;   ./a/a = 3

         dirname           Path from which to start the listing.
 
  EDU displays output of the form:

  XXX.XX Megabytes in DIRECTORYNAME

An example of EDU.

C:\> cd emacs
C:\emacs> edu
  3.20 Megabytes in .\19.27\bin
  0.85 Megabytes in .\19.27\data
  0.21 Megabytes in .\19.27\emx
  2.18 Megabytes in .\19.27\etc
  0.02 Megabytes in .\19.27\lib-src\os2
  0.40 Megabytes in .\19.27\lib-src
  0.20 Megabytes in .\19.27\lisp\term
 11.54 Megabytes in .\19.27\lisp
  1.88 Megabytes in .\19.27\man
  0.00 Megabytes in .\19.27\src\m
  0.00 Megabytes in .\19.27\src\s
  3.41 Megabytes in .\19.27\src
 23.74 Megabytes in .\19.27
 23.74 Megabytes in .
  
Building EDU from source on a UNIX machine:

  You must define UNIX when compiling.  For example:
      cc -DUNIX edu.c

Building EDU from source on a non-UNIX machine:

        Windows 95 with Microsoft Visual C++, you must define WIN95:
                cl -DWIN95 edu.c

        Otherwise:
                Compile normally.


History:

  Date  Author                    Change
  7-93  Kenneth DeGrant           Original version developed for MS-DOS.
  1-95      "                     Ported to SCO-UNIX.
  1-95      "                     Ported to OS2 using the EMX 0.9a development
                                  system.
  5-96      "                     Ported to Windows 95 with Microsoft V-C++ 2.0
  
Ported to Platforms:
      
  Platform       Compiler
  --------       -----------------------------
  Windows 95     Microsoft Visual C++ 2.0
  MS-DOS         Borland 4.01
  OS2/EMX        EMX GCC
  SCO-UNIX       CC (Microsoft) and GCC
  Linux          GCC

Future:

  Plans to port to Microsoft Windows Environment, and an X11 environment.
  
Wanted:

  A VMS port. (Behind Unix, my second favorite OS.)

Contributors:
  Name              Address       Change Made

******************************************************************************************/
#define UNIX

#include<stdio.h>
#include<stdlib.h>
#include<ctype.h>
#include<string.h>
#include<sys/types.h>
#include<sys/stat.h>

#ifdef UNIX
#include<dirent.h>
#endif

#ifndef _MAX_FNAME
#define _MAX_FNAME 512
#endif

#ifdef WIN95
#include<direct.h>
#include<io.h>
#endif

#define MEGABYTE              1048576L
#ifndef TRUE
#define TRUE                  1
#define FALSE                 0
#endif

#ifndef MAXPATHLEN
#define MAXPATHLEN 257
#endif

/* Keep total by Megabyte.Byte */

typedef struct total{
                         unsigned long  Megabytes;
                         unsigned long  Bytes;

                    } Total;


/*
        Given megabytes and bytes, add them to the Total number.
*/

void Add( 
         unsigned long megs,           /* pass        */
         unsigned long bytes,          /* pass        */
         Total *number                 /* pass/return */ 
        )
{
   number->Bytes += bytes;
   number->Megabytes += megs;

   while( number->Bytes > MEGABYTE )
   {
      number->Megabytes++;
      number->Bytes -= MEGABYTE;
   }
}

/*
  This is the main totalling engine.  This recursive procedure takes
  a directory name, and will total all directories recursively. 

  A lot of hacking had to be done for the Windows 95 environment with
  Visual C++.  They just don't want you to be able to port a program
  from some other environment.  I think people call it.... Proprietary.

*/
   
Total DirectoryTotal( char *dirname, int total_only, char PathDelimiter, int RecursionLevel, int RecursionLimit )
{

#ifdef UNIX 

   DIR *mydir;
   struct dirent *fbuf;
   struct stat statbuf;

#else                                                   /* Windows 95 */

  long   SearchHandle;
  int    Status;
  char   EffectivePath[_MAX_FNAME];
  struct _finddata_t FileInfo;

#endif

   char filespec[MAXPATHLEN];
   char newdir[MAXPATHLEN];

   Total DirTotal = {0,0};
   Total TempTotal = {0,0};

#ifdef UNIX  /* UNIX, DOS, or OS/2 */

   if( NULL == ( mydir = opendir( dirname ) ) )
   {
      fprintf(stderr,"Unable to open directory: %s\n", dirname );
      perror("opendir:");
      return (DirTotal);
   }

   while( NULL != ( fbuf = readdir(mydir) ) )
   {
      sprintf(filespec,"%-s%c%-s", dirname, PathDelimiter, fbuf->d_name);

          /* UNIX supports file `links', do not follow directory links. */

#ifdef UNIX
      if ( lstat( filespec, &statbuf) == -1 ) 
      {
         continue;
      }
      if( (statbuf.st_mode & S_IFMT) == S_IFLNK  )
      {
         continue;
      }
#else /* DOS or OS/2 */
      if ( stat( filespec, &statbuf) == -1 ) 
      {
         continue;
      }
#endif

      if( (statbuf.st_mode & S_IFMT) == S_IFDIR  )
      {
         if( fbuf->d_name[0] == '.' ) 
         {
            /* "." and ".." are directory names, do not follow them! */
            
            if( (fbuf->d_name[1] == 0) || (fbuf->d_name[1] == '.') )
               continue;
         }

         sprintf(newdir,"%s%c%s", dirname, PathDelimiter, fbuf->d_name );

         TempTotal = DirectoryTotal( newdir, total_only, PathDelimiter, RecursionLevel + 1 ,RecursionLimit);

         Add( TempTotal.Megabytes, (unsigned long ) TempTotal.Bytes, &DirTotal );
      }
      else 
      {
         Add( 0, (unsigned long) statbuf.st_size, &DirTotal );
      }
   }

#else /* Windows 95 Specific, Damn you Microsoft! */

  sprintf( EffectivePath, "%s\\*.*", dirname );

  SearchHandle = _findfirst( EffectivePath, &FileInfo );
   
  Status = SearchHandle;

  while( Status != -1 )
    {
      if( FileInfo.attrib == _A_SUBDIR )
        {
          if( 0 != strcmp(FileInfo.name, ".") && 0 != strcmp(FileInfo.name, "..") )
            {
              sprintf(newdir,"%s%c%s", dirname, PathDelimiter, FileInfo.name );
              
              TempTotal = DirectoryTotal( newdir, total_only, PathDelimiter, RecursionLevel + 1, RecursionLimit );
              
              Add( TempTotal.Megabytes, (unsigned long ) TempTotal.Bytes, &DirTotal );
            }
        }
      else 
        {
          Add( 0, (unsigned long) FileInfo.size, &DirTotal );
        }

      Status = _findnext( SearchHandle, &FileInfo );

      sprintf(filespec,"%-s%c%-s", dirname, PathDelimiter, FileInfo.name);
      
    }/*while*/

#endif /* WIN95; UNIX, DOS or OS/2 */

  if( total_only == FALSE )
  {
     if( RecursionLevel  <= RecursionLimit )
     printf( "%6.2lf Megabytes in %-s\n", 
         (double)((double)DirTotal.Megabytes + ((double)DirTotal.Bytes/(double)MEGABYTE)), dirname);
  }

#ifndef WIN95                                   /* UNIX, DOS or OS/2 */
   closedir( mydir );
#else                                                   /* Windows 95 */
  _findclose( SearchHandle );
#endif

   return ( DirTotal );
}

/*
  Return TRUE if a character is an option character, FALSE otherwise.
*/

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
}


int main( int argc, char *argv[] )
{
   char path[MAXPATHLEN] = ".";
   int total_only;
   Total OverallTotal;
   char PathDelimiter;
   int  RecursionLimit;

#ifdef UNIX
      PathDelimiter  =  '/';
#else
      PathDelimiter  = '\\';
#endif

   RecursionLimit = 999;   /* Displays information for . (1) and the immediate subdirs ./a (2) */

   total_only = FALSE;

   while( --argc )
   {
      if( isOptionChar(argv[argc][0])  &&  toupper( argv[argc][1] ) == 'H' )
      {
         puts( "\nExtended Disk Usage 1.2  1993-96 Kenneth DeGrant\n\n"
               "edu [/total_only]         ; Displays the overall total only\n"
               "    [/help]               ; Displays this help message\n"
               "    [/level=1..999]       ; Level to display directories:\n"
               "                          ;   .     = 1\n"
               "                          ;   ./a   = 2\n"
               "                          ;   ./a/a = 3\n"
               "                          ; Default = 999 or all\n"
               "    [dirname]                                 \n");
         exit(0);
      }
      else if( isOptionChar(argv[argc][0]) && toupper( argv[argc][1] ) == 'T' )
      {
         total_only = TRUE;
      }
      else if( isOptionChar(argv[argc][0]) && toupper( argv[argc][1] ) == 'L' )
      {
          char *p = strchr( argv[argc], '=' );
          if( p == NULL )
          {
             RecursionLimit = 999;
          }
          else
          {
             RecursionLimit = atoi( p + 1);
          }

          if( RecursionLimit <= 0 || RecursionLimit > 999 )
          {
             fprintf(stderr,"edu: Invalid directory display limit of %d.\n", RecursionLimit );
             exit(1);
          }
      }
      else
      {
         strcpy( path, argv[argc] );
      }

   }


   OverallTotal =   DirectoryTotal( path, total_only, PathDelimiter, 1, RecursionLimit) ;

   if( total_only )
      printf( "%6.2lf Megabytes\n",
        (double)((double)OverallTotal.Megabytes+((double)OverallTotal.Bytes/(double)MEGABYTE)));

   return 0;
}



