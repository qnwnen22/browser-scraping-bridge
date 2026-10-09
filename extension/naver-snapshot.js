function readNaverSnapshot() {
  const readProducts = () => { 
return (function() {
    var products = [], seen = {};
    // 1. DOM 링크에서 상품 URL 추출 (가장 신뢰도 높음)
    try {
        var links = document.querySelectorAll('a[href]');
        for (var i = 0; i < links.length; i++) {
            var a = links[i], href = a.href || '';
            var pm = href.match(/window-products\/[^/?#]+\/(\d+)/)
                  || href.match(/\/catalog\/(\d{10,})/)
                  || href.match(/\/products?\/(\d+)/);
            if (!pm && href.indexOf('cr.shopping.naver.com') > -1) {
                try {
                    var crUrl = new URL(href);
                    var nvMid = crUrl.searchParams.get('nvMid');
                    if (nvMid && nvMid.length >= 10) {
                        pm = [null, nvMid];
                        href = 'https://search.shopping.naver.com/catalog/' + nvMid;
                    } else {
                        var uP = crUrl.searchParams.get('u');
                        if (uP) { var d = decodeURIComponent(uP); pm = d.match(/\/catalog\/(\d{10,})/) || d.match(/\/products?\/(\d+)/); if (pm) href = d; }
                    }
                } catch(ue) {}
            }
            if (!pm) continue;
            var id = pm[pm.length - 1];
            if (seen[id]) continue;
            if (!a.querySelector('img') && a.offsetHeight < 30) continue;
            seen[id] = true;
            var name = '';
            var el = a.querySelector('[class*="name" i], [class*="title" i], [class*="tit" i]');
            if (el) name = el.textContent.trim();
            if (!name) { var img = a.querySelector('img[alt]'); if (img) name = img.alt || ''; }
            if (!name) name = (a.getAttribute('title') || '').trim();
            products.push({id:id, url:href, name:name});
        }
    } catch(e) {}
    // 2. data 속성에서 상품 ID 추출
    try {
        var cards = document.querySelectorAll('[data-nclick], [data-shp-contents-id]');
        for (var i = 0; i < cards.length; i++) {
            var card = cards[i];
            var id = card.getAttribute('data-shp-contents-id') || '';
            if (!id) { var nc = card.getAttribute('data-nclick') || ''; var ncm = nc.match(/i:(\d+)/); if (ncm) id = ncm[1]; }
            if (!id || seen[id]) continue;
            seen[id] = true;
            var name = '';
            var ne = card.querySelector('[class*="name" i], [class*="title" i], [class*="tit" i]');
            if (ne) name = ne.textContent.trim();
            var cl = card.tagName === 'A' ? card : card.querySelector('a[href]');
            var url = cl ? cl.href : '';
            products.push({id:id, url:url, name:name});
        }
    } catch(e) {}
    // 3. flight data에서 productId/nvMid만 추출 (catalogId, generic id 제외 — 카테고리 ID 혼동 방지)
    try {
        if (window.__next_f && products.length === 0) {
            var all = '';
            for (var i = 0; i < window.__next_f.length; i++) {
                if (window.__next_f[i] && typeof window.__next_f[i][1] === 'string')
                    all += window.__next_f[i][1];
            }
            var idPats = [
                /"productId"\s*:\s*"(\d{10,})"/g,
                /"nvMid"\s*:\s*"(\d{10,})"/g,
                /"channelProductId"\s*:\s*"(\d+)"/g
            ];
            for (var pi = 0; pi < idPats.length; pi++) {
                var re = idPats[pi], m;
                while ((m = re.exec(all)) !== null) {
                    var id = m[1];
                    if (seen[id]) continue;
                    var start = Math.max(0, m.index - 500);
                    var ctx = all.substring(start, m.index + 800);
                    var nm = ctx.match(/"(?:name|title|productName|productTitle)"\s*:\s*"([^"]{1,200})"/);
                    var ur = ctx.match(/"(?:productUrl|mobileProductUrl|url|crUrl|purchaseUrl)"\s*:\s*"(https?:[^"]+)"/);
                    seen[id] = true;
                    products.push({id:id, name:nm?nm[1]:'', url:ur?ur[1]:''});
                }
            }
        }
    } catch(e) {}
    return JSON.stringify(products);
})()
 };
  const readProduct = () => { 
return (function() {
    if (!window.__next_f) return null;
    var all = '';
    for (var i = 0; i < window.__next_f.length; i++) {
        if (window.__next_f[i] && typeof window.__next_f[i][1] === 'string')
            all += window.__next_f[i][1];
    }
    var result = {title:'',salePrice:0,originalPrice:0,images:[],options:[],detailHtml:''};
    var tm = all.match(/"(?:name|title|productName)"\s*:\s*"([^"]{1,300})"/);
    if (tm) result.title = tm[1];
    var pm = all.match(/"(?:salePrice|price|discountedSalePrice)"\s*:\s*(\d+)/);
    if (pm) result.salePrice = parseInt(pm[1]);
    var om = all.match(/"(?:originalPrice|listPrice)"\s*:\s*(\d+)/);
    if (om) result.originalPrice = parseInt(om[1]);
    var imgRe = /"(?:representativeImageUrl|imageUrl|productImageUrl)"\s*:\s*"(https?:[^"]+)"/g, im;
    while ((im = imgRe.exec(all)) !== null) {
        if (result.images.indexOf(im[1]) === -1) result.images.push(im[1]);
    }
    return JSON.stringify(result);
})()
 };
  return { products: readProducts(), product: readProduct() };
}
