﻿namespace a2p.Shared.Core.Interfaces.Services
{
    public interface IPrefService
    {
        // string AddItem(Interop.PrefSales.SalesDoc salesDocument, double width, double height, int quantity, double weight, double price, string nomenclature, string model, string description);

        Task<(int, int)> GetSalesDocAsync(string order);


    }
}
