#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Models.Product;

namespace BrowserScrapingBridge.Server.Sourcing.Common
{
    public class ProductHelper
    {
        /// <summary>
        /// Option.OptionGroup에서 Option.DetailOption.optionGroupName을 생성
        /// </summary>
        /// <param name="optionGroups"></param>
        /// <returns></returns>
        public static string GetOptionGroupName(List<Option.OptionGroup> optionGroups)
        {
            if (optionGroups == null || optionGroups.Count <= 0) return null;
            List<Name> groupNames = optionGroups.SelectMany(x => x.groupName)?.ToList();
            List<string> texts = groupNames.Select(x => x.text)?.ToList();
            if (texts == null || texts.Count <= 0) return null;
            return string.Join("ㅡ", texts);
        }

        public static string GetOptionGroupId(List<Option.OptionGroup> optionGroups)
        {
            if (optionGroups == null || optionGroups.Count <= 0) return null;
            IEnumerable<string> ids = optionGroups.Select(x => x.optionId);
            return string.Join("ㅡ", ids);
        }


        /// <summary>
        /// 수집 사이트 중 옵션 가격이 Product 기본 가격에서 + 되는 형식인 곳에서 사용
        /// 모든 옵션 가격에 Product 기본 가격을 더함
        /// *수집 정책 = 수집된 Product의 옵션 가격은 상품을 구매 했을 때, 실제 가격이여야함
        /// </summary>
        /// <param name="product"></param>
        public static void SetDetailOptionPriceToProductPrice(Product product)
        {
            if (product.option == null || product.option.detailOptions == null) return;
            foreach (Price price in product.price)
            {
                for (int i = 0; i < product.option.detailOptions.Count; i++)
                {
                    Price? find = product.option.detailOptions[i].price.Find(x => x.currency == price.currency);
                    if (find == null) continue;
                    find.salePrice += price.salePrice;
                    find.originalPrice += price.originalPrice;
                }
            }
        }

        /// <summary>
        /// DetailOption 중 최저가 옵션 가격 반환
        /// *수집 정책 = 옵션이 있을 경우 상품 가격은 옵션 가격 중 가장 낮은 가격으로 설정
        /// </summary>
        /// <param name="detailOptions">독립 옵션 리스트</param>
        /// <param name="currency">화폐 단위</param>
        /// <returns></returns>
        public static List<Price> GetMinPriceInDetailOption(List<Option.DetailOption> detailOptions, Currency currency)
        {
            return GetMinPriceInDetailOption(detailOptions, currency.ToString());
        }
        public static List<Price> GetMinPriceInDetailOption(List<Option.DetailOption> detailOptions, string currency)
        {
            if (detailOptions == null || detailOptions.Count <= 0) return null;
            List<List<Price>> selectOptionPrices = detailOptions.Select(x => x.price).ToList();
            if (selectOptionPrices == null || selectOptionPrices.Count <= 0) return null;

            List<Price>? minPrice = null;
            for (int i = 0; i < selectOptionPrices.Count; i++)
            {
                List<Price> price = selectOptionPrices[i];

                Price? currentPrice = price.Find(x => x.currency == currency);
                if (currentPrice == null) continue;
                if (currentPrice.salePrice <= 0 || currentPrice.originalPrice <= 0)
                {
                    continue;
                }

                if (minPrice == null)
                {
                    minPrice = price;
                }
                else
                {
                    Price? previousPrice = minPrice.Find(x => x.currency == currency);
                    if (previousPrice == null) { continue; }
                    if (currentPrice.salePrice < previousPrice.salePrice)
                    {
                        minPrice = price;
                    }
                    else if (currentPrice.salePrice <= previousPrice.salePrice && currentPrice.originalPrice < previousPrice.originalPrice)
                    {
                        minPrice = price;
                    }
                }
            }

            var result = new List<Price>(); // 참조 문제를 발생시키지 않기 위해 인스턴스 새로 생성
            if (minPrice != null)
            {
                foreach (Price optionPrice in minPrice)
                {
                    result.Add(new Price(optionPrice.currency, optionPrice.originalPrice, optionPrice.salePrice));
                }
            }
            return result;
        }

