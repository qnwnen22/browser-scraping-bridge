#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing
{
    public abstract class CollectBase : ICollect
    {
        public abstract string Site { get; }
        public abstract string Name { get; }
        public abstract Language Language { get; set; }
        public abstract Currency Currency { get; set; }
        public abstract bool SupportsCaching { get; set; }

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }

        public abstract Task<string> GetLoginUrlAsync(string connectionId);

        public abstract Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page);

        public virtual async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            return await ChatHubService.GetRequest(connectionId, url);
        }

        #region Build Product
        /// <summary>
        /// captcha 페이지 여부 판단하여 captcha 페이지일 경우 예외(CaptchaException) 발생시키는 메서드
        /// </summary>
        /// <param name="html"></param>
        public abstract void ThrowIfCaptcha(string url, string html);
        public abstract string GetCode(string url);
        public abstract string CreateProductUrl(string code);
        public abstract Task<JToken> GetData(string connectionId, string url, string html);
        public abstract List<Name> GetTitle(string html, JToken data);
        public abstract List<Name> GetBrand(string html, JToken data);
        public abstract string GetModel(string html, JToken data);
        public abstract List<Desc> GetDescs(string html, JToken data);
        public abstract List<Tag> GetTags(string html, JToken data);
        public abstract List<Price> GetPrice(string html, JToken data);
        public virtual List<Delivery> GetDeliveries(string html, JToken data)
        {
            return null;
        }
        public abstract List<string> GetMainImages(string html, JToken data);
        public abstract string GetVideoUrl(string html, JToken data);
        public abstract string GetDetailPageHtml(string html, JToken data);
        public abstract Option GetOption(string html, JToken data);
        /// <summary>
        /// 품절 여부 판단하여 품절일 경우 예외(SaleException) 발생시키는 메서드 
        /// </summary>
        public abstract void ThrowIfOutOfStock(string html, JToken data, Product product);
        /// <summary>
        /// 상품 재고 수량 반환하는 메서드(재고 정보를 파악할 수 없는 경우 null반환)
        /// null 반환 시 옵션이 없을 경우 9개로 자동 설정
        /// </summary>
        public abstract int? GetTotalQuantity(string html, JToken data);

        private void IncludeShippingInPrice(Product product)
        {
            if (product.deliveries != null && product.deliveries.Count > 0)
            {
                Delivery delivery = product.deliveries.MinBy(x => x.price.First().salePrice);

                var deliveryPrice = delivery.price.First();
                product.price.First().salePrice += deliveryPrice.salePrice;
                product.price.First().originalPrice += deliveryPrice.originalPrice;

                if (product.option != null)
                {
                    foreach (var detailOption in product.option.detailOptions)
                    {
                        detailOption.price.First().salePrice += deliveryPrice.salePrice;
                        detailOption.price.First().originalPrice += deliveryPrice.originalPrice;
                    }
                }
            }
        }

        protected Product BuildProduct(string url, string html, JToken data)
        {
            var product = new Product();
            try
            {
                ThrowIfCaptcha(url, html);
                product.site = this.Site;
                string code = GetCode(url);
                if (string.IsNullOrWhiteSpace(code)) throw new Exception("상품 코드를 찾을 수 없습니다.");
                product.code = code;
                string itemUrl = CreateProductUrl(code);
                product.url = string.IsNullOrWhiteSpace(itemUrl) ? url : itemUrl;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(html, data);
                if (product.title == null) throw new Exception("상품명을 찾을 수 없습니다.");
                product.brand = GetBrand(html, data);
                product.model = GetModel(html, data);
                product.descs = GetDescs(html, data);
                product.tags = GetTags(html, data);
                product.price = GetPrice(html, data);
                product.deliveries = GetDeliveries(html, data);
                if (product.price == null) throw new Exception("가격정보를 찾을 수 없습니다.");
                product.mainImages = GetMainImages(html, data);
                if (product.mainImages == null || product.mainImages.Count == 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
                product.videoUrl = GetVideoUrl(html, data);
                product.detailPageHtml = GetDetailPageHtml(html, data);
                product.option = GetOption(html, data);
                IncludeShippingInPrice(product);
                ThrowIfOutOfStock(html, data, product);
                if (product.option != null)
                {
                    // 재고가 1000개가 넘을 경우 1000으로 고정
                    // ※ 재고가 999999999와 같은 비정상적인 값으로 합산(Sum) 할 경우 ArithmeticException 발생
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        Option.DetailOption detailOption = product.option.detailOptions[i];
                        int quantity = detailOption.quantity;
                        detailOption.quantity = quantity > 1000
                            ? 1000
                            : product.option.detailOptions[i].quantity;
                    }
                    int totalQuantity = product.option.detailOptions.Sum(x => x.quantity);
                    product.totalQuantity = totalQuantity;
                }
                else
                {
                    int? totalQuantity = GetTotalQuantity(html, data);
                    // 옵션이 없을 경우 기본 재고 9개로 설정
                    product.totalQuantity = totalQuantity == null ? 9 : (int)totalQuantity;
                }
                product.regDate = DateTime.UtcNow;
            }
            catch (CaptchaException ce)
            {
                throw;
            }
            catch (SocketException sce)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = $"소켓 오류 발생 : {sce.Message}";
            }
            catch (CollectException ce)
            {
                product.collectStatus = ce.CollectStatus;
                product.memo = ce.Message;
            }
            catch (SaleException se)
            {
                product.collectStatus = ProductCollectStatus.Success;
                product.saleStatus = se.SaleStatus;
                product.memo = se.Message;
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
            }
            return product;
        }
        #endregion

        public virtual async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await GetHtmlAsync(connectionId, url);
            JToken data = await GetData(connectionId, url, html);
            Product product = BuildProduct(url, html, data);
            await ApplyCollectOptionSetting(connectionId, new CollectRawData { Url = url, Html = html, Data = data, }, product, collectOptionSetting);
            return product;
        }

        public virtual async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            string html = await GetHtmlAsync(connectionId, url);
            JToken data = await GetData(connectionId, url, html);
            var collectRawData = new CollectRawData(url, data, html);
            return collectRawData;
        }

        public virtual Product GetProduct(CollectRawData rawData)
        {
            var product = BuildProduct(rawData.Url, rawData.Html, rawData.Data);
            return product;
        }

        public abstract Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting);
    }
}
