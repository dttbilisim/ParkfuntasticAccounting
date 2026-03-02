using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
namespace ecommerce.Core.Utils{
    public enum EntityStatus{
        [Display(Description = "Pasif")] Passive = 0,
        [Display(Description = "Aktif")] Active = 1,
        [Display(Description = "Silindi")] Deleted = 99,
        [Display(Description = "Kullanılmayan")]
        NotUse = 100,
    }
    public enum EntityStatusForFilter{
        [Display(Description = "Pasif")] Passive = 0,
        [Display(Description = "Aktif")] Active = 1,
    }
    public enum MembershipType : byte{
        [Display(Description = "Eczacı")] Pharmacy = 1,
        [Display(Description = "Ecza Deposu")] PharmacyWarehouse = 2
    }
    public enum ProductSellerItemFilter{
        ExprationDateUp, ExprationDateDown, StockUp, PriceUp, PriceDown, HighPrice, MaxSellCount, ProductId, CreatedDate, ProductName, IsFuture, Updated,
        StockDown
    }
    public enum CompanyWorkingType : byte{
        [Display(Description = "Hiçbiri")] None = 0,
        [Display(Description = "Satıcı")] Seller = 1,
        [Display(Description = "Satıcı ve Alıcı")] BuyerAndSeller = 2,
        [Display(Description = "Alıcı")] Buyer = 3
       
    }
    public enum CustomerWorkingTypeEnum : byte{
        [Display(Description = "Peşin")] Pesin = 1,
        [Display(Description = "Vadeli")] Vadeli = 2,
         [Display(Description = "Peşin & Vadeli")] PesinAndVadeli = 3
    }
    public enum NotificationTypeList : byte{
        [Display(Description = "Hiçbiri")] None = 0,
        [Display(Description = "E-Mail")] EmailNotification = 1,
        [Display(Description = "SMS")] SmsNotification = 2
    }
    public enum UserType : byte{
        [Display(Description = "Hiçbiri")] None = 0,
        [Display(Description = "Satıcı")] Seller = 1,
        [Display(Description = "İş Ortağı")] BusinessPartner = 2,
        [Display(Description = "Müşteri")] Custormer = 3
    }
    public enum WebUserType : byte{
        [Display(Description = "Yeni Üye")] B2C = 1,
        [Display(Description = "Tedarikçi")] B2B = 2,
        [Display(Description = "Mobile")] MOBILE = 3,
    }
    public enum ScaleType{
        [Display(Name = "Hiçbiri")] None = 0,
        [Display(Name = "Ölçek")] Scale = 1,
        [Display(Name = "Tablet")] Tablet = 2
    }

