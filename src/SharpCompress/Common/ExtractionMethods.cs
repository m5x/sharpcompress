using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SharpCompress.Common
{
    internal static class ExtractionMethods
    {
        static readonly char[] _PathComponentsSeparators =  { '/', '\\' };

        /// <summary>
        /// Extract to specific directory, retaining filename
        /// </summary>
        public static void WriteEntryToDirectory(IEntry entry, string destinationDirectory,
                                                 ExtractionOptions options, Action<string, ExtractionOptions> write)
        {
            string destinationFileName;
            string file = Path.GetFileName(entry.Key);
            string fullDestinationDirectoryPath = Path.GetFullPath(destinationDirectory);

            options = options ?? new ExtractionOptions()
                                 {
                                     Overwrite = true
                                 };

            if (options.ExtractFullPath)
            {
                string folder = Path.GetDirectoryName(entry.Key);

                if(options.StripComponents.HasValue)
                {
                    folder = folder.Trim( _PathComponentsSeparators );
                    folder += '/';
                    
                    var folderLength = folder.Length;
                    var stripIndex = 0;
                    
                    for(int n = 0, c = options.StripComponents.Value; n < c; n++)
                    {
                        stripIndex = folder.IndexOfAny( _PathComponentsSeparators, stripIndex );
                        if( stripIndex < 0 )
                            return; // mimic tar --strip-components behavior, i.e. throw away content not deep enough

                        // consider multiple sequential path component separators as one
                        stripIndex++;
                        while( stripIndex < folderLength )
                        {
                            var strip = false;
                            var chr = folder[stripIndex];
                            for( int i = 0; i < _PathComponentsSeparators.Length; i++ )
                            {
                                if( chr == _PathComponentsSeparators[i] )
                                {
                                    strip = true;
                                    break;
                                }
                            }

                            if( strip )
                                stripIndex++;
                            else
                                break;
                        }
                    }

                    folder = folder.Substring( stripIndex ).TrimEnd( '/' );
                }
                
                string destdir = Path.GetFullPath(
                                                  Path.Combine(fullDestinationDirectoryPath, folder)
                                                 );

                if (!Directory.Exists(destdir))
                {
                    if (!destdir.StartsWith(fullDestinationDirectoryPath))
                    {
                        throw new ExtractionException("Entry is trying to create a directory outside of the destination directory.");
                    }

                    Directory.CreateDirectory(destdir);
                }
                destinationFileName = Path.Combine(destdir, file);
            }
            else
            {        
                destinationFileName = Path.Combine(fullDestinationDirectoryPath, file);

            }

            if (!entry.IsDirectory)
            {
                destinationFileName = Path.GetFullPath(destinationFileName);

                if (!destinationFileName.StartsWith(fullDestinationDirectoryPath))
                {
                    throw new ExtractionException("Entry is trying to write a file outside of the destination directory.");
                }
                write(destinationFileName, options);
            }
            else if (options.ExtractFullPath && !Directory.Exists(destinationFileName))
            {
                Directory.CreateDirectory(destinationFileName);
            }
        }
        
        public static void WriteEntryToFile(IEntry entry, string destinationFileName,
                                            ExtractionOptions options,
                                            Action<string, FileMode> openAndWrite)
        {
            if (entry.LinkTarget != null)
            {
                if (null == options.WriteSymbolicLink)
                {
                    throw new ExtractionException("Entry is a symbolic link but ExtractionOptions.WriteSymbolicLink delegate is null");
                }
                options.WriteSymbolicLink(destinationFileName, entry.LinkTarget);
            }
            else
            {
                FileMode fm = FileMode.Create;
                options = options ?? new ExtractionOptions()
                                     {
                                         Overwrite = true
                                     };

                if (!options.Overwrite)
                {
                    fm = FileMode.CreateNew;
                }

                openAndWrite(destinationFileName, fm);
                entry.PreserveExtractionOptions(destinationFileName, options);
            }
        }
    }
}