﻿using a2p.Shared.Core.Entities.Models;
using a2p.Shared.Core.Enums;
using a2p.Shared.Core.Interfaces.Services;
using a2p.Shared.Core.Interfaces.Services.Other.Mappers;

namespace a2p.Shared.Infrastructure.Services
{
    public class MappingService : IMappingService
    {
        private readonly ILogService _logService;
        private readonly IGlassMapper _glassMapper;
        private readonly IPanelMapper _panelMapper;
        private readonly IItemMapper _itemMapper;
        private readonly IMaterialMapper _materialMapper;
        private ProgressValue _progressValue;

        public MappingService(ILogService logService,
              IMaterialMapper _writeService,
              IItemMapper itemMapper,
              IMaterialMapper materialMapper,
              IGlassMapper glassMapper,
              IPanelMapper panelMapper)
        {
            _logService = logService;
            _itemMapper = itemMapper;
            _materialMapper = materialMapper;
            _glassMapper = glassMapper;
            _panelMapper = panelMapper;
            _progressValue = new ProgressValue();
        }

        public async Task MapDataAsync(IEnumerable<A2POrder> a2POrderList, IProgress<ProgressValue>? progress = null)
        {
            try
            {
                if (a2POrderList.Count() == 0)
                {
                    _logService.Error("Import Service: OrderNumber list is null");
                    throw new ArgumentNullException(nameof(a2POrderList));
                }


                int orderCount = 0;
                _progressValue.MaxValue = a2POrderList.Count();
                _progressValue.MinValue = 0;
                _progressValue.Value = 0;
                _progressValue.ProgressTitle = $"Processing Orders...";
                _progressValue.ProgressTask1 = string.Empty;
                _progressValue.ProgressTask2 = string.Empty;
                _progressValue.ProgressTask3 = string.Empty;
                progress?.Report(_progressValue);

                foreach (A2POrder order in a2POrderList)
                {
                    _progressValue.ProgressTitle = $"Importing OrderNumber # {order.OrderNumber}. OrderNumber {orderCount + 1} of {a2POrderList.Count()}";
                    _progressValue.Value = orderCount + 1;
                    _progressValue.ProgressTask1 = $"Processing OrderFiles...";
                    progress?.Report(_progressValue);

                    if (order == null)
                    {
                        _logService.Error("Import Service: OrderNumber is null");
                        continue;
                    }

                    if (order.OrderFiles == null)
                    {
                        _logService.Error($"Import Service: OrderFiles of OrderNumber # {order.OrderNumber} are null!");
                        continue;
                    }
                    int fileCount = 0;
                    foreach (A2POrderFile file in order.OrderFiles)
                    {

                        _progressValue.ProgressTask1 = $"Importing file {file.FileName}. File {fileCount + 1} of {order?.OrderFiles.Count}.";
                        _progressValue.ProgressTask2 = $"Processing Worksheets...";
                        progress?.Report(_progressValue);

                        if (order == null)
                        {
                            _logService.Error("MS: Error at file ${File} order is null", file.FileName);
                            continue;
                        }


                        if (file.OrderFileWorksheets == null)
                        {
                            _logService.Error($"MS: Worksheets in file {file.FileName} are null!");
                            continue;
                        }

                        int worksheetCount = 0;
                        foreach (A2POrderFileWorksheet worksheet in file.OrderFileWorksheets)

                        {
                            _progressValue.ProgressTask2 = $"Processing Worksheet {worksheet.WorksheetName}. Worksheet {worksheetCount + 1} of {file.OrderFileWorksheets.Count}.";
                            _progressValue.ProgressTask3 = $"Processing Rows...";
                            progress?.Report(_progressValue);

                            if (worksheet == null)
                            {
                                _logService.Error("MS: Error Worksheet in file {File} is null", file.FileName);
                                continue;
                            }

                            if (worksheet.WorkSheetRowCount == 0)
                            {
                                _logService.Error("MS: Error in file {File}, worksheet {$Worksheet} row count is 0.", file.FileName, worksheet.WorksheetName);
                                continue;
                            }



                            _logService.Debug("MS. Start importing order {$OrderNumber}, {$WorksheetType}", worksheet.OrderNumber ?? "Unknown", worksheet.WorksheetType.ToString());


                            if (worksheet.WorksheetType == WorksheetType.Items_Sapa_v1)
                            {
                                var result = await _itemMapper.GetSapa_v1Async(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Items_Sapa_v2)
                            {
                                var result = await _itemMapper.GetSapa_v2Async(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Items_Schuco)
                            {
                                var result = await _itemMapper.GetSchucoAsync(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Materials_Sapa_v1)
                            {
                                var result = await _materialMapper.GetSapa_v1Async(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Materials_Sapa_v2)
                            {
                                var result = await _materialMapper.GetSapa_v2Async(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Materials_Schuco)
                            {
                                var result = await _materialMapper.GetSchucoAsync(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Glasses_Sapa_v1)
                            {
                                var result = await _glassMapper.GetSapa_v1Async(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Glasses_Sapa_v2)
                            {
                                var result = await _glassMapper.GetSapa_v2Async(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Glasses_Schuco)
                            {
                                var result = await _glassMapper.GetSchucoAsync(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Panels_Sapa_v1)
                            {
                                var result = await _panelMapper.GetSapa_v1Async(worksheet);
                            }
                            else if (worksheet.WorksheetType == WorksheetType.Panels_Sapa_v2)
                            {
                                var result = await _panelMapper.GetSapa_v2Async(worksheet);
                            }
                            else
                            {
                                _logService.Error("MS: Error  in file {File}, worksheet {$Worksheet} type is Unknown", file.FileName, worksheet.WorksheetName);
                                continue;
                            }

                            _logService.Debug("Import Service. Finish importing order {$OrderNumber}, {WorksheetType}", worksheet.OrderNumber ?? "Unknown", worksheet.WorksheetType.ToString());
                            worksheetCount++;
                        }



                        if (file.OrderFileWorksheets == null)
                        {
                            _logService.Error($"Import Service: Worksheet in file {file.FileName} is null!");
                            continue;
                        }
                        fileCount++;
                    }
                    orderCount++;
                }

            }

            catch (Exception ex)
            {
                _logService.Error(ex, "MS: Unhandled Error while importing data", ex.Message);
                throw;

            }

        }

    }
}