    public enum ExpenseOperationType
    {
        [Display(Name = "Gider")]
        Gider = 1,
        [Display(Name = "Gelir")]
        Gelir = 2
    }
    public enum BankPaymentType
    {
        [Display(Name = "Nakit")]
        Nakit = 1,
        [Display(Name = "Kredi Kartı")]
        KrediKarti = 2,
        [Display(Name = "Havale/EFT")]
        HavaleEFT = 3,
        [Display(Name = "Çek")]
        Cek = 4
    }
    public enum ProductFileType{
        [Display(Name = "Hiçbiri")] None = 0,
        [Display(Name = "Ana Görsel")] MainImage = 1,
        [Display(Name = "Detay Görsel")] DetailImage = 1,
    }
    public enum UploadFileType : byte{
        None = 0,
        Product = 1,
        Discount = 2,
        StaticPage = 3
    }
    public enum DiscountRuleDefinationItemKey{
        None = 0,
        ProductDataSet = 1,
        CategoryDataSet = 2,
        SellerDataSet = 3,
        PharmacySegment = 4
    }
    public enum DiscountRuleType{
        None = 0,
        Arithmetic = 1,
        ProductDataSet = 2,
        CategoryDataSet = 3,
        SellerDataSet = 4,
        DayMonthYearBeginEndDateDaysOfWeek = 5,
        LastXDayPayY = 6,
        LastXDayYOrder = 7,
        PharmacySegment = 8
    }
    public enum DiscountRuleInputType{
        None = 0,
        Text = 1,
        Dropdown = 2,
        DateTime = 3
    }
    public enum DiscountRuleStatus{
        None = 0,
        Published = 1,
        Passive = 2,
    }
    public enum DiscountChangeType{
        BasketAmount = 1,
        Product = 2,
        Category = 3,
        Seller = 4,
        Brand = 5,
        DayAndAmount = 6,
        SellerForBasketAmount = 7,
        SellerForBasketAmountForBasketAmount = 7
    }
    public enum DiscountRuleFieldId{
        BasketAmount = 1,
        Product = 2,
        Category = 3,
        Seller = 4,
        LastXDayYPriceOrder = 6,
        LastXDayYPieceOrder = 7,
        Brand = 19,
        XProductYPriceDiscount = 20,
        XProductYPercentageDiscount = 21,
        XProductYGiftProduct = 22,
    }
    public enum DiscountRuleField{
        BasketAmount = 1,
        Product = 2,
        Category = 3,
        Seller = 4,
        LastXDayYPriceOrder = 5,
        LastXDayYPieceOrder = 6,
        Brand = 7,
        XProductYGiftProduct = 8,
    }
    /// <summary>
    /// CouponRuleField tablosundaki verilerin ID karşılıklarıdır.
    /// </summary>
    public enum CouponRuleFieldId{
        BasketAmount = 2,
        Product = 3,
        Category = 4,
        Seller = 5,
        LastXDayYPriceOrder = 6,
        LastXDayYPieceOrder = 7,
        Brand = 8,
        XProductYPriceDiscount = 9,
        XProductYPercentageDiscount = 10,
        XProductYGiftProduct = 11,
    }
    public enum BannerType{
        [Display(Description = "Banner")] Banner = 0,
        [Display(Description = "Dönemsel Fırsatlar")]
        AnlikFirsatlar = 1,
        [Display(Description = "Fırsatı Yakalayın-1")]
        FirsatiYakalayin1 = 2,
        [Display(Description = "Fırsatı Yakalayın-2")]
        FirsatiYakalayin2 = 3,
        [Display(Description = "Hemen Alın")] HemenAlin = 4,
        [Display(Description = "Fırsatı Yakalayın-3")]
        FirsatiYakalayin3 = 5,
        [Display(Description = "Mağazaya Git")]
        MagazayaGit = 6,
        [Display(Description = "Her Gün Büyüyen Satış Partnerlerimiz")]
        HerGunBuyuyenSatisPartnerlerimiz = 7,
        [Display(Description = "Takvim Slider")]
        CalendarSlider = 8,
        [Display(Description = "Login Ekranı Slider")]
        LoginSlider = 9,
        [Display(Description = "Signup Ekranı Slider")]
        SignupSlider = 10,
        [Display(Description = "Hızlı Destek Ekranı Slider")]
        SupportLineSlider = 11,
        [Display(Description = "İş Ortaklarımız")]
        IsOrtaklarimiz = 12,
        [Display(Description = "Ana Sayfa Header")]
        MainPageHeader = 13,
        [Display(Description = "Loginsiz Alan Slider")]
        LandingPageSlider = 14
    }
    public enum DiscountType{
        [Display(Description = "Sepete Atanan")]
        AssignedToCart = 1,
        [Display(Description = "Ürünlere Atanan")]
        AssignedToProducts = 2,
        [Display(Description = "Kategorilere Atanan")]
        AssignedToCategories = 3,
        [Display(Description = "Markalara Atanan")]
        AssignedToBrands = 4,
        // [Display(Description = "Satıcılara Atanan")]
        // AssignedToSellers = 5,
        [Display(Description = "Kargoya Atanan")]
        AssignedToCargo = 6
    }
    public enum DiscountLimitationType{
        [Display(Description = "Sınırsız")] Unlimited = 0,
        [Display(Description = "N Kere")] NTimesOnly = 1,
        [Display(Description = "Müşteri Başına N Kere")]
        NTimesPerCustomer = 2
    }
    public enum OrderStatusType{
        [Display(Description = "Yeni Sipariş")]
        OrderNew = 1,
        [Display(Description = "Hazırlanıyor")]
        OrderPrepare = 2,
        [Display(Description = "Sorunlu Siparişler")]
        OrderProblem = 3,
        [Display(Description = "Onay Bekleyenler")]
        OrderWaitingApproval = 4,
        [Display(Description = "Ödeme Bekliyor")]
        OrderWaitingPayment = 5,
        [Display(Description = "Kargodakiler")]
        OrderinCargo = 6,
        [Display(Description = "İptaller")] OrderCanceled = 7,
        [Display(Description = "Tamamlananlar")]
        OrderSuccess = 8,
        [Display(Description = "Ödeme Alındı")]
        PaymentSuccess = 10
    }
    public enum OrderPlatformType : byte{
        [Display(Description = "Pazaryeri")]
        Marketplace = 1,
        [Display(Description = "B2B")]
        B2B = 2,
        [Display(Description = "Mobil")]
        Mobile = 3
    }
    /// <summary>Kurye başvurusu durumu (Kuryem Olur musun modülü).</summary>
    public enum CourierApplicationStatus : byte
    {
        [Display(Description = "Beklemede")] Pending = 0,
        [Display(Description = "Onaylandı")] Approved = 1,
        [Display(Description = "Reddedildi")] Rejected = 2,
    }
    /// <summary>Kurye teslimat durumu (sipariş atandıktan sonra).</summary>
    public enum CourierDeliveryStatus : byte
    {
        [Display(Description = "Atama Bekliyor")] PendingAssignment = 0,
        [Display(Description = "Atandı")] Assigned = 1,
        [Display(Description = "Kabul Edildi")] Accepted = 2,
        [Display(Description = "Satıcıdan Alındı")] PickedUp = 3,
        [Display(Description = "Yolda")] OnTheWay = 4,
        [Display(Description = "Teslim Edildi")] Delivered = 5,
        [Display(Description = "İptal")] Cancelled = 6,
    }
    /// <summary>Teslimat seçeneği tipi (sepet/checkout: kargo vs kurye).</summary>
    public enum DeliveryOptionType : byte
    {
        [Display(Description = "Kargo")] Cargo = 0,
        [Display(Description = "Kurye")] Courier = 1,
    }

