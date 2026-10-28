using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Services;

public sealed class FasonStockService(ApplicationDbContext db)
{
    public async Task<int> CreateAsync(FasonReceiptFormVM input,string actor,CancellationToken ct=default)
    {
        if(input.RequestId==Guid.Empty)throw new InvalidOperationException("Form anahtarı geçersiz; sayfayı yeniden açınız.");
        var existing=await db.FsnReceipts.AsNoTracking().FirstOrDefaultAsync(x=>x.RequestId==input.RequestId,ct);
        if(existing!=null){if(existing.WarehouseId!=input.WarehouseId)throw new InvalidOperationException("Bu form anahtarı başka bir firma girişinde kullanılmış.");return existing.ID;}
        if(!await db.FsnWarehouses.AnyAsync(x=>x.ID==input.WarehouseId&&x.IsDelete!=true&&x.IsActive!=false,ct))throw new InvalidOperationException("Geçerli bir fason firma/depo seçiniz.");
        if(input.ReceiptDate.Year<2000||input.ReceiptDate.Date>DateTime.Today||string.IsNullOrWhiteSpace(input.DispatchNumber)||input.DispatchNumber.Trim().Length>100||input.Notes?.Length>500)throw new InvalidOperationException("Giriş tarihi, irsaliye ve açıklama bilgilerini kontrol ediniz.");
        if(input.DispatchDate.HasValue&&(input.DispatchDate.Value.Year<2000||input.DispatchDate.Value.Date>input.ReceiptDate.Date))throw new InvalidOperationException("İrsaliye tarihi giriş tarihinden sonra olamaz.");
        var active=input.Lines.Where(x=>x.MaterialId.HasValue).ToList();
        if(active.Count==0||input.Lines.Count>100)throw new InvalidOperationException("1-100 ürün satırı giriniz.");
        if(input.Lines.Any(x=>!x.MaterialId.HasValue&&(!string.IsNullOrWhiteSpace(x.Quantity)||!string.IsNullOrWhiteSpace(x.LotNumber)||x.UnitId.HasValue||x.ProductionDate.HasValue||x.ExpirationDate.HasValue||!string.IsNullOrWhiteSpace(x.Notes))))throw new InvalidOperationException("Bilgi girilen her satırda malzeme seçiniz.");
        var ids=active.Select(x=>x.MaterialId!.Value).Distinct().ToList();
        var materials=await db.PrdMaterials.AsNoTracking().Where(x=>ids.Contains(x.ID)&&x.IsDelete!=true&&x.IsActive!=false&&(x.Type==PrdMaterialType.RawMaterial||x.Type==PrdMaterialType.Packaging)).ToDictionaryAsync(x=>x.ID,ct);
        var units=await db.PrdUnits.AsNoTracking().Where(x=>x.IsDelete!=true&&x.IsActive!=false).Select(x=>x.ID).ToListAsync(ct);
        var converter=await ProductionStockUnitConverter.LoadAsync(db,ids,ct);var now=DateTime.Now;
        var lines=new List<FsnReceiptLine>();
        foreach(var row in active)
        {
            if(!materials.TryGetValue(row.MaterialId!.Value,out var material))throw new InvalidOperationException("Aktif bir hammadde veya ambalaj seçiniz.");
            var unit=row.UnitId??material.UnitId;
            if(!units.Contains(unit)||!units.Contains(material.UnitId))throw new InvalidOperationException(material.Code+": geçerli bir birim seçiniz.");
            if(!RawMaterialConsumptionService.TryQuantity(row.Quantity,out var quantity)||quantity>999999999999.999999m)throw new InvalidOperationException(material.Code+": miktar sıfırdan büyük ve en fazla 6 ondalıklı olmalıdır.");
            var lot=row.LotNumber?.Trim();
            if(lot?.Length>100||row.Notes?.Length>250||material.RequiresLotTracking&&string.IsNullOrEmpty(lot))throw new InvalidOperationException(material.Code+": lot numarası ve not bilgilerini kontrol ediniz.");
            if(material.RequiresExpirationDate&&!row.ExpirationDate.HasValue)throw new InvalidOperationException(material.Code+": SKT zorunludur.");
            if(row.ProductionDate.HasValue&&(row.ProductionDate.Value.Year<1900||row.ProductionDate.Value.Date>input.ReceiptDate.Date)||row.ExpirationDate.HasValue&&row.ExpirationDate.Value.Year<1900||row.ProductionDate.HasValue&&row.ExpirationDate.HasValue&&row.ExpirationDate.Value.Date<row.ProductionDate.Value.Date)throw new InvalidOperationException(material.Code+": üretim tarihi/SKT bilgilerini kontrol ediniz.");
            var converted=converter.Quantity(material.ID,unit,quantity);
            if(converted>999999999999.999999m)throw new InvalidOperationException(material.Code+": stok miktarı desteklenen aralığı aşıyor.");
            lines.Add(new(){Sequence=lines.Count+1,MaterialId=material.ID,OriginalUnitId=unit,OriginalQuantity=quantity,UnitId=material.UnitId,Quantity=converted,LotNumber=string.IsNullOrEmpty(lot)?"LOTSUZ":lot,ProductionDate=row.ProductionDate?.Date,ExpirationDate=row.ExpirationDate?.Date,Notes=row.Notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=actor});
        }
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var receipt=new FsnReceipt{RequestId=input.RequestId,Number="FHG-"+now.ToString("yyyyMMddHHmmssfff")+"-"+Guid.NewGuid().ToString("N")[..8],WarehouseId=input.WarehouseId!.Value,ReceiptDate=input.ReceiptDate.Date,DispatchNumber=input.DispatchNumber.Trim(),DispatchDate=input.DispatchDate?.Date,Notes=input.Notes?.Trim(),Status=FsnReceiptStatus.Draft,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=actor};
        db.Add(receipt);await db.SaveChangesAsync(ct);foreach(var line in lines)line.ReceiptId=receipt.ID;db.AddRange(lines);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return receipt.ID;
    }