        public static Option.DetailOption FindMinDetailOption(List<Option.DetailOption> detailOptions)
        {
            if (detailOptions == null || detailOptions.Count <= 0) return null;
            Option.DetailOption result = null;
            foreach (var currentOption in detailOptions)
            {
                if (result == null)
                {
                    result = currentOption;
                }
                else
                {
                    double prevSP = result.price.First().salePrice;
                    double prevOP = result.price.First().originalPrice;

                    double currentSP = currentOption.price.First().salePrice;
                    double currentOP = currentOption.price.First().originalPrice;

                    if (currentSP < prevSP)
                    {
                        result = currentOption;
                    }
                    else if (currentSP <= prevSP && currentOP < prevOP)
                    {
                        result = currentOption;
                    }
                }
            }
            return result;
        }

        public static List<Name> CreateName(Language language, string name)
        {
            return new List<Name> { new Name(language, name) };
        }
        public static List<Price> CreatePrice(Currency currency, double? originalPrice, double? salePrice, bool isdefalutPrice)
        {
            return new List<Price> { new Price(currency, (double)originalPrice, (double)salePrice, isdefalutPrice) };
        }
        public static List<Price> CreatePrice(Currency currency, double originalPrice, double salePrice, bool isdefalutPrice)
        {
            return new List<Price> { new Price(currency, originalPrice, salePrice, isdefalutPrice) };
        }
        public static List<Price> CreatePrice(Currency currency, double? originalPrice, double? salePrice)
        {
            if (originalPrice == null) originalPrice = 0;
            if (salePrice == null) salePrice = 0;
            return new List<Price> { new Price(currency, (double)originalPrice, (double)salePrice) };
        }
        public static List<Price> CreatePrice(Currency currency, double originalPrice, double salePrice)
        {
            return new List<Price> { new Price(currency, originalPrice, salePrice) };
        }
        public static List<Price> CreatePrice(string currency, double originalPrice, double salePrice)
        {
            return new List<Price> { new Price(currency, originalPrice, salePrice) };
        }

        /// <summary>
        /// 모든 옵션에 곱집합(카테시안 곱) 가져오기
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequences"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> GetCartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> result = new[] { Enumerable.Empty<T>() };

            foreach (var sequence in sequences)
            {
                result = result.SelectMany(acc => sequence, (acc, item) => acc.Append(item));
            }

            return result;
        }

        public static List<Option.DetailOption> CreateDetailOptions(List<Option.OptionGroup> optionGroups)
        {
            if (optionGroups == null || optionGroups.Count <= 0) return null;

            Language language = Language.KR;

            string optionGroupId = GetOptionGroupId(optionGroups);

            string optionGroupName = string.Join("ㅡ", optionGroups.Select(optionGroup => optionGroup.groupName?.Find(groupName => groupName.language == "KR")?.text ?? optionGroup.groupName?.FirstOrDefault()?.text ?? string.Empty));

            List<List<Option.OptionGroup.Group>> selectOptionGroups = optionGroups.Select(optionGroup => optionGroup.groups).ToList();
            IEnumerable<IEnumerable<Option.OptionGroup.Group>> cartesianProduct = GetCartesianProduct(selectOptionGroups);

            var createCartesianOptions = new Dictionary<string, string>();
            List<IEnumerable<Option.OptionGroup.Group>> cartesianOptions = cartesianProduct.ToList();

            foreach (IEnumerable<Option.OptionGroup.Group> cartesianOption in cartesianOptions)
            {
                IEnumerable<string> ids = cartesianOption.Select(optionGroupItem => optionGroupItem.groupId);
                IEnumerable<string> optionNames = cartesianOption.Select(optionGroupItem => optionGroupItem.optionName?.Find(optionName => optionName.language == "KR")?.text ?? optionGroupItem.optionName?.FirstOrDefault()?.text ?? string.Empty);

                var createId = string.Join("ㅡ", ids);
                var createOptionName = string.Join("ㅡ", optionNames);

                createCartesianOptions.Add(createId, createOptionName);
            }

            var detailOptions = new List<Option.DetailOption>();
            foreach (KeyValuePair<string, string> cartesianOption in createCartesianOptions)
            {
                var detailOption = new Option.DetailOption();
                detailOption.optionIds = cartesianOption.Key;
                detailOption.optionGroupName = CreateName(language, optionGroupName);
                detailOption.optionName = CreateName(language, cartesianOption.Value);
                detailOption.quantity = 9;
                detailOption.isSoldOut = false;
                detailOptions.Add(detailOption);
            }

            return detailOptions;
        }
    }
}
