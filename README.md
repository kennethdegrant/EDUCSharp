//----------------------------------------------------------------------------------------------------
// Extended Disk Usage (edu) for Windows - Copyright(c) 1993-2022 - Kenneth L. DeGrant II 
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
//----------------------------------------------------------------------------------------------------
