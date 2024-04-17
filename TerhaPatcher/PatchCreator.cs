
using System.IO;
using System.Security.Cryptography;
using VCDiff.Includes;
using VCDiff.Encoders;
using System.ComponentModel;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using VCDiff.Decoders;

namespace TerhaPatcher
{
    public class PatchCreator
    {

        private readonly ILogger logger;
        public PatchCreator(ILogger logger)
        {
            this.logger = logger;

            // Scrive un messaggio di inizializzazione nel file di log
            logger.Log("PatchCreator init...");
        }
        
        private void DoEncode(string outPatchFile, string oldFile, string newFile)
        {
            // encode the patch

            byte[] hash = GetSha1FromFile(oldFile);
            byte[] newHash = GetSha1FromFile(newFile);
            using (FileStream output = new FileStream(outPatchFile, FileMode.Create, FileAccess.Write))
            using (FileStream dict = new FileStream(oldFile, FileMode.Open, FileAccess.Read))
            using (FileStream target = new FileStream(newFile, FileMode.Open, FileAccess.Read))
            {
                output.Write(hash, 0, 20);
                output.Write(newHash, 0, 20);

                VcEncoder coder = new VcEncoder(dict, target, output);
                VCDiffResult result = coder.Encode(); //encodes with no checksum and not interleaved
                if (result != VCDiffResult.SUCCESS)
                {
                    logger.Log("DoEncode was not able to encode properly file: " + Path.GetFileName(oldFile), "ERROR");
                    throw new Exception("DoEncode was not able to encode properly file: " + Path.GetFileName(oldFile));
                }
            }
        }

        private void GetAllFilesInDirectoryRec(string dir, List<String> files)
        {
            files.AddRange(Directory.GetFiles(dir));
            foreach (string subDir in Directory.GetDirectories(dir))
            {
                GetAllFilesInDirectoryRec(subDir, files);
            }
        }

        private string[] GetAllFilesInDirectory(string dir)
        {
            List<String> files = new List<string>();
            GetAllFilesInDirectoryRec(dir, files);
            return files.ToArray();
        }

        private bool isPathInsidePath(string dir1, string dir2)
        {
            return StringExtensions.IsSubPathOf(dir1, dir2) || StringExtensions.IsSubPathOf(dir2, dir1);
        }