    /// <summary>Kurye araç tipi — hizmet bölgesi araç bazlı tanımlanır.</summary>
    public enum CourierVehicleType : byte
    {
        [Display(Description = "Motosiklet")] Motosiklet = 0,
        [Display(Description = "Bisiklet")] Bisiklet = 1,
        [Display(Description = "Otomobil")] Otomobil = 2,
        [Display(Description = "Kamyonet")] Kamyonet = 3,
    }
    /// <summary>Kargo tipi — standart (Yurtiçi, Sendeo, MNG) vs Bicops Express (kurye teslimatı). İleride depo bazlı kargo için genişletilebilir.</summary>
    public enum CargoType : byte
    {
        [Display(Description = "Standart Kargo")] Standard = 0,
        [Display(Description = "Hızlı Kargo Bicops Express")] BicopsExpress = 1,
    }
    public enum OrderProblemStatus{
        [Display(Description = "İade Gönderisi Oluşturuluyor")]
        CreatingReturnShipment = 0,
        [Display(Description = "İptal Onayı Bekliyor")]
        WaitingCancelApproval = 1,
        [Display(Description = "Kargoya Verilmesi Bekleniyor")]
        WaitingCargoShipment = 2,
        [Display(Description = "Satıcıya Ulaşması Bekleniyor")]
        WaitingCargoDelivery = 3,
        [Display(Description = "Satıcı Onayı Bekliyor")]
        WaitingReturnApproval = 4,
        [Display(Description = "İşlem Reddedildi")]
        Rejected = 5,
        [Display(Description = "Ücret İadesi Bekleniyor")]
        WaitingRefund = 6,
        [Display(Description = "İşlem Tamamlandı")]
        Completed = 7,
        [Display(Description = "İşlem İptal Edildi")]
        Cancelled = 10
    }
    public enum StaticPageType : byte{
        [Display(Description = "Hiçbiri")] None = 0,
        [Display(Description = "Hakkımızda")] AboutUs = 1,
        [Display(Description = "S.S.S")] FrequentlyAskedQuestions = 2,
        [Display(Description = "Kullanım Koşulları")]
        TermsOfUse = 3,
        [Display(Description = "Kargo ve İade")]
        CargoAndReturn = 4,
        [Display(Description = "Sosyal Medya")]
        SocialMedia = 5,
        [Display(Description = "İletişim")] Contact = 6,
        [Display(Description = "Nasıl Başvuru Yapabilirim")]
        HowCanIApply = 7,
        [Display(Description = "Satışa Yasaklı Ürünler")]
        BannedProducts = 8,
    }
    public enum SSSAndBlogGroup : byte{
        [Display(Description = "Hiçbiri")] None = 0,
        [Display(Description = "Blog")] Blog = 1,
        [Display(Description = "S.S.S")] SSS = 2,
        [Display(Description = "Sipariş İptal Nedenleri")]
        OrderCancelStatusType = 3,
        [Display(Description = "Sipariş İade Nedenleri")]
        OrderReturnStatusType = 4,
        [Display(Description = "Müşteri Hizmetleri Konuları")]
        SupportLine = 5,
        [Display(Description = "İletişim Formu")]
        ContactLine = 10,
    }
    public enum ProductSort{
        [Display(Description = "Öne Çıkarılanlar")]
        Highlighted = 0,
        [Display(Description = "En Uygunlar")] BestPrice = 1,
        [Display(Description = "Çok Satanlar")]
        BestSelling = 2,
        [Display(Description = "Fiyata Göre Artan")]
        PriceAsc = 3,
        [Display(Description = "Ürün Adına Göre")]
        NameAsc = 4,
    }
    public enum ProductFilter{
        [Display(Description = "A-Z ye")] productNameAsc = 2,
        [Display(Description = "Z-A ya")] productNamedesc = 3,
        [Display(Description = "Fiyata Göre Artan")]
        ByPriceAsc = 0,
        [Display(Description = "Fiyata Göre Azalan")]
        ByPriceDesc = 1,
        [Display(Description = "Artan Stok")] ByStockAsc = 4,
        [Display(Description = "Azalan Stok")] ByStockDesc = 5,
        [Display(Description = "Yeni Eklenenler")]
        ByCreationDateAsc = 6,
    }
    public enum PointTransactionType{
        [Display(Description = "Siparişte Satıcı Değerlendirmesi")]
        OrderInPoint = 1
    }
    public enum EditorialContentType{
        [Display(Description = "Makale")]
        [EnumMember(Value = "article")]
        Article = 0,
        [Display(Description = "Canlı Yayın")]
        [EnumMember(Value = "live-stream")]
        Video = 1
    }
    public enum SupportLinereturnType{
        [Display(Description = "Email İle Dönüş Yapılsın")]
        Email = 0,
        [Display(Description = "Sms İle Dönüş Yapılsın")]
        Sms = 1,
        [Display(Description = "Telefon ile Aranmak İstiyorum")]
        Telefon = 2,
    }
    public enum SupportLineType
    {
        [Display(Description = "İletişim Formu")]
        [EnumMember(Value = "contact")]
        ContactForm = 0,

