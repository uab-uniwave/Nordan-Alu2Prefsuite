﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using a2p.Shared.Application.Domain.Entities;
using a2p.Shared.Application.Domain.Enums;
using a2p.Shared.Application.Interfaces;
using a2p.Shared.Application.Models;
using a2p.Shared.Infrastructure.Interfaces;

using ClosedXML.Excel;

using Microsoft.Extensions.Configuration;

namespace a2p.Shared.Application.Services
{
    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;
        private readonly IExcelReadService _excelReadService;
        private readonly IUserSettingsService _userSettingsService;
        private SettingsContainer _settingsContainer;
        private AppSettings _appSettings;
        private readonly DataCache _dataCache;

        private readonly IMapperSapaV1 _mapperSapaV1;
        private readonly IMapperSapaV2 _mapperSapaV2;
        private readonly IMapperSchuco _mapperSchuco;

        private ProgressValue _progressValue;
        private IProgress<ProgressValue>? _progress;

        public FileService(IUserSettingsService userSettingsService,
                           ILogService logService,
                           IExcelReadService excelReadService,
                           IPrefSuiteService prefSuiteService,
                           IMapperSapaV1 mapperSapaV1,
                           IMapperSapaV2 mapperSapaV2,
                           IMapperSchuco mapperSchuco,
                           DataCache dataCache)

        {
            _logService = logService;
            _progressValue = new ProgressValue();
            _progress = new Progress<ProgressValue>();
            _excelReadService = excelReadService;
            _userSettingsService = userSettingsService;
            _appSettings = _userSettingsService.LoadSettings();
            _settingsContainer = _userSettingsService.LoadAllSettings();
            _mapperSapaV1 = mapperSapaV1;
            _mapperSapaV2 = mapperSapaV2;
            _mapperSchuco = mapperSchuco;
            _dataCache = dataCache;
        }

        //======================================================================
        //🔵 Read
        //======================================================================
        public async Task<ProgressValue> GetOrdersAsync(ProgressValue progressValue, IProgress<ProgressValue>? progress = null)
        {
            try
            {
                _progressValue = progressValue;
                _progress = progress;
                await GetOrderNumbersAsync();

                List<A2POrder> a2pOrders = _dataCache.GetAllOrders();
                _progressValue.MinValue = 0;
                _progressValue.MaxValue = a2pOrders.Count * 7;
                int ordersCounter = 0;

                // ======================================
                // 🔵 Iterate through orders
                // ======================================
                for (int i = 0; i < a2pOrders.Count; i++)
                {

                    _progressValue.ProgressTask1 = $"Reading Order # {ordersCounter} of {a2pOrders.Count} - {a2pOrders[i].Order} ";
                    _progressValue.Value++; // Increment the progress value - Read per Order 1
                    _progress?.Report(_progressValue);

                    a2pOrders[i].Files = await GetOrderFilesAsync(a2pOrders[i].Order, _progressValue, _progress);
                    _progressValue.Value++; // Increment the progress value - Read per Order 2
                    _progress?.Report(_progressValue);
                    // Check if the order exists in the PrefSuite
                    //===================================================================================================================================================

                    int fileCounter = 0;

                    // ======================================
                    // 🔵 Iterate through files
                    // ======================================

                    for (int j = 0; j < a2pOrders[i].Files.Count; j++)
                    {

                        fileCounter++;

                        _progressValue.ProgressTask2 = $"Reading File {fileCounter} of {a2pOrders[i].Files.Count()} - {a2pOrders[i].Files[j].FileName}";

                        _progress?.Report(_progressValue);
                        if (a2pOrders[i].Files[j].FileName.Contains(" "))
                        {
                            a2pOrders[i].SourceAppType = SourceAppType.SapaV2;

                            if (a2pOrders[i].Files[j].FileName.Contains("Price_Details"))
                            {
                                a2pOrders[i].Files[j].IsOrderItemsFile = true;
                            }

                        }
                        if (a2pOrders[i].Files[j].IsLocked)
                        {
                            _logService.Warning("File Service: File is locked {$FilePath}", a2pOrders[i].Files[j].FilePath);
                            a2pOrders[i].ReadErrors.Add(new A2POrderError
                            {
                                Order = a2pOrders[i].Order,
                                Level = ErrorLevel.Fatal,
                                Code = ErrorCode.FileSystemRead,
                                Message = $"Order {a2pOrders[i]} file {a2pOrders[i].Files[j].FileName} is locked by other application! "

                            });

                            a2pOrders[i].Files[j].Order = a2pOrders[i].Order;
                        }

                        List<A2PWorksheet> a2pWorksheets = await _excelReadService.GetWorksheetsAsync(a2pOrders[i].Files[j], _progressValue, _progress);

                        a2pOrders[i].Files[j].Worksheets = a2pWorksheets;

                        int worksheetCounter = 0;

                        for (int k = 0; k < a2pOrders[i].Files[j].Worksheets.Count; k++)
                        {
                            worksheetCounter++;

                            a2pOrders[i].Files[j].Worksheets[k].Order = a2pOrders[i].Order;

                            _progressValue.ProgressTask2 = $"Reading worksheet {worksheetCounter} of {a2pOrders[i].Files[j].Worksheets.Count} - {a2pOrders[i].Files[j].Worksheets[k].Name}.";
                            _progress?.Report(_progressValue);

                            //==================================================================================================================================
                            //🔵 Sapa V1 Mapping  not used
                            //==================================================================================================================================
                            if (a2pOrders[i].Files[j].IsOrderItemsFile && a2pOrders[i].SourceAppType == SourceAppType.SapaV1)
                            {
                                _ = await _mapperSapaV1.MapItemsAsync(a2pOrders[i].Files[j].Worksheets[k], _progressValue, _progress);
                            }
                            else if (!a2pOrders[i].Files[j].IsOrderItemsFile && a2pOrders[i].SourceAppType == SourceAppType.SapaV1)
                            {
                                _ = await _mapperSapaV1.MapMaterialsAsync(a2pOrders[i].Files[j].Worksheets[k], _progressValue, _progress);
                            }

                            //==================================================================================================================================
                            //🔵 Sapa V2 Mapping 
                            //==================================================================================================================================
                            else if (a2pOrders[i].Files[j].IsOrderItemsFile && a2pOrders[i].SourceAppType == SourceAppType.SapaV2)
                            {
                                await _mapperSapaV2.MapItemsAsync(a2pOrders[i].Files[j].Worksheets[k], _progressValue, _progress);

                            }
                            else if (!a2pOrders[i].Files[j].IsOrderItemsFile && a2pOrders[i].SourceAppType == SourceAppType.SapaV2)
                            {

                                await _mapperSapaV2.MapMaterialsAsync(a2pOrders[i].Files[j].Worksheets[k], _progressValue, _progress);
                            }
                            //==================================================================================================================================
                            //🔵 Schuco Mapping  not used
                            //==================================================================================================================================
                            else if (a2pOrders[i].Files[j].IsOrderItemsFile && a2pOrders[i].SourceAppType == SourceAppType.Schuco)
                            {
                                _ = await _mapperSchuco.MapItemsAsync(a2pOrders[i].Files[j].Worksheets[k], _progressValue, _progress);
                            }
                            else if (!a2pOrders[i].Files[j].IsOrderItemsFile && a2pOrders[i].SourceAppType == SourceAppType.Schuco)
                            {
                                _ = await _mapperSchuco.MapMaterialsAsync(a2pOrders[i].Files[j].Worksheets[k], _progressValue, _progress);
                            }

                        }

                    }

                    
                    _progressValue.Value++; // Increment the progress value - Read per Order 3
                    _progress?.Report(_progressValue);
                }
                return _progressValue;
            }
            catch (Exception ex)
            {

                _logService.Error("File Service: Unhandled Error.Exception{$Exception}", ex.Message);
                return _progressValue;
            }

        }

