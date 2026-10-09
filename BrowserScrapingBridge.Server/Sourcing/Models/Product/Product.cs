#nullable disable warnings

namespace BrowserScrapingBridge.Server.Sourcing.Models.Product
{
    public class ProductBase
    {
        /// <summary>
        /// 사이트명
        /// </summary>
        public string site { get; set; }
        /// <summary>
        /// 상품 고유 코드
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// 상품 주소
        /// </summary>
        public string url { get; set; }
        /// 통화
        /// </summary>
        public string currency { get; set; }
        /// <summary>
        /// 수집 결과 상태
        /// </summary>        public ProductCollectStatus collectStatus { get; set; }
        /// <summary>
        /// 상품 판매 상태
        /// </summary>        public ProductSaleStatus saleStatus { get; set; }
        /// <summary>
        /// 상품명
        /// </summary>
        public List<Name> title { get; set; }
        /// <summary>
        /// 상품 브랜드
        /// </summary>
        public List<Name> brand { get; set; }
        /// <summary>
        /// 상품 모델 번호
        /// </summary>
        public string? model { get; set; }
        /// <summary>
        /// 상품 상세 설명 및 스펙 정보
        /// </summary>
        public List<Desc> descs { get; set; }
        /// <summary>
        /// SEO 검색 키워드
        /// </summary>
        public List<Tag> tags { get; set; }
        /// <summary>
        /// 상품 가격
        /// </summary>
        public List<Price> price { get; set; }
        /// <summary>
        /// 수집 시 배송 정보
        /// </summary>
        public List<Delivery> deliveries { get; set; }
        /// <summary>
        /// 상품 메인 이미지(썸네일 이미지)
        /// </summary>
        public List<string> mainImages { get; set; }
        /// <summary>
        /// 상품 영상 소개
        /// </summary>
        public string videoUrl { get; set; }
        /// <summary>
        /// 상세 페이지에 있는 Image Url 리스트(번역때 사용)
        /// </summary>
        public List<string> originalDetailPageUrls { get; set; }
        public List<string> translateDetailPageUrls { get; set; }
        /// <summary>
        /// 상품 상세 페이지 HTML
        /// </summary>
        public string detailPageHtml { get; set; }
        /// <summary>
        /// 상품 옵션
        /// </summary>
        public Option option { get; set; }
        /// <summary>
        /// 상품 전체 재고 수량
        /// </summary>
        public int totalQuantity { get; set; }
        public string? groupName { get; set; }
        public int? workNumber { get; set; }
        /// <summary>
        /// 상품 수집 시 오류 메시지 저장용으로 사용
        /// </summary>
        public string? memo { get; set; }
        public bool? isAIImageProcessing { get; set; }
        public DateTime regDate { get; set; }
    }
    public class Product : ProductBase
    {
        public Product()
        {

        }

        public Product(ProductBase product)
        {
            this.site = product.site;
            this.code = product.code;
            this.url = product.url;
            this.currency = product.currency;
            this.collectStatus = product.collectStatus;
            this.saleStatus = product.saleStatus;
            this.title = product.title;
            this.brand = product.brand;
            this.model = product.model;
            this.descs = product.descs;
            this.tags = product.tags;
            this.price = product.price;
            this.mainImages = product.mainImages;
            this.videoUrl = product.videoUrl;
            this.originalDetailPageUrls = product.originalDetailPageUrls;
            this.translateDetailPageUrls = product.translateDetailPageUrls;
            this.detailPageHtml = product.detailPageHtml;
            this.option = product.option;
            this.totalQuantity = product.totalQuantity;
            this.groupName = product.groupName;
            this.workNumber = product.workNumber;
            this.memo = product.memo;
            this.isAIImageProcessing = product.isAIImageProcessing;
            this.regDate = product.regDate;
        }
    }
}
