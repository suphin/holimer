using Ekomers.Models.Entity.Production;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services;

public static class FasonStockModel
{
    public static void Configure(ModelBuilder b)
    {
        foreach(var type in new[]{typeof(FsnWarehouse),typeof(FsnReceipt),typeof(FsnReceiptLine),typeof(FsnStockMovement)})
        {
            var e=b.Entity(type);e.HasBaseType((Type?)null);e.ToTable(type.Name);e.HasKey("ID");
            foreach(var p in e.Metadata.GetProperties().Where(x=>x.ClrType==typeof(decimal)))e.Property(p.Name).HasPrecision(18,6);
        }
        var w=b.Entity<FsnWarehouse>();w.Property(x=>x.Code).HasMaxLength(50);w.Property(x=>x.Name).HasMaxLength(300);w.Property(x=>x.CustomerCode).HasMaxLength(50);w.Property(x=>x.CustomerName).HasMaxLength(250);w.Property(x=>x.Notes).HasMaxLength(500);w.HasIndex(x=>x.Code).IsUnique();w.HasIndex(x=>x.CustomerCode).IsUnique();
        var r=b.Entity<FsnReceipt>();r.Property(x=>x.Number).HasMaxLength(60);r.Property(x=>x.DispatchNumber).HasMaxLength(100);r.Property(x=>x.Notes).HasMaxLength(500);r.Property(x=>x.PostedUser).HasMaxLength(450);r.Property(x=>x.ReversedUser).HasMaxLength(450);r.Property(x=>x.ReversalReason).HasMaxLength(500);r.Property(x=>x.Status).IsConcurrencyToken();r.HasIndex(x=>x.Number).IsUnique();r.HasIndex(x=>new{x.WarehouseId,x.ReceiptDate});r.HasOne<FsnWarehouse>().WithMany().HasForeignKey(x=>x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        var l=b.Entity<FsnReceiptLine>();l.Property(x=>x.LotNumber).HasMaxLength(100);l.Property(x=>x.Notes).HasMaxLength(250);l.HasIndex(x=>new{x.ReceiptId,x.Sequence}).IsUnique();l.HasIndex(x=>new{x.MaterialId,x.LotNumber});l.HasOne<FsnReceipt>().WithMany().HasForeignKey(x=>x.ReceiptId).OnDelete(DeleteBehavior.Restrict);l.HasOne<PrdMaterial>().WithMany().HasForeignKey(x=>x.MaterialId).OnDelete(DeleteBehavior.Restrict);l.HasOne<PrdUnit>().WithMany().HasForeignKey(x=>x.UnitId).OnDelete(DeleteBehavior.Restrict);l.HasOne<PrdUnit>().WithMany().HasForeignKey(x=>x.OriginalUnitId).OnDelete(DeleteBehavior.Restrict);
        r.HasIndex(x=>x.RequestId).IsUnique();
        var m=b.Entity<FsnStockMovement>();m.Property(x=>x.Reason).HasMaxLength(500);m.HasIndex(x=>new{x.ReceiptLineId,x.IsReversal}).IsUnique();m.HasIndex(x=>x.MovementDate);m.HasOne<FsnReceiptLine>().WithMany().HasForeignKey(x=>x.ReceiptLineId).OnDelete(DeleteBehavior.Restrict);
        l.ToTable(t=>t.HasCheckConstraint("CK_FsnReceiptLine_Positive","[Quantity] > 0 AND [OriginalQuantity] > 0"));m.ToTable(t=>t.HasCheckConstraint("CK_FsnStockMovement_Positive","[Quantity] > 0"));
    }
}