        //Gets 20 bytes SHA1 hash
        private byte[] GetSha1FromFile(string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open))
            using (BufferedStream bs = new BufferedStream(fs))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(bs);
                    return hash;
                }
            }
        }

        private bool FilesAreEqual(string file1, string file2)
        {
            FileInfo fi1 = new FileInfo(file1);
            FileInfo fi2 = new FileInfo(file2);
            return FilesAreEqual(fi1, fi2);

        }


        private bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            const int BYTES_TO_READ = sizeof(Int64);
            if (first.Length != second.Length)
                return false;

            if (string.Equals(first.FullName, second.FullName, StringComparison.OrdinalIgnoreCase))
                return true;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }

            return true;
        }

        public string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        /// <summary>
        /// Returns a relative path string from a full path based on a base path
        /// provided.
        /// </summary>
        /// <param name="fullPath">The path to convert. Can be either a file or a directory</param>
        /// <param name="basePath">The base path on which relative processing is based. Should be a directory.</param>
        /// <returns>
        /// String of the relative path.
        /// 
        /// Examples of returned values:
        ///  test.txt, ..\test.txt, ..\..\..\test.txt, ., .., subdir\test.txt
        /// </returns>
        public static string GetRelativePath(string fullPath, string basePath)
        {
            // Require trailing backslash for path
            if (!basePath.EndsWith("\\"))
                basePath += "\\";

            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            // Uri's use forward slashes so convert back to backward slashes
            //return relativeUri.ToString().Replace("/", "\\");
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace("/", "\\");

        }

        public void CreatePatch(string oldVersionPath, string newVersionPath, string outputFile, System.Windows.Controls.ProgressBar progressBar)
        {
           
            try
            {
                if (string.IsNullOrEmpty(oldVersionPath) || string.IsNullOrEmpty(newVersionPath) || string.IsNullOrEmpty(outputFile) || progressBar == null)
                {
                    logger.Log("All parameters must be not null or non empty", "ERROR");
                    throw new ArgumentNullException("All parameters must be not null or non empty");
                }
                
                logger.Log("Patch creation started...");
                logger.Log($"oldVersionPath={oldVersionPath}, newVersionPath={newVersionPath}, outputFile={outputFile}");

                if (isPathInsidePath(oldVersionPath, newVersionPath))
                {
                    logger.Log("Paths overlapping", "ERROR");
                    throw new ArgumentException("Paths overlapping");
                }

                // Utilizziamo un BackgroundWorker per eseguire il lavoro in background
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;

                // Evento che si verifica quando il lavoro in background ha fatto progressi
                worker.ProgressChanged += (sender, e) =>
                {
                    progressBar.Value = e.ProgressPercentage;
                };

                // Evento che si verifica quando il lavoro in background è completato
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    progressBar.Value = 100; // Assicuriamoci che la progress bar sia al 100% alla fine
                };

                // Logica per il lavoro in background
                worker.DoWork += (sender, e) =>
                {
                    List<String> oldFiles = new List<string>();
                    foreach (string file in GetAllFilesInDirectory(oldVersionPath))
                    {
                        oldFiles.Add(GetRelativePath(file, oldVersionPath));
                    }

                    List<string> newFiles = new List<string>();
                    foreach (string file in GetAllFilesInDirectory(newVersionPath))
                    {
                        newFiles.Add(GetRelativePath(file, newVersionPath));
                    }

                    string patchFolder = GetTemporaryDirectory();

                    int totalFiles = newFiles.Count + oldFiles.Count;
                    int filesProcessed = 0;

                    // Processo la creazione dei nuovi file
                    foreach (string newFileSubPath in newFiles)
                    {
                        string newFile = Path.Combine(newVersionPath, newFileSubPath);
                        string oldFile = Path.Combine(oldVersionPath, newFileSubPath);

                        if (!File.Exists(oldFile))
                        {
                            string fileToCreate = Path.Combine(patchFolder, newFileSubPath + ".new");
                            Directory.CreateDirectory(Path.GetDirectoryName(fileToCreate));
                            File.Copy(newFile, fileToCreate);

                            filesProcessed++;
                            worker.ReportProgress(filesProcessed * 100 / totalFiles);
                        }
                    }

                    // Processo la lettura degli altri file e la creazione dei file di aggiornamento
                    foreach (string newFileSubPath in newFiles)
                    {
                        string newFile = Path.Combine(newVersionPath, newFileSubPath);
                        string oldFile = Path.Combine(oldVersionPath, newFileSubPath);

                        if (File.Exists(oldFile) && !FilesAreEqual(oldFile, newFile))
                        {
                            string fileToCreate = Path.Combine(patchFolder, newFileSubPath + ".upd");
                            Directory.CreateDirectory(Path.GetDirectoryName(fileToCreate));
                            DoEncode(fileToCreate, oldFile, newFile);

                            filesProcessed++;
                            worker.ReportProgress(filesProcessed * 100 / totalFiles);
                        }
                    }

                    // Processo la creazione dei file eliminati
                    foreach (string oldFileSubPath in oldFiles)
                    {
                        string newFile = Path.Combine(newVersionPath, oldFileSubPath);
                        if (!File.Exists(newFile))
                        {
                            string fileToCreate = Path.Combine(patchFolder, oldFileSubPath + ".del");
                            Directory.CreateDirectory(Path.GetDirectoryName(fileToCreate));
                            using (StreamWriter sw = new StreamWriter(fileToCreate))

                            filesProcessed++;
                            worker.ReportProgress(filesProcessed * 100 / totalFiles);
                        }
                    }

                    // Comprimi i file
                    FastZip zip = new();
                    zip.CreateZip(outputFile, patchFolder, true, "");
                    Directory.Delete(patchFolder, true);

                };

                // Avvia il lavoro in background
                worker.RunWorkerAsync();
                logger.Log($"Patch created successfully: {outputFile}");
            }
            catch (Exception ex)
            {
                logger.Log($"Error occurred: {ex.Message}", "ERROR");
            }
            finally
            {
                logger.Log("Patch creation ended...");
            }
        }

        private bool CompareHashes(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length)
                return false;
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    return false;
                }
            }
            return true;
        }

        void DoDecode(string outputFile, string oldFile, string patchFile)
        {
            using (FileStream target = new FileStream(patchFile, FileMode.Open, FileAccess.Read))
            {
                byte[] oldHash = new byte[20];
                byte[] newHash = new byte[20];
                target.Read(oldHash, 0, oldHash.Length);
                target.Read(newHash, 0, newHash.Length);

                byte[] realHash = GetSha1FromFile(oldFile);

                bool oldHashMatches = CompareHashes(oldHash, realHash);

                if (!oldHashMatches)
                {
                    if (CompareHashes(realHash, newHash))
                    {
                        File.Copy(oldFile, outputFile);
                        return;
                    }
                    else
                        throw new Exception("file hash mismatch");
                }


                using (FileStream dict = new FileStream(oldFile, FileMode.Open, FileAccess.Read))
                using (FileStream output = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {

                    VcDecoder decoder = new VcDecoder(dict, target, output);

                    long bytesWritten = 0;
                    VCDiffResult result = decoder.Decode(out bytesWritten);

                    if (result != VCDiffResult.SUCCESS)
                    {
                        //error decoding
                        throw new Exception("Error decoding");
                    }

                    //if success bytesWritten will contain the number of bytes that were decoded
                }
            }
        }

        public void InstallUpdateFromZip(string zipFile, string outFolder, HashSet<string> filesNotUpdated)
        {

            using (FileStream fs = new FileStream(zipFile, FileMode.Open))
            {
                using (var zipInputStream = new ZipInputStream(fs))
                {
                    while (zipInputStream.GetNextEntry() is ZipEntry zipEntry)
                    {
                        var entryFileName = zipEntry.Name;
                        string relativeFilePath = entryFileName.Substring(0, entryFileName.Length - 4);




                        // Manipulate the output filename here as desired.
                        var fullZipToPath = Path.Combine(outFolder, entryFileName);
                        var directoryName = Path.GetDirectoryName(fullZipToPath);
                        if (directoryName.Length > 0)
                            Directory.CreateDirectory(directoryName);

                        // Skip directory entry
                        if (Path.GetFileName(fullZipToPath).Length == 0)
                        {
                            continue;
                        }

                        if (entryFileName.EndsWith(".del"))
                        {
                            string fileToDelete = fullZipToPath.Substring(0, fullZipToPath.Length - 4);
                            File.Delete(fileToDelete);
                            filesNotUpdated.Remove(relativeFilePath);
                            continue;
                        }

                        if (entryFileName.EndsWith(".new"))
                        {
                            fullZipToPath = fullZipToPath.Substring(0, fullZipToPath.Length - 4);
                            filesNotUpdated.Remove(relativeFilePath);
                        }

                        // Unzip file in buffered chunks. This is just as fast as unpacking
                        // to a buffer the full size of the file, but does not waste memory.
                        // The "using" will close the stream even if an exception occurs.
                        using (FileStream streamWriter = File.Create(fullZipToPath))
                        {
                            // 4K is optimum
                            var buffer = new byte[4096];
                            StreamUtils.Copy(zipInputStream, streamWriter, buffer);
                        }

                        if (entryFileName.EndsWith(".upd"))
                        {
                            string oldFile = fullZipToPath.Substring(0, fullZipToPath.Length - 4);
                            string newFile = oldFile + ".new";
                            try
                            {
                                //string patchFile, string oldFile, string newFile
                                DoDecode(newFile, oldFile, fullZipToPath);
                                filesNotUpdated.Remove(relativeFilePath);
                                File.Delete(fullZipToPath);
                                File.Delete(oldFile);
                                File.Move(newFile, oldFile);
                            }
                            catch
                            {
                                File.Delete(fullZipToPath);
                                filesNotUpdated.Add(relativeFilePath);
                            }

                        }

                    }
                }
            }
        }
    }
    
}