    public async Task PostAsync(int id,string actor,CancellationToken ct=default)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);var receipt=await LockAsync(id,ct);
        if(receipt.Status==FsnReceiptStatus.Posted){await tx.CommitAsync(ct);return;}
        if(receipt.Status!=FsnReceiptStatus.Draft)throw new InvalidOperationException("Yalnızca taslak giriş stoğa alınabilir.");
        if(!await db.FsnWarehouses.AnyAsync(x=>x.ID==receipt.WarehouseId&&x.IsDelete!=true&&x.IsActive!=false,ct))throw new InvalidOperationException("Firma deposu aktif değil.");
        var lines=await db.FsnReceiptLines.Where(x=>x.ReceiptId==id&&x.IsDelete!=true).ToListAsync(ct);
        if(lines.Count==0)throw new InvalidOperationException("Giriş satırı bulunamadı.");
        var ids=lines.Select(x=>x.MaterialId).Distinct().ToList();var converter=await ProductionStockUnitConverter.LoadAsync(db,ids,ct);
        var valid=await db.PrdMaterials.Where(x=>ids.Contains(x.ID)&&x.IsDelete!=true&&x.IsActive!=false&&(x.Type==PrdMaterialType.RawMaterial||x.Type==PrdMaterialType.Packaging)).Select(x=>x.ID).ToListAsync(ct);
        var units=await db.PrdUnits.Where(x=>x.IsDelete!=true&&x.IsActive!=false).Select(x=>x.ID).ToListAsync(ct);
        foreach(var line in lines)
            if(!valid.Contains(line.MaterialId)||!units.Contains(line.OriginalUnitId)||!units.Contains(line.UnitId)||!converter.IsBaseUnitEquivalent(line.MaterialId,line.UnitId)||converter.Quantity(line.MaterialId,line.OriginalUnitId,line.OriginalQuantity)!=line.Quantity)
                throw new InvalidOperationException("Taslak sonrası malzeme, birim veya dönüşüm değişmiş. Taslağı iptal edip yeniden hazırlayınız.");
        var now=DateTime.Now;foreach(var line in lines)db.FsnStockMovements.Add(new(){ReceiptLineId=line.ID,Quantity=line.Quantity,MovementDate=receipt.ReceiptDate,IsReversal=false,Reason=receipt.Notes,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=actor});
        receipt.Status=FsnReceiptStatus.Posted;receipt.PostedDate=now;receipt.PostedUser=actor;receipt.UpdateDate=now;receipt.UpdateUserID=actor;
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }

    public async Task ReverseAsync(int id,string reason,string actor,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(reason)||reason.Trim().Length<5||reason.Length>500)throw new InvalidOperationException("Geri alma gerekçesi 5-500 karakter olmalıdır.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);var receipt=await LockAsync(id,ct);
        if(receipt.Status==FsnReceiptStatus.Reversed){await tx.CommitAsync(ct);return;}
        if(receipt.Status!=FsnReceiptStatus.Posted)throw new InvalidOperationException("Yalnızca stoğa alınmış giriş geri alınabilir.");
        var lines=await db.FsnReceiptLines.Where(x=>x.ReceiptId==id&&x.IsDelete!=true).ToListAsync(ct);var ids=lines.Select(x=>x.ID).ToList();
        var balances=await db.FsnStockMovements.Where(x=>ids.Contains(x.ReceiptLineId)&&x.IsDelete!=true).GroupBy(x=>x.ReceiptLineId).Select(g=>new{Id=g.Key,Balance=g.Sum(x=>x.IsReversal?-x.Quantity:x.Quantity)}).ToDictionaryAsync(x=>x.Id,x=>x.Balance,ct);
        if(lines.Any(l=>!balances.TryGetValue(l.ID,out var quantity)||quantity!=l.Quantity))throw new InvalidOperationException("Giriş bakiyesi doğrulanamadı; geri alınamaz.");
        var now=DateTime.Now;foreach(var line in lines)db.FsnStockMovements.Add(new(){ReceiptLineId=line.ID,Quantity=line.Quantity,IsReversal=true,MovementDate=now,Reason=reason.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=actor});
        receipt.Status=FsnReceiptStatus.Reversed;receipt.ReversedDate=now;receipt.ReversedUser=actor;receipt.ReversalReason=reason.Trim();receipt.UpdateDate=now;receipt.UpdateUserID=actor;
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }

    public async Task CancelAsync(int id,string actor,CancellationToken ct=default)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);var receipt=await LockAsync(id,ct);
        if(receipt.Status==FsnReceiptStatus.Cancelled){await tx.CommitAsync(ct);return;}
        if(receipt.Status!=FsnReceiptStatus.Draft)throw new InvalidOperationException("Yalnızca taslak iptal edilebilir; işlenmiş girişte geri alma kullanınız.");
        receipt.Status=FsnReceiptStatus.Cancelled;receipt.UpdateDate=DateTime.Now;receipt.UpdateUserID=actor;await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    private async Task<FsnReceipt> LockAsync(int id,CancellationToken ct)
    {
        var query=db.Database.IsSqlServer()?db.FsnReceipts.FromSqlInterpolated($"SELECT * FROM dbo.FsnReceipt WITH(UPDLOCK,HOLDLOCK) WHERE ID={id}"):db.FsnReceipts.AsQueryable();
        var latest=await query.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct)??throw new InvalidOperationException("Giriş belgesi bulunamadı.");
        var local=db.FsnReceipts.Local.FirstOrDefault(x=>x.ID==id);if(local!=null){db.Entry(local).CurrentValues.SetValues(latest);db.Entry(local).OriginalValues.SetValues(latest);return local;}
        db.Attach(latest);return latest;
    }

    public IQueryable<FasonHistoryRowVM> History(int? warehouseId=null,int? materialId=null,string? lotNumber=null) =>
        from movement in db.FsnStockMovements.AsNoTracking()
        join line in db.FsnReceiptLines.AsNoTracking() on movement.ReceiptLineId equals line.ID
        join receipt in db.FsnReceipts.AsNoTracking() on line.ReceiptId equals receipt.ID
        join warehouse in db.FsnWarehouses.AsNoTracking() on receipt.WarehouseId equals warehouse.ID
        join material in db.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
        join unit in db.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
        where movement.IsDelete!=true&&line.IsDelete!=true&&receipt.IsDelete!=true&&warehouse.IsDelete!=true&&(!warehouseId.HasValue||warehouse.ID==warehouseId)&&(!materialId.HasValue||material.ID==materialId)&&(lotNumber==null||line.LotNumber==lotNumber)
        select new FasonHistoryRowVM{Id=movement.ID,ReceiptId=receipt.ID,Number=receipt.Number,Date=movement.MovementDate,Warehouse=warehouse.Code+" - "+warehouse.Name,Customer=warehouse.CustomerCode+" - "+warehouse.CustomerName,Material=material.Code+" - "+material.Name,LotNumber=line.LotNumber,Unit=unit.Name,Quantity=movement.Quantity,IsReversal=movement.IsReversal,User=movement.CreateUserID,Reason=movement.Reason,LineNotes=line.Notes,ProductionDate=line.ProductionDate,ExpirationDate=line.ExpirationDate};

    public IQueryable<FasonStockRowVM> Stock(int? warehouseId=null,string? search=null) =>
        from movement in db.FsnStockMovements.AsNoTracking()
        join line in db.FsnReceiptLines.AsNoTracking() on movement.ReceiptLineId equals line.ID
        join receipt in db.FsnReceipts.AsNoTracking() on line.ReceiptId equals receipt.ID
        join warehouse in db.FsnWarehouses.AsNoTracking() on receipt.WarehouseId equals warehouse.ID
        join material in db.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
        join unit in db.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
        where movement.IsDelete!=true&&line.IsDelete!=true&&receipt.IsDelete!=true&&warehouse.IsDelete!=true&&(!warehouseId.HasValue||warehouse.ID==warehouseId)&&(search==null||material.Code.Contains(search)||material.Name.Contains(search)||line.LotNumber.Contains(search)||warehouse.CustomerCode.Contains(search)||warehouse.CustomerName.Contains(search))
        group new{movement,line} by new{WarehouseId=warehouse.ID,Warehouse=warehouse.Code+" - "+warehouse.Name,Customer=warehouse.CustomerCode+" - "+warehouse.CustomerName,MaterialId=material.ID,Material=material.Code+" - "+material.Name,line.LotNumber,line.ProductionDate,line.ExpirationDate,line.UnitId,Unit=unit.Name} into g
        where g.Sum(x=>x.movement.IsReversal?-x.movement.Quantity:x.movement.Quantity)>0
        select new FasonStockRowVM{WarehouseId=g.Key.WarehouseId,Warehouse=g.Key.Warehouse,Customer=g.Key.Customer,MaterialId=g.Key.MaterialId,Material=g.Key.Material,LotNumber=g.Key.LotNumber,ProductionDate=g.Key.ProductionDate,ExpirationDate=g.Key.ExpirationDate,UnitId=g.Key.UnitId,Unit=g.Key.Unit,Received=g.Sum(x=>x.movement.IsReversal?0:x.movement.Quantity),Reversed=g.Sum(x=>x.movement.IsReversal?x.movement.Quantity:0)};
}