        [Display(Description = "Tavsiye / Parça Önerisi")]
        [EnumMember(Value = "part-advice")]
        PartAdvice = 1,

        [Display(Description = "Sipariş Destek Talebi")]
        [EnumMember(Value = "order-support")]
        OrderSupport = 3,

        [Display(Description = "Stok ve Fiyat Bilgisi Talebi")]
        [EnumMember(Value = "stock-price")]
        StockPriceRequest = 4,

        [Display(Description = "Garanti / İade Talebi")]
        [EnumMember(Value = "warranty-return")]
        WarrantyReturn = 5
    }
    public enum PopupTrigger{
        [Display(Description = "Otomatik")] Auto = 0,
        [Display(Description = "Tıklama")] Click = 1
    }
    public enum SendMail{
        [Display(Description = "Gönderildi")] Evet = 1,
        [Display(Description = "Gönderilmedi")]
        Hayir = 0
    }
    public enum PageFilterEnum{
        [Display(Description = "Miadı 8 Aydan Uzunlar")]
        Miat8,
        [Display(Description = "Miadı 12 Aydan Uzunlar")]
        Miat,
        [Display(Description = "Puanı Yüksek Satıcılar")]
        SellerPoint,
        [Display(Description = "Minimum Sepet Tutarı Olmayanlar")]
        MinimumBasketTotal,
        [Display(Description = "Ücretsiz Kargo Kampanyası")]
        BasketMinKargo,
        [Display(Description = "Sadece Eczaneler")]
        OnlyPharmacy,
        [Display(Description = "Sadece Depolar")]
        OnlyWarehouse,
        [Display(Description = "Sadece İlaç Firmaları")]
        MedicationCompany,
        [Display(Description = "Sadece Yerel Depolar")]
        OnlyWarehouseLocalStorage
    }
    public enum MyFavoritesFilterEnum{
        [Display(Description = "En Son Eklenen")]
        LastAddedFavorites,
        [Display(Description = "En Düşük Fiyat")]
        BestPriceFavorites,
        [Display(Description = "En Yüksek Fiyat")]
        PricedescFavorites
    }
    public enum PagePointFilterEnum{
        [Display(Description = "2'den Yüksek Puanlılar")]
        TwoPoint = 2,
        [Display(Description = "3'ten Yüksek Puanlılar")]
        ThreePoint = 3,
        [Display(Description = "4'ten Yüksek Puanlılar")]
        FourPoint = 4
    }
    public enum ProductSearchTabEnum{
        [Display(Description = "Ürünler")] products,
        [Display(Description = "Markalar")] brands,
        [Display(Description = "Satıcılar")] seller
    }
    public enum CalendarSubjectType{
        [Display(Description = "Genel")]
        [EnumMember(Value = "1")]
        DayOfGenaral = 1,
        [Display(Description = "Doğum Günleri")]
        [EnumMember(Value = "2")]
        DayOfBirthdate = 2,
        [Display(Description = "Tatil Günleri")]
        [EnumMember(Value = "3")]
        DayOfHoliday = 3,
        [Display(Description = "Tıp Günleri")]
        [EnumMember(Value = "4")]
        DayofMedicine = 4,
        [Display(Description = "Toplantı")]
        [EnumMember(Value = "5")]
        DayOfMeeting = 5
    }
    public enum InvoiceType : byte{
        [Display(Description = "Kargo Faturaları ")]
        CargoInvoice = 1,
        [Display(Description = "Kargo Kesintileri Faturaları")]
        CargoDeductionsInvoice = 2,
        [Display(Description = "Hizmet Bedeli Faturaları")]
        ServiceFeeInvoices = 3,
    }
    public enum EducationCategoryType{
        [Display(Description = "Kurs")]
        [EnumMember(Value = "Course")]
        Course = 0,
        [Display(Description = "Sunum – Makale ve Trendler")]
        [EnumMember(Value = "PresentationArticleAndTrends")]
        PresentationArticleAndTrends = 1
    }
    public enum LogType{
        Info = 1,
        Warning = 2,
        Error = 3
    }
    public enum PaymentType : byte{
        [Display(Description = "Kredi Kartı")]
        CreditCart = 1,
        [Display(Description = "Cari Hesap")]
        CustomerBalance = 2
    }
    public enum EmailTemplateType : byte{
        [Display(Description = "Seçiniz")] None = 0,
        [Display(Description = "Yeni Kullanici ")]
        NewUser = 1,
        [Display(Description = "Kullanici Dogrum Gunu")]
        UserBirthDate = 2,
        [Display(Description = "Parola Yenileme")]
        PasswordReset = 3,
        [Display(Description = "Siparis Bildirimi")]
        OrderNotification = 4,
        [Display(Description = "Destek Bildirimi")]
        NewSupport = 5,
        [Display(Description = "Satıcı Sipariş Bildirimi")]
        OrderNotificationSeller = 6,
        [Display(Description = "Sipariş İptal Bildirimi (Müşteri)")]
        OrderCancelledCustomer = 7,
        [Display(Description = "Sipariş İptal Bildirimi (Satıcı)")]
        OrderCancelledSeller = 8,
        [Display(Description = "Müşteri Sipariş Bildirimi")]
        OrderNotificationCustomer = 9,
        [Display(Description = "Tahsilat Makbuzu")]
        PaymentReceipt = 10,
        [Display(Description = "Teslimat Doğrulama Kodu")]
        DeliveryVerificationCode = 11
    }
    public enum BankNames
    {
        //NestPay
        [Display(Name = "Akbank")]
        AkBank = 46,