        private async Task GetOrderNumbersAsync()
        {
            try
            {
                List<A2POrder> orders = [];
         

                //GetrList of files in the root destinationFolder
                //==============================================================================================
                string rootFolder = _appSettings.Folders.RootFolder;
                List<string> files = (await Task.Run(() => Directory.GetFiles(rootFolder))).ToList(); // Get all files in the root destinationFolder
                if (files == null || !files.Any())
                {
                    _logService.Information("File Service: No files found in {$RootFolder}", rootFolder);
                    return;
                }

                //Extract order numbers from the file names and remove duplicates
                //==============================================================================================
                List<string> orderNumbers = files.Where(o => o != null && !o.Contains("~$"))
                    .Select(o => System.IO.Path.GetFileName(o)!.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries)[0])
                    .Distinct()
                    .OrderBy(o => o)
                    .ToList();

                // if no orders found return empty list
                //============================================================================================== 
                if (orderNumbers == null || !orderNumbers.Any())
                {
                    _logService.Information("File Service: No a2pOrders found in {$RootFolder}", rootFolder);

                    return;
                }
                foreach (string orderNumber in orderNumbers)
                {

                    A2POrder order = new() { Order = orderNumber };
                    _dataCache.AddOrder(order);
                }
            }
            catch (Exception ex)
            {
                _logService.Error("File Service: Unhandled Error getting order numbers. Exception{$Exception}", ex.Message);

            }

        }

        private async Task<List<A2PFile>> GetOrderFilesAsync(string orderNumber, ProgressValue progressValue, IProgress<ProgressValue>? progress = null)
        {
            try
            {
                string rootFolder = _appSettings.Folders.RootFolder;
                List<string> files = (await Task.Run(() => Directory.GetFiles(rootFolder))).ToList(); // Get all files in the root destinationFolder
                List<string> orderFiles = files.Where(file => file.Contains(orderNumber) && file.EndsWith(".xlsx")).ToList(); // Get all files that match the order number

                _progressValue.ProgressTask2 = $"Found {orderFiles.Count} files for order {orderNumber}";
                _progress?.Report(_progressValue);
                List<A2PFile> a2pFiles = [];

                for (int i = 0; i < orderFiles.Count; i++)
                {

                    _progressValue.ProgressTask2 = $"Reading file {i + 1} of {orderFiles.Count} - {orderFiles[i]}";
                    _progress?.Report(_progressValue);

                    A2PFile a2pFile = new()
                    {
                        Order = orderNumber,
                        File = orderFiles[i],
                        IsLocked = await IsLockedAsync(orderFiles[i]),
                        FilePath = System.IO.Path.GetDirectoryName(orderFiles[i]) ?? string.Empty,
                        FileName = System.IO.Path.GetFileName(orderFiles[i]) ?? string.Empty
                    };

                    a2pFiles.Add(a2pFile);
                }
                return a2pFiles;
            }
            catch (Exception ex)
            {
                _logService.Error("File Service: Unhandled Error getting fails. Exception;: {$Exception}", ex.Message);
                return [];
            }
        }

        private async Task<bool> IsLockedAsync(string filePath)
        {
            try

            {
                using FileStream stream = await Task.Run(() => new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                return false;
            }
            catch (Exception ex)
            {
                _logService.Warning("File Service: File locked ${FilePath}. Reason: ${Exception}", filePath, ex.Message);
            }
            return true;
        }

        //======================================================================
        // Write
        //======================================================================
        public void MoveFilesAsync()
        {
            try
            {
                List<A2POrder> a2pOrders = _dataCache.GetAllOrders();

                foreach (A2POrder a2pOrder in a2pOrders)
                {

                    if (a2pOrder.Import == false)
                    {
                        continue;
                    }

                    string destinationFolder = string.Empty;
                    string rootFolder = _appSettings.Folders.RootFolder;
                    string failedFolder = _appSettings.Folders.ImportFailed;
                    string SuccessFolder = _appSettings.Folders.ImportSuccess;
                    int totalErrors = a2pOrder.WriteErrors.Count(e => e.Level is ErrorLevel.Fatal or ErrorLevel.Error);
                    destinationFolder = totalErrors > 0 ? System.IO.Path.Combine(rootFolder, failedFolder) : System.IO.Path.Combine(rootFolder, SuccessFolder);

                    foreach (A2PFile file in a2pOrder.Files)
                    {

                        if (!Directory.Exists(file.FilePath.Replace(file.FileName, "")))

                        {

                            if (!Directory.Exists(file.File))
                            {
                                _logService.Information("File Service: Moving files from {$SourceFolder} to {$destinationFolder}", rootFolder, destinationFolder);
                                continue;
                            }

                        }

                        if (!Directory.Exists(destinationFolder))
                        {
                            _ = Directory.CreateDirectory(destinationFolder);
                        }

                        if (File.Exists(file.File))
                        {

                            if (File.Exists(System.IO.Path.Combine(destinationFolder, System.IO.Path.GetFileName(file.FileName))))
                            {
                                File.Delete(System.IO.Path.Combine(destinationFolder, System.IO.Path.GetFileName(file.FileName)));
                            }

                            File.Move(file.File, System.IO.Path.Combine(destinationFolder, System.IO.Path.GetFileName(file.FileName)));
                        }

                    }
                }

                WriteExcelLog();
            }
            catch (Exception ex)
            {
                _logService.Fatal("File Service: Unhandled error moving files. Exception: {$Exception}.", ex.Message);
            }
        }

        public void WriteExcelLog()
        {
            foreach (A2POrder a2pOrder in _dataCache.GetAllOrders())
            {

                if (a2pOrder.Import == false)
                {
                    continue;
                }

                string destinationFolder = string.Empty;
                int totalErrors = a2pOrder.WriteErrors.Count(e => e.Level is ErrorLevel.Fatal or ErrorLevel.Error);
                string rootFolder = _appSettings.Folders.RootFolder;
                string failedFolder = _appSettings.Folders.ImportFailed;

                if (totalErrors > 0)
                {
                    string fileName = System.IO.Path.Combine(rootFolder, failedFolder, a2pOrder.Order + " ErrorList.xlsx");

                    System.Data.DataTable dataTable = new();

                    _ = dataTable.Columns.Add("Order", typeof(string));
                    _ = dataTable.Columns.Add("Level", typeof(string));
                    _ = dataTable.Columns.Add("Code", typeof(string));
                    _ = dataTable.Columns.Add("Message", typeof(string));

                    using (XLWorkbook workbook = new())
                    {

                        foreach (A2POrderError error in a2pOrder.WriteErrors)
                        {
                            _ = dataTable.Rows.Add(error.Order, error.Level.ToString(), error.Code.ToString(), error.Message);
                            _logService.Information("Log saved successfully to {FileName}", System.IO.Path.GetFileName(fileName));

                        }

                        _ = workbook.Worksheets.Add(dataTable, "LogRecords");

                        workbook.SaveAs(fileName);
                    }

                }
            }
        }

    }
}
