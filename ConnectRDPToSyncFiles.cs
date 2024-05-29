using FileSync;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

public class ConnectRDPToSyncFiles
{
    private readonly ILogger<WorkerService> _logger;
    /// <summary>
    /// Initiate logger 
    /// </summary>
    /// <param name="logger"></param>
    public ConnectRDPToSyncFiles(ILogger<WorkerService> logger)
    {
        _logger = logger;
    }

    #region Variables

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
        int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public extern static bool CloseHandle(IntPtr handle);
    //[PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]

    #endregion

    public void GetFilesFromRDP(string lpszUsername, string lpszPassword, string lpszDomain, string computerip)
    {
        SafeTokenHandle safeTokenHandle;
        try
        {
            string userName, domainName;
            domainName = lpszDomain;
            userName = lpszUsername;
            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;
            bool returnValue = LogonUser(userName, domainName, lpszPassword,
                LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                out safeTokenHandle);

            #region logInfo
            Console.WriteLine("LogonUser called.");
            _logger.LogInformation("RDP LogonUser called........: {time}", DateTimeOffset.Now);
            #endregion

            if (false == returnValue)
            {
                int ret = Marshal.GetLastWin32Error();
                #region logInfo
                Console.WriteLine("LogonUser failed with error code : {0}", ret);
                _logger.LogInformation("LogonUser failed with error code........: {time}", ret);
                #endregion
                return;
            }
            using (safeTokenHandle)
            {
                using (WindowsIdentity newId = new WindowsIdentity(safeTokenHandle.DangerousGetHandle()))
                {
                    #region stop watch
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    #endregion

                    if (newId.IsAuthenticated)
                    {
                        string folderPath = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString();
                        DirectoryInfo dir = new DirectoryInfo(string.Format(@"Server path" + @"\" + folderPath));
                        FileInfo[] files = dir.GetFiles();

                        #region Shared Network Drive Connecting

                        bool validLogin = false;
                        using (PrincipalContext tempcontext = new PrincipalContext(ContextType.Domain,
                                                                                   lpszDomain
                                                                                  , null, ContextOptions.Negotiate))
                        {
                                                       
                            validLogin = tempcontext.ValidateCredentials("Username",
                                                                         "Password",
                                                                         ContextOptions.Negotiate);
                            #region logInfo
                            _logger.LogInformation("Shared Network Drive Connected........: {0}", validLogin);
                            #endregion
                        }

                        #endregion

                        #region logInfo
                        _logger.LogInformation("Total file count :........: {0}", files.Length);
                        #endregion

                        for (int i = 0; i < files.Length; i++)
                        {
                            string sharedDrivePath = "shared drive location";
                           
                            #region logInfo
                            Console.WriteLine("file Count : " + i);
                            _logger.LogInformation("Processing file count :........: {0}", i);
                            #endregion

                            var filename = files[i].FullName;
                            if (!CheckFileExist(sharedDrivePath, files[i].Name))
                            {
                                #region StopWatch 
                                Stopwatch stopWatch1 = new Stopwatch();
                                stopWatch1.Start();
                                #endregion

                                FileInfo fi = new FileInfo(files[i].FullName);
                                FileStream fs = fi.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                                UploadFileToSharedDrive(fs, sharedDrivePath, files[i].Name);

                                #region stopwatch
                                stopWatch1.Stop();
                                TimeSpan ts1 = stopWatch.Elapsed;
                                string elapsedTime1 = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                                     ts1.Hours, ts1.Minutes, ts1.Seconds,
                                                     ts1.Milliseconds / 10);
                                Console.WriteLine(" Time Taken to Insert record {0}: " + elapsedTime1, i);
                                #endregion
                            }
                        }
                    }

                    #region Stop watch to find timer for all the records 

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                         ts.Hours, ts.Minutes, ts.Seconds,
                                         ts.Milliseconds / 10);
                    Console.WriteLine("Total Time Taken to Insert: " + elapsedTime);

                    #endregion
                }

            }
        }
        catch (Exception ex)
        {
            #region logInfo            
            _logger.LogInformation("Exception occurred.  :........: {0}", ex.Message);
            Console.WriteLine("Exception occurred. " + ex.Message);
            #endregion
            
        }
    }

    /// <summary>
    /// Check File exist or not
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>    

    public bool CheckFileExist(string path, string fileName)
    {       
        FileInfo fi = new FileInfo(Path.Combine(path, fileName));
        return fi.Exists;
    }

    /// <summary>
    /// File Upload to Shared Drive
    /// </summary>
    /// <param name="fileStream"></param>
    /// <param name="path"></param>
    /// <param name="fileName"></param>   

    public void UploadFileToSharedDrive(FileStream fileStream, string path, string fileName)
    {
        try
        {
            #region logInfo            
            _logger.LogInformation("Upload Initated File to Shared Drive  :........: {0}", fileName);
            #endregion

            var mem = new MemoryStream();

            // If using .NET 4 or later:
            fileStream.CopyTo(mem);

            // Otherwise:
            CopyStream(fileStream, mem);

            // getting the internal buffer (no additional copying)
            byte[] buffer = mem.GetBuffer();
            long length = mem.Length; // the actual length of the data 
                          // (the array may be longer)

             // if you need the array to be exactly as long as the data
            byte[] truncated = mem.ToArray(); // makes another copy

            using (FileStream fs = new FileStream(Path.Combine(path, fileName), FileMode.CreateNew, FileAccess.Write))
            {
                fs.Write(truncated, 0, (int)truncated.Length);
                fs.Close();
            }

            #region logInfo            
            _logger.LogInformation("File uploaded to Shared Drive  :........: {0}", fileName);
            #endregion
        }
        catch (Exception ex)
        {
            #region logInfo            
            _logger.LogInformation("Exception occured while uploading file to shared drive  :........: {0}", fileName);
            _logger.LogInformation("Exception occured while uploading file to shared drive  :........: {0}", ex.Message);
            #endregion
        }
    }
    public static void CopyStream(Stream input, Stream output)
    {
        byte[] b = new byte[32768];
        int r;
        while ((r = input.Read(b, 0, b.Length)) > 0)
            output.Write(b, 0, r);
    }
}

/// <summary>
/// safe token validator
/// </summary>
public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SafeTokenHandle()
        : base(true)
    {
    }

    [DllImport("kernel32.dll")]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    protected override bool ReleaseHandle()
    {
        return CloseHandle(handle);
    }
}