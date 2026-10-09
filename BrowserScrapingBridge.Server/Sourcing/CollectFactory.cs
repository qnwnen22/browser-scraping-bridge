#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.GmarketGeneral;
using System.Net;

namespace BrowserScrapingBridge.Server.Sourcing
{
    public class CollectFactory
    {
        private CollectFactory() { }

        /// <summary>
        /// payment.site_info에 권한 설정 전 개발 단계에서 테스트를 위해 사이트 권한 강제 추가
        /// </summary>
        public static string[] CollectSites { get; } = new string[]
        {
        };

        public static ICollect CreateInstance(string site, IChatHubService chatHubService, WebProxy webProxy)
        {
            ICollect collect = null;
            switch (site)
            {
                case "AliExpress-KR":
                    collect = new AliExpress_KR.AliExpress();
                    break;
                case nameof(B2bFashion.B2bFashion):
                    collect = new B2bFashion.B2bFashion();
                    break;
                case nameof(Alibaba.Taobao):
                    collect = new Alibaba.Taobao();
                    break;
                case "1688":
                    collect = new Alibaba.Alibaba1688();
                    break;
                case "AmazonUS":
                case "Amazon-US":
                    collect = new Amazon.AmazonUS();
                    break;
                case "AmazonJP":
                case "Amazon-JP":
                    collect = new Amazon.AmazonJP();
                    break;
                case "AmazonDE":
                case "Amazon-DE":
                    collect = new Amazon.AmazonDE();
                    break;
                case "Amazon-AU":
                    collect = new Amazon.AmazonAU();
                    break;
                case "Amazon-FR":
                    collect = new Amazon.AmazonFR();
                    break;
                case "Amazon-UK":
                    collect = new Amazon.AmazonUK();
                    break;
                case nameof(Rakuten.Rakuten):
                    collect = new Rakuten.Rakuten();
                    break;
                case nameof(ShoppingNaver.ShoppingNaver):
                    break;
                case "ShoppingNaver-Logistics":
                    collect = new ShoppingNaver.ShoppingNaverLogistics();
                    break;
                case "ShoppingNaver-Directfarm":
                    collect = new ShoppingNaver.ShoppingNaverDirectfarm();
                    break;
                case nameof(Domeggook.Domeme):
                    collect = new Domeggook.Domeme();
                    break;
                case nameof(Mytheresa.Mytheresa):
                    collect = new Mytheresa.Mytheresa();
                    break;
                case nameof(Farfetch.Farfetch):
                    collect = new Farfetch.Farfetch();
                    break;
                case nameof(Breuninger.Breuninger):
                    collect = new Breuninger.Breuninger();
                    break;
                case nameof(Walmart.Walmart):
                    collect = new Walmart.Walmart();
                    break;
                case nameof(Ebay.Ebay):
                    collect = new Ebay.Ebay();
                    break;
                case nameof(VVIC.VVIC):
                    collect = new VVIC.VVIC();
                    break;
                case nameof(Oliveyoung.Oliveyoung):
                    collect = new Oliveyoung.Oliveyoung();
                    break;
                case nameof(ShopSimon.ShopSimon):
                    collect = new ShopSimon.ShopSimon();
                    break;
                case nameof(Daiso.Daiso):
                    collect = new Daiso.Daiso();
                    break;
                case nameof(Idealo.Idealo):
                case "IdealoDE":
                case "IdealoFR":
                    //collect = new Idealo.Idealo();
                    break;
                case nameof(PocaMarket):
                    collect = new PocaMarket.PocaMarket();
                    break;
                case nameof(LotteOn.LotteOn):
                case "LotteOnCol":
                    collect = new LotteOn.LotteOn();
                    break;
                case nameof(SSG.SSG):
                case "SSGCol":
                    collect = new SSG.SSG();
                    break;
                case nameof(FashionPlus.FashionPlus):
                    collect = new FashionPlus.FashionPlus();
                    break;
                case nameof(Gmarket.Gmarket):
                    collect = new Gmarket.Gmarket();
                    break;

                case "Gmarket_General":
                    collect = new GmarketGeneral.GmarketGeneral();
                    break;

                case nameof(GmarketGeneral):
                    collect = new GmarketGeneral.GmarketGeneral();
                    break;

                case nameof(GmarketShop.GmarketShop):
                    collect = new GmarketShop.GmarketShop();
                    break;

                case "BlackYak":
                    collect = new BlackYak.BlackYak();
                    break;
                case nameof(Zara.Zara):
                case "자라":
                    collect = new Zara.Zara();
                    break;
                case nameof(NaverPStore.NaverPStore):
                case "네이버+스토어":
                    collect = new NaverPStore.NaverPStore();
                    break;

                case nameof(Halfclub.Halfclub):
                case "하프클럽":
                    collect = new Halfclub.Halfclub();
                    break;
                case nameof(Hmall.Hmall):
                case "H몰":
                    collect = new Hmall.Hmall();
                    break;
                case nameof(Ikea.Ikea):
                case "이케아":
                    collect = new Ikea.Ikea();
                    break;
                case nameof(AbcMart.AbcMart):
                case "ABCMart":
                case "ABC마트":
                    collect = new AbcMart.AbcMart();
                    break;
                case nameof(TrueReligion.TrueReligion):
                case "트릴리전":
                    collect = new TrueReligion.TrueReligion();
                    break;
                case nameof(WeverseShop.WeverseShop):
                case "위버스샵":
                    collect = new WeverseShop.WeverseShop();
                    break;
                case nameof(CoupangRocket.CoupangRocket):
                case "쿠팡로켓배송":
                    collect = new CoupangRocket.CoupangRocket();
                    break;
                case nameof(GrandStage.GrandStage):
                case "그랜드스테이지":
                    collect = new GrandStage.GrandStage();
                    break;
                case nameof(Zentrade.Zentrade):
                case "젠트레이드":
                    collect = new Zentrade.Zentrade();
                    break;
                case nameof(SellerHae.SellerHae):
                case "셀러해":
                    collect = new SellerHae.SellerHae();
                    break;
                case nameof(Compuzone.Compuzone):
                case "컴퓨존":
                    collect = new Compuzone.Compuzone();
                    break;
                case nameof(Yes24.Yes24):
                case "YES24":
                    collect = new Yes24.Yes24();
                    break;
                case nameof(TheHyundai.TheHyundai):
                case "더현대":
                    collect = new TheHyundai.TheHyundai();
                    break;
                case nameof(Ktown4u.Ktown4u):
                case "KTOWN4U":
                    collect = new Ktown4u.Ktown4u();
                    break;
                case nameof(FolderStyle.FolderStyle):
                case "폴더":
                    collect = new FolderStyle.FolderStyle();
                    break;
                case nameof(GlobalTNT.GlobalTNT):
                case "글로벌티엔티":
                    collect = new GlobalTNT.GlobalTNT();
                    break;
                case nameof(Itovic.Itovic):
                case "아이토빅":
                    collect = new Itovic.Itovic();
                    break;
                case nameof(AppFans.AppFans):
                case "팬즈":
                    collect = new AppFans.AppFans();
                    break;
                case nameof(Queenit.Queenit):
                case "퀸잇":
                    collect = new Queenit.Queenit();
                    break;
                case nameof(GsShop.GsShop):
                case "GS샵":
                    collect = new GsShop.GsShop();
                    break;
                case nameof(Nike.Nike):
                case "나이키":
                    collect = new Nike.Nike();
                    break;
                case nameof(Makestar.Makestar):
                case "메이크스타":
                    collect = new Makestar.Makestar();
                    break;

                case nameof(Musinsa.Musinsa):
                    collect = new Musinsa.Musinsa();
                    break;
                case nameof(OwnerClan.OwnerClan):
                    collect = new OwnerClan.OwnerClan();
                    break;
                case nameof(Costco.Costco):
                    collect = new Costco.Costco();
                    break;
                case nameof(Zurbrueggen.Zurbrueggen):
                    collect = new Zurbrueggen.Zurbrueggen();
                    break;
                case nameof(Boticinal.Boticinal):
                    collect = new Boticinal.Boticinal();
                    break;
                case nameof(Vitacost.Vitacost):
                    collect = new Vitacost.Vitacost();
                    break;
                case nameof(Selfridges.Selfridges):
                    collect = new Selfridges.Selfridges();
                    break;
                case nameof(Healthylife.Healthylife):
                    collect = new Healthylife.Healthylife();
                    break;
                case nameof(Bueromarkt.Bottcher):
                    collect = new Bueromarkt.Bottcher();
                    break;
                case nameof(Etsy.Etsy):
                    collect = new Etsy.Etsy();
                    break;
                case nameof(KoreanSsan.KoreanSsan):
                    collect = new KoreanSsan.KoreanSsan();
                    break;
                case nameof(Poizon_KR.PoizonKR):
                case "포이즌KR":
                    collect = new Poizon_KR.PoizonKR();
                    break;
#if DEBUG
                case nameof(Flaconi.Flaconi):
                    collect = new Flaconi.Flaconi();
                    break;
#endif
            }
            if (collect != null)
            {
                collect.ChatHubService = chatHubService;
                collect.Proxy = webProxy;
            }
            return collect;
        }
    }
}