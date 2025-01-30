﻿using a2p.Shared.Core.DTO;
using a2p.Shared.Core.Interfaces.Repository;
using a2p.Shared.Core.Interfaces.Services.Import.SubServices;
using a2p.Shared.Core.Utils;


namespace a2p.Shared.Infrastructure.Services.Import.SubServices
{
 public class ImportSapa_v2 : IImportSapa_v2
 {
  private readonly ILogService _logger;
  private readonly ISqlService _sqlService;

  public ImportSapa_v2(ILogService logger, ISqlService sqlService)
  {
   _logger=logger;
   _sqlService=sqlService;
  }


  public async Task<int> ImportItemsAsync(List<ItemDTO> position)
  {
   return await Task.Run(() => 0);
  }

  public async Task<int> ImportMaterialsAsync(List<MaterialDTO> material)
  {
   return await Task.Run(() => 0);
  }



  public async Task<int> ImportGlassesAsync(List<GlassDTO> glass)
  {
   return await Task.Run(() => 0);

  }


  public async Task<int> ImportPanelsAsync(List<PanelDTO> panels)
  {

   return await Task.Run(() => 0);

  }



 }
}

