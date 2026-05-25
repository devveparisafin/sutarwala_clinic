using System;
using System.Collections.Generic;
using MongoDB.Driver;
using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Scratch
{
    public class CheckData
    {
        public static void Main()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("PharmacyERP");
            
            var masters = db.GetCollection<PurchaseMaster>("PurchaseMasters").Find(FilterDefinition<PurchaseMaster>.Empty).Limit(5).ToList();
            var suppliers = db.GetCollection<Supplier>("Suppliers").Find(FilterDefinition<Supplier>.Empty).ToList();
            
            Console.WriteLine("Purchases:");
            foreach(var m in masters)
            {
                Console.WriteLine($"Inv: {m.InvoiceNo}, SupplierId: '{m.SupplierId}'");
            }
            
            Console.WriteLine("\nSuppliers:");
            foreach(var s in suppliers)
            {
                Console.WriteLine($"Name: {s.Name}, Id: '{s.Id}'");
            }
        }
    }
}
