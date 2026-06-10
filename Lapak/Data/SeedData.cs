using Lapak.Models;

namespace Lapak.Data;

public static class SeedData
{
    private static readonly Random _rng = new(42);

    public static void Initialize(LapakDbContext db)
    {
        var electronicsId = Guid.NewGuid(); var fashionId = Guid.NewGuid(); var homeLivingId = Guid.NewGuid();
        var foodBevId = Guid.NewGuid(); var hobbyId = Guid.NewGuid(); var beautyId = Guid.NewGuid();
        var sportsId = Guid.NewGuid(); var automotiveId = Guid.NewGuid();

        var categories = new List<Category>
        {
            new(){Id=electronicsId,Name="Elektronik",Slug="elektronik",SortOrder=1},new(){Id=fashionId,Name="Fashion",Slug="fashion",SortOrder=2},
            new(){Id=homeLivingId,Name="Rumah & Kehidupan",Slug="rumah-kehidupan",SortOrder=3},new(){Id=foodBevId,Name="Makanan & Minuman",Slug="makanan-minuman",SortOrder=4},
            new(){Id=hobbyId,Name="Hobi & Koleksi",Slug="hobi-koleksi",SortOrder=5},new(){Id=beautyId,Name="Kecantikan",Slug="kecantikan",SortOrder=6},
            new(){Id=sportsId,Name="Olahraga",Slug="olahraga",SortOrder=7},new(){Id=automotiveId,Name="Otomotif",Slug="otomotif",SortOrder=8},
        };
        db.Categories.AddRange(categories);

        var subCats = new List<Category>
        {
            new(){Name="Smartphone",Slug="smartphone",ParentCategoryId=electronicsId},new(){Name="Laptop & PC",Slug="laptop-pc",ParentCategoryId=electronicsId},
            new(){Name="Tablet",Slug="tablet",ParentCategoryId=electronicsId},new(){Name="Audio & Headphone",Slug="audio-headphone",ParentCategoryId=electronicsId},
            new(){Name="Kamera",Slug="kamera",ParentCategoryId=electronicsId},new(){Name="Aksesoris Elektronik",Slug="aksesoris-elektronik",ParentCategoryId=electronicsId},
            new(){Name="Pakaian Pria",Slug="pakaian-pria",ParentCategoryId=fashionId},new(){Name="Pakaian Wanita",Slug="pakaian-wanita",ParentCategoryId=fashionId},
            new(){Name="Sepatu",Slug="sepatu",ParentCategoryId=fashionId},new(){Name="Tas",Slug="tas",ParentCategoryId=fashionId},
            new(){Name="Jam Tangan",Slug="jam-tangan",ParentCategoryId=fashionId},new(){Name="Furniture",Slug="furniture",ParentCategoryId=homeLivingId},
            new(){Name="Dapur",Slug="dapur",ParentCategoryId=homeLivingId},new(){Name="Dekorasi",Slug="dekorasi",ParentCategoryId=homeLivingId},
            new(){Name="Makanan Ringan",Slug="makanan-ringan",ParentCategoryId=foodBevId},new(){Name="Minuman",Slug="minuman",ParentCategoryId=foodBevId},
            new(){Name="Bahan Masakan",Slug="bahan-masakan",ParentCategoryId=foodBevId},new(){Name="Buku",Slug="buku",ParentCategoryId=hobbyId},
            new(){Name="Mainan & Game",Slug="mainan-game",ParentCategoryId=hobbyId},new(){Name="Alat Musik",Slug="alat-musik",ParentCategoryId=hobbyId},
            new(){Name="Skincare",Slug="skincare",ParentCategoryId=beautyId},new(){Name="Makeup",Slug="makeup",ParentCategoryId=beautyId},
            new(){Name="Parfum",Slug="parfum",ParentCategoryId=beautyId},new(){Name="Fitness",Slug="fitness",ParentCategoryId=sportsId},
            new(){Name="Sepatu Olahraga",Slug="sepatu-olahraga",ParentCategoryId=sportsId},new(){Name="Aksesoris Mobil",Slug="aksesoris-mobil",ParentCategoryId=automotiveId},
            new(){Name="Aksesoris Motor",Slug="aksesoris-motor",ParentCategoryId=automotiveId},
        };
        db.Categories.AddRange(subCats);

        var storeNames = new[]{"GadgetZone Official","Fashionista ID","Rumah Impian Store","Dapur Mama","BookWorm Indonesia","BeautyGlow Official","Sportivo","Otomotif Keren","Kids Paradise","AudioPro ID","Koleksi Jam Mewah","Toserba Murah Meriah"};
        var scities = new[]{"Jakarta Pusat","Bandung","Surabaya","Yogyakarta","Jakarta Selatan","Tangerang","Bekasi","Semarang","Medan","Jakarta Barat","Denpasar","Makassar"};
        var sprovs = new[]{"DKI Jakarta","Jawa Barat","Jawa Timur","DI Yogyakarta","DKI Jakarta","Banten","Jawa Barat","Jawa Tengah","Sumatera Utara","DKI Jakarta","Bali","Sulawesi Selatan"};
        var stores = new List<Store>();
        for(int i=0;i<storeNames.Length;i++){
            var s=new Store{Name=storeNames[i],Slug=storeNames[i].ToLower().Replace(" ","-"),City=scities[i],Province=sprovs[i],IsVerified=i<8,VerificationStatus=i<8?"Verified":"Pending",AverageRating=Math.Round(3.5+_rng.NextDouble()*1.5,1),RatingCount=_rng.Next(10,500),TotalSales=_rng.Next(50,2000),IsActive=true,CreatedAt=DateTime.UtcNow.AddDays(-_rng.Next(30,365))};
            db.Stores.Add(s);stores.Add(s);
        }

        var userNames = new[]{"Budi Santoso","Siti Nurhaliza","Ahmad Rizki","Dewi Lestari","Eko Prasetyo","Fitri Handayani","Gunawan Wibisono","Hana Amelia","Irfan Hakim","Jessica Putri","Kevin Anggara","Linda Kusuma","Muhammad Reza","Nadia Safira","Oscar Darmawan","Putri Ayu","Rangga Febrian","Sari Indah","Tono Wijaya","Umar Bakri","Vina Melinda","Wahyu Nugroho","Xenia Putri","Yusuf Hamdan","Zahra Aulia","Admin Lapak","Rina Marlina","Doni Saputra","Mega Wati","Fajar Aditya"};
        var ucs=new[]{"Jakarta","Bandung","Surabaya","Yogyakarta","Medan","Semarang","Bekasi","Tangerang"};
        var ups=new[]{"DKI Jakarta","Jawa Barat","Jawa Timur","DI Yogyakarta","Sumatera Utara","Jawa Tengah"};
        var users = new List<User>();
        for(int i=0;i<userNames.Length;i++){
            var u=new User{UserName=userNames[i].ToLower().Replace(" ",".")+"@lapak.com",Email=userNames[i].ToLower().Replace(" ",".")+"@lapak.com",NormalizedEmail=userNames[i].ToUpper().Replace(" ",".")+"@LAPAK.COM",NormalizedUserName=userNames[i].ToUpper().Replace(" ",".")+"@LAPAK.COM",FullName=userNames[i],PhoneNumber="08"+_rng.Next(100000000,999999999),Address="Jl. Merdeka No. "+_rng.Next(1,200),City=ucs[_rng.Next(8)],Province=ups[_rng.Next(6)],PostalCode=_rng.Next(10000,99999).ToString(),UserType=i==25?"Admin":"Buyer",IsActive=true,EmailConfirmed=true,CreatedAt=DateTime.UtcNow.AddDays(-_rng.Next(1,400)),LastLoginAt=DateTime.UtcNow.AddDays(-_rng.Next(0,7)),Score=_rng.Next(0,1500),TotalTransactions=_rng.Next(0,100),TotalTransactionValue=_rng.Next(0,50000000),LoyaltyPoints=_rng.Next(0,5000)};
            u.Tier=u.Score>=1000?"Platinum":(u.Score>=500?"Gold":(u.Score>=100?"Silver":"Bronze"));
            db.Users.Add(u);users.Add(u);
        }
        for(int i=0;i<stores.Count&&i<users.Count;i++){stores[i].UserId=users[i].Id;users[i].Store=stores[i];users[i].UserType="Seller";}

        var products = new List<Product>();
        var prodDefs = new (string n,string s,int c,int st,decimal p,decimal? op,int stk,int w)[]{
            ("iPhone 15 Pro Max","iphone-15-pro-max",0,0,21999000,24999000,25,220),("Samsung Galaxy S24 Ultra","samsung-galaxy-s24-ultra",0,0,18999000,20999000,35,230),
            ("Xiaomi 14 Pro","xiaomi-14-pro",0,0,10999000,null,50,210),("Realme GT 5","realme-gt-5",0,1,6499000,null,60,200),
            ("MacBook Pro 14 M3 Pro","macbook-pro-14-m3-pro",1,0,29999000,32999000,15,1600),("ASUS ROG Zephyrus G14","asus-rog-zephyrus-g14",1,0,21999000,null,10,1700),
            ("Lenovo ThinkPad X1 Carbon","lenovo-thinkpad-x1-carbon",1,1,25999000,28999000,8,1120),("Acer Aspire 5 Slim","acer-aspire-5-slim",1,1,8499000,9999000,40,1800),
            ("Sony WH-1000XM5","sony-wh-1000xm5",3,9,4999000,5999000,30,250),("AirPods Pro 2nd Gen","airpods-pro-2nd-gen",3,0,3499000,null,45,50),
            ("JBL Flip 7","jbl-flip-7",3,9,1499000,null,50,550),("Sony A7 IV","sony-a7-iv",4,0,34999000,null,5,650),
            ("DJI Osmo Pocket 3","dji-osmo-pocket-3",4,0,7999000,null,12,180),("Kemeja Batik Premium","kemeja-batik-premium",6,1,599000,null,100,300),
            ("Jaket Denim Classic","jaket-denim-classic",6,1,899000,1199000,60,800),("Celana Chino Slim Fit","celana-chino-slim-fit",6,1,349000,null,150,400),
            ("Dress Batik Modern","dress-batik-modern",7,1,459000,599000,80,350),("Blouse Satin Elegan","blouse-satin-elegan",7,1,299000,null,120,250),
            ("Nike Air Jordan 1 Retro","nike-air-jordan-1-retro",8,1,3499000,null,10,900),("Adidas Ultraboost Light","adidas-ultraboost-light",8,6,2499000,2999000,40,300),
            ("Converse Chuck Taylor 70","converse-chuck-taylor-70",8,1,1299000,null,60,800),("Tas Ransel Eiger Premium","tas-ransel-eiger-premium",9,6,599000,799000,35,1200),
            ("Seiko Prospex Diver","seiko-prospex-diver",10,10,5499000,null,8,180),("Casio G-Shock GA-2100","casio-g-shock-ga-2100",10,10,1799000,null,20,60),
            ("Kursi Kantor Ergonomis","kursi-kantor-ergonomis",11,2,2499000,null,20,15000),("Meja Belajar Minimalis","meja-belajar-minimalis",11,2,1299000,1599000,15,12000),
            ("Wajan Anti Lengket Set","wajan-anti-lengket-set",12,3,799000,999000,40,5000),("Blender Philips 3in1","blender-philips-3in1",12,3,699000,null,30,3000),
            ("Serum Vitamin C 20%","serum-vitamin-c-20",20,5,159000,null,200,30),("Sunscreen SPF 50","sunscreen-spf-50",20,5,129000,169000,180,50),
            ("Cushion Foundation Flawless","cushion-foundation-flawless",21,5,249000,null,100,80),("Lip Cream Matte Set","lip-cream-matte-set",21,5,179000,249000,150,40),
            ("Parfum Premium Woody","parfum-premium-woody",22,5,399000,599000,50,300),("Kopi Arabika Gayo","kopi-arabika-gayo",15,3,89000,null,200,250),
            ("Matcha Ceremonial Grade","matcha-ceremonial-grade",15,3,249000,299000,60,100),("Atomic Habits - James Clear","atomic-habits-james-clear",17,4,129000,169000,100,300),
            ("Filosofi Teras","filosofi-teras",17,4,89000,null,150,250),("Dumbbell Set Adjustable 24kg","dumbbell-set-adjustable-24kg",23,6,1499000,1999000,20,12000),
            ("Yoga Mat Premium TPE","yoga-mat-premium-tpe",23,6,349000,null,60,900),("Dashcam Dual Camera 4K","dashcam-dual-camera-4k",25,7,1299000,1799000,25,300),
            ("Car Phone Holder Magnetic","car-phone-holder-magnetic",25,7,89000,null,100,100),("Samsung Galaxy Buds3 Pro","samsung-galaxy-buds3-pro",3,0,2799000,3199000,35,50),
            ("Keripik Singkong Pedas","keripik-singkong-pedas",14,3,35000,null,500,250),("Rendang Daging Sapi Premium","rendang-daging-sapi-premium",14,3,159000,199000,40,500),
            ("Oppo Find N3 Flip","oppo-find-n3-flip",0,0,13999000,15999000,20,190),("Canon EOS R6 Mark II","canon-eos-r6-mark-ii",4,9,42999000,45999000,3,680),
            ("Fossil Minimalist Slim","fossil-minimalist-slim",10,10,1499000,1999000,15,50),("Rak Buku 4 Susun","rak-buku-4-susun",11,2,899000,null,25,8000),
            ("Set Pisau Dapur Chef","set-pisau-dapur-chef",12,3,549000,749000,35,2500),("Moisturizer Ceramide Cream","moisturizer-ceramide-cream",20,5,199000,null,150,100),
            ("Eyeshadow Palette 16 Colors","eyeshadow-palette-16-colors",21,5,299000,null,60,200),
        };
        foreach(var def in prodDefs){
            var dpct=def.op.HasValue&&def.op.Value>def.p?(int)((1-def.p/def.op.Value)*100):0;
            var p=new Product{Name=def.n,Slug=def.s,ShortDescription=def.n,Description="Produk berkualitas: "+def.n,Price=def.p,OriginalPrice=def.op,DiscountPercentage=dpct>0?dpct:null,Stock=def.stk,MinOrder=1,MaxOrder=10,StockStatus=def.stk>10?"Available":(def.stk>0?"LowStock":"OutOfStock"),CategoryId=subCats[def.c].Id,StoreId=stores[def.st].Id,WeightInGrams=def.w,IsFeatured=_rng.Next(10)<3,IsActive=true,AverageRating=Math.Round(3.0+_rng.NextDouble()*2.0,1),RatingCount=_rng.Next(5,200),SoldCount=_rng.Next(0,500),LikeCount=_rng.Next(10,1000),ViewCount=_rng.Next(100,5000),CreatedAt=DateTime.UtcNow.AddDays(-_rng.Next(5,200))};
            db.Products.Add(p);products.Add(p);stores[def.st].TotalProducts++;
        }

        db.Vouchers.AddRange(new[]{new Voucher{Code="WELCOME20",Name="Welcome Bonus 20%",Type="Percentage",Value=20,MaxDiscount=100000,MinPurchase=150000,MaxUsage=2000,IsActive=true,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow.AddMonths(6)},new Voucher{Code="HEMAT100",Name="Hemat Rp100.000",Type="Fixed",Value=100000,MinPurchase=1000000,MaxUsage=1000,IsActive=true,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow.AddMonths(3)},new Voucher{Code="FREESHIP",Name="Gratis Ongkir",Type="Shipping",Value=30000,MinPurchase=100000,MaxUsage=5000,IsActive=true,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow.AddMonths(1)},new Voucher{Code="GOLD25",Name="Gold Member 25%",Type="Percentage",Value=25,MaxDiscount=250000,MinPurchase=500000,TargetTier="Gold",MaxUsage=500,IsActive=true,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow.AddMonths(2)},new Voucher{Code="BAYARDIKIT",Name="Cashback 15%",Type="Percentage",Value=15,MaxDiscount=50000,MinPurchase=50000,MaxUsage=3000,IsActive=true,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow.AddMonths(4)},new Voucher{Code="MERDEKA",Name="Promo Kemerdekaan",Type="Percentage",Value=17,MaxDiscount=170000,MinPurchase=200000,MaxUsage=1945,IsActive=true,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow.AddDays(30)}});

        for(int i=0;i<8&&i<products.Count;i++)
            db.ProductPromos.Add(new ProductPromo{Name=$"Flash Sale {i+1}",Type=i%3==0?"Discount":(i%3==1?"Cashback":"BuyOneGetOne"),Value=(i+1)*10,StartDate=DateTime.UtcNow.AddDays(-_rng.Next(0,3)),EndDate=DateTime.UtcNow.AddDays(_rng.Next(1,14)),IsActive=true,ProductId=products[i].Id});

        // Orders
        var oss=new[]{"Pending","Paid","Processing","Shipped","Delivered","Completed"};
        var crs=new[]{"JNE","J&T","SiCepat","Pos Indonesia"};
        var maxBuyer=Math.Min(13,users.Count);
        for(int i=0;i<30;i++){
            var rp=products[_rng.Next(products.Count)];var bi=_rng.Next(maxBuyer);var st=oss[_rng.Next(6)];
            var co=crs[_rng.Next(4)];var qty=_rng.Next(1,4);var price=rp.Price;var sub=price*qty;
            var ship=_rng.Next(10000,50000);var disc=_rng.Next(0,20000);var od=DateTime.UtcNow.AddDays(-_rng.Next(0,90));
            var o=new Order{OrderNumber=$"LPK-{od:yyMMdd}-{i+1:D4}",Status=st,PaymentStatus=st=="Pending"?"Unpaid":"Paid",PaymentMethod=_rng.Next(2)==0?"bank_transfer":"ewallet",PaymentGateway="Midtrans",PaymentTransactionId=$"MT-{Guid.NewGuid():N}"[..16],SubTotal=sub,ShippingCost=ship,Discount=disc,Tax=(sub-disc)*0.11m,GrandTotal=sub+ship-disc+(sub-disc)*0.11m,ShippingCourier=co,ShippingService="REG",TrackingNumber=$"{co}{od:yyMMdd}{_rng.Next(10000,99999)}",ShippingAddress=$"Jl. Pahlawan No. {_rng.Next(1,200)}",ShippingCity=users[bi].City,ShippingProvince=users[bi].Province,UserId=users[bi].Id,StoreId=rp.StoreId,CreatedAt=od,PaidAt=st!="Pending"?od.AddHours(_rng.Next(1,24)):null,ShippedAt=st is"Shipped"or"Delivered"or"Completed"?od.AddDays(_rng.Next(1,3)):null,DeliveredAt=st is"Delivered"or"Completed"?od.AddDays(_rng.Next(3,7)):null,CompletedAt=st=="Completed"?od.AddDays(_rng.Next(7,14)):null};
            db.Orders.Add(o);db.OrderItems.Add(new OrderItem{OrderId=o.Id,ProductId=rp.Id,Quantity=qty,Price=price,SubTotal=sub,CreatedAt=od});
            if(st is"Shipped"or"Delivered"or"Completed"){var ts=new[]{"PICKUP","IN_TRANSIT","IN_TRANSIT","WITH_COURIER","DELIVERED"};for(int t=0;t<Math.Min(5,(st=="Shipped"?3:5));t++)db.ShippingTrackings.Add(new ShippingTracking{OrderId=o.Id,Status=ts[t],Description=ts[t]switch{"PICKUP"=>"Paket diambil kurir","IN_TRANSIT"=>"Paket dalam perjalanan","WITH_COURIER"=>"Paket sedang diantar","DELIVERED"=>"Paket diterima",_=>"Update"},Location=t<3?"Jakarta":(users[bi].City??"Jakarta"),EventDate=od.AddHours(t*_rng.Next(6,24))});}
            users[bi].TotalTransactions++;users[bi].TotalTransactionValue+=o.GrandTotal;
        }

        // Reviews - with dedup to avoid unique constraint violation
        var comments=new[]{"Produk bagus! Sesuai deskripsi.","Kualitas oke, harga terjangkau.","Barang sesuai, packing aman.","Suka banget! Recommended seller.","Standard sesuai harga.","Keren! Melebihi ekspektasi.","Agak kecewa dikit tapi overall ok.","Repeat order! Langganan nih.","Product original, pengiriman cepat.","Excellent quality premium!"};
        var usedPR = new HashSet<string>();
        for(int i=0;i<40;i++){
            var rp=products[_rng.Next(products.Count)];var ri=_rng.Next(maxBuyer);
            var key=$"{users[ri].Id}_{rp.Id}";if(usedPR.Contains(key))continue;usedPR.Add(key);
            var rat=_rng.Next(1,6);
            db.ProductReviews.Add(new ProductReview{ProductId=rp.Id,UserId=users[ri].Id,Rating=rat,Comment=comments[_rng.Next(10)],CreatedAt=DateTime.UtcNow.AddDays(-_rng.Next(1,60))});
            rp.RatingCount++;rp.AverageRating=Math.Round((rp.AverageRating*(rp.RatingCount-1)+rat)/rp.RatingCount,1);
        }
        var usedSR = new HashSet<string>();
        for(int i=0;i<20;i++){
            var rs=stores[_rng.Next(stores.Count)];var ri=_rng.Next(maxBuyer);
            var key=$"{users[ri].Id}_{rs.Id}";if(usedSR.Contains(key))continue;usedSR.Add(key);
            var rat=_rng.Next(3,6);
            db.StoreReviews.Add(new StoreReview{StoreId=rs.Id,UserId=users[ri].Id,Rating=rat,Comment=comments[_rng.Next(10)],CreatedAt=DateTime.UtcNow.AddDays(-_rng.Next(1,90))});
            rs.RatingCount++;rs.AverageRating=Math.Round((rs.AverageRating*(rs.RatingCount-1)+rat)/rs.RatingCount,1);
        }

        db.SaveChanges();
    }
}
