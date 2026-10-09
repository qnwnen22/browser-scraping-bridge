#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using System;

namespace BrowserScrapingBridge.Server.Sourcing.Common
{
    /// <summary>
    /// SaleException은 판매상태를 체크해서 예외를 던지는 기능을 하는 클래스
    /// SaleException 사용 기준은 수집은 성공했지만, 판매 중이 아닐 경우 예외를 던지는 경우에 사용
    /// </summary>
    public class SaleException : Exception
    {
        public ProductSaleStatus SaleStatus { get; set; }
        public SaleException(ProductSaleStatus saleStatus, string message) : base(message)
        {
            SaleStatus = saleStatus;
        }
    }
}
