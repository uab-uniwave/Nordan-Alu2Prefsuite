﻿using a2p.Shared.Core.Entities.Models;
using a2p.Shared.Core.Interfaces.Services;
using a2p.Shared.Infrastructure.Services.Other;

using Microsoft.Extensions.Configuration;

namespace a2p.Shared.Infrastructure.Services
{
    public class
     FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;
        private readonly IReadService _readService;
        private ProgressValue _progressValue;




        public FileService(IConfiguration configuration, ILogService logService, IReadService excelService)
        {
            _configuration = configuration;
            _logService = logService;
            _readService = excelService;
            _progressValue = new ProgressValue();

        }

        // Get orders and files asynchronously
        public async Task<List<A2POrder>> GetOrdersAsync(List<A2POrder> a2pOrderList, ProgressValue progressValue, IProgress<ProgressValue>? progress)
        {
            _progressValue = progressValue;
            _progressValue.ProgressTitle = $"Getting Folder...";
            progress?.Report(_progressValue);
            string rootFolder = _configuration["AppSettings:RootFolder"] ?? @"C:\Temp\Import";


            // Get file names asynchronously
            //===========================================================================================================


            //progress searching files
            {
                _progressValue.ProgressTitle = $"Searching OrderFiles in {rootFolder}...";
                progress?.Report(_progressValue);
                //Task.Delay(2000).Wait();
            }
            List<string> files = (await Task.Run(() => Directory.GetFiles(rootFolder))).ToList(); // Get all files in the root folder

            //progress found files
            {

                _progressValue.ProgressTitle = "Searching Orders...";
                _progressValue.ProgressTask1 = $"Found {files.Count}) file in {rootFolder}.";
                progress?.Report(_progressValue);
                //Task.Delay(2000).Wait();

            }



            // Extract unique orders numbers
            //===========================================================================================================
            if (files == null || !files.Any())
            {
                _logService.Warning("FS: No files found in {RootFolder}", rootFolder);
                return a2pOrderList;
            }
            IEnumerable<string?> fileNames = files.Select(Path.GetFileName).ToList();
            IEnumerable<string> orderNumbers = fileNames
             .Where(o => o != null && !o.Contains("~$"))
             .Select(o => o!.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries)[0])
             .Distinct()
             .OrderBy(o => o)
             .ToList();

            //progress found orders
            {
                _progressValue.ProgressTask1 = $"Found {orderNumbers.Count()} Orders in files...";

                progress?.Report(_progressValue);
                //Task.Delay(2000).Wait();
            }
            int count = 0;
            foreach (string orderNumber in orderNumbers)
            {



                {
                    _progressValue.ProgressTitle = $"Processing OrderNumber # {orderNumber}. OrderNumber {count + 1} / {orderNumbers.Count()}";
                    _progressValue.MaxValue = orderNumbers.Count();
                    _progressValue.Value = count + 1;
                    _progressValue.ProgressTask1 = $"Searching OrderFiles...";
                    _progressValue.ProgressTask2 = string.Empty;
                    _progressValue.ProgressTask3 = string.Empty;
                    progress?.Report(_progressValue);
                    //Task.Delay(2000).Wait();
                }
                A2POrder a2pOrder = new()
                {
                    OrderNumber = orderNumber,


                    OrderFiles = await GetSingleOrderFilesAsync(orderNumber, files, progress)
                };




                _ = await A2POrderAgregator.AddOrUpdateOrderAsync(a2pOrderList, a2pOrder);
                count++;

            }
            return a2pOrderList;
        }




        // Get files for a specific order asynchronously
        private async Task<List<A2POrderFile>> GetSingleOrderFilesAsync(string orderNumber, List<string> files, IProgress<ProgressValue>? progress = null)
        {
            List<string>? matchingFiles = files.Where(file => file.Contains(orderNumber)).ToList();
            List<A2POrderFile> a2pFileList = [];

            //progress Found files
            // 
            {
                _progressValue.ProgressTask1 = $"Found {a2pFileList.Count} files.";
                progress?.Report(_progressValue);
                //Task.Delay(2000).Wait();
            }
            _logService.Information("FS. OrderNumber # {OrderNumber}. Found {FileCount} files.", orderNumber, files.Count);



            int count = 0;


            foreach (string file in matchingFiles)
            {
                string fileName = Path.GetFileName(file) ?? string.Empty;

                {
                    _progressValue.ProgressTask1 = $"Processing {Path.GetFileName(fileName)}. File {count + 1} of {matchingFiles.Count}.";
                    _progressValue.ProgressTask2 = $"Searching worksheets...";
                    _progressValue.ProgressTask3 = string.Empty;
                    progress?.Report(_progressValue);

                }
                //Task.Delay(3000).Wait();


                A2POrderFile a2pFile = new()
                {


                    File = file.Trim(),
                    OrderNumber = orderNumber,
                    IsLocked = await IsLockedAsync(file.Trim()),
                    FilePath = Path.GetDirectoryName(file) ?? string.Empty,
                    FileName = fileName ?? string.Empty,
                };

                List<A2POrderFile> a2pFileListTemp = [a2pFile];



                //Task.Delay(2000).Wait();

                List<A2POrderFileWorksheet> wr = await _readService.GetWorksheetListAsync(a2pFileListTemp, _progressValue, progress);
                a2pFile.OrderFileWorksheets = wr;

                a2pFileList.Add(a2pFile);
                count++;
            }

            return a2pFileList;

        }

        public async Task<bool> IsLockedAsync(string filePath)
        {
            try

            {
                using FileStream stream = await Task.Run(() => new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                return false;
            }
            catch (Exception ex)
            {
                _logService.Warning("FS: File Locked: ${FilePath}. Reason: ${Exception}", filePath, ex.Message);
            }
            return true;
        }
        // Move files asynchronously
        public void MoveFilesAsync(string order, string fileName)
        {
            try
            {
                string rootFolder = _configuration["AppSettings:RootFolder"] ?? @"C:\Temp\Import";
                string scTempFolder = _configuration["AppSettings:SuccessImport"] ?? Path.Combine(rootFolder, "SC");
                string usTempFolder = _configuration["AppSettings:UnsuccessImport"] ?? Path.Combine(rootFolder, "US");

                // Implement file move logic here as needed (e.g., File.Move)
            }
            catch (Exception ex)
            {
                _logService.Fatal(ex, "FS: Unhandled error moving files");
            }
        }



    }
}