        [Display(Name = "İş Bankası")]
        IsBankasi = 64,

        [Display(Name = "Halkbank")]
        HalkBank = 12,

        [Display(Name = "Ziraat Bankası")]
        ZiraatBankasi = 10,

        [Display(Name = "Türk Ekonomi Bankası(TEB)")]
        TurkEkonomiBankasi = 32,

        [Display(Name = "ING Bank")]
        IngBank = 99,

        [Display(Name = "Türkiye Finans")]
        TurkiyeFinans = 206,

        [Display(Name = "Anadolubank")]
        AnadoluBank = 135,

        [Display(Name = "HSBC")]
        HSBC = 123,

        [Display(Name = "Şekerbank")]
        SekerBank = 59,

        //InterVPOS
        [Display(Name = "Denizbank")]
        DenizBank = 134,

        //PayFor
        [Display(Name = "QNB Finansbank")]
        FinansBank = 111,

        //GVP
        [Display(Name = "Garanti Bankası")]
        Garanti = 62,

        //KuveytTurk
        [Display(Name = "Kuveyt Türk")]
        KuveytTurk = 205,

        //GET 7/24
        [Display(Name = "Vakıfbank")]
        VakifBank = 15,

        //Posnet
        [Display(Name = "Yapıkredi Bankası")]
        Yapikredi = 67,
        [Display(Name = "Albaraka Türk")]
        Albaraka = 203
    }
    public enum PaymentStatus
    {
        [Display(Name = "Beklemede")]
        Pending = 10,

        [Display(Name = "Ödendi")]
        Paid = 20,

        [Display(Name = "Hatalı")]
        Failed = 30
    }
    
    public enum CustomerAccountTransactionType : byte
    {
        [Display(Description = "Borç")]
        Debit = 1,
        [Display(Description = "Alacak")]
        Credit = 2
    }

    /// <summary>
    /// Kasa hareket tipi: Giriş (kasa girişi) veya Çıkış (kasa çıkışı)
    /// </summary>
    public enum CashRegisterMovementType : byte
    {
        [Display(Description = "Kasa Girişi")]
        In = 1,
        [Display(Description = "Kasa Çıkışı")]
        Out = 2
    }

    /// <summary>
    /// Çek durumu: Portföyde, Tahsil edildi, Reddedildi, İade
    /// </summary>
    public enum CheckStatus : byte
    {
        [Display(Description = "Portföyde")]
        InPortfolio = 1,
        [Display(Description = "Tahsil Edildi")]
        Collected = 2,
        [Display(Description = "Reddedildi")]
        Bounced = 3,
        [Display(Description = "İade")]
        Returned = 4
    }
}
