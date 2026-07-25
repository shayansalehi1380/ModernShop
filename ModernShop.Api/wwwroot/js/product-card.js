/* ==============================================================
   Atelier — کامپوننت مشترک کارت محصول (علاقه‌مندی + کلیک برای رفتن به صفحه محصول)
   این فایل باید بعد از js/api.js لود بشه. روی هر صفحه‌ای که کارت محصول
   نشون می‌ده (index.html، shop.html، محصولات مرتبط تو product.html) استفاده می‌شه:

     await ProductCard.loadState();     // وضعیت واقعی علاقه‌مندی‌ها رو از سرور می‌خونه
     grid.innerHTML = products.map(ProductCard.render).join('');
     ProductCard.bind(grid);            // event delegation برای دکمه‌ی علاقه‌مندی
     ProductCard.resolvePendingWishlist(); // اگه از لاگین برگشته و یه علاقه‌مندی معلق داشته

   طبق تصمیم محصولی: هیچ‌جای سایت کارت محصول دکمه‌ی «افزودن به سبد» نداره؛
   کل کارت یک لینک به صفحه محصوله و افزودن به سبد فقط از همون‌جا انجام می‌شه
   (چه محصول ساده چه متغیر) - تجربه‌ی یکسان و بدون گیج‌کنندگی رنگ/سایز رو کارت.

   ساختار مورد انتظار هر محصول: { id, name, slug, mainImageUrl, categoryName,
   price, discountPrice, averageRating, reviewCount, inStock, badge }
   ============================================================== */

const ProductCard = (function () {
  const PENDING_WISHLIST_KEY = 'atelier_pending_wishlist';

  let wishlistState = new Set();  // productId

  function escapeHtmlPC(str) {
    return String(str ?? '').replace(/[&<>"']/g, s => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[s]));
  }
  function toFaPC(n) {
    const d = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];
    return String(n).replace(/[0-9]/g, x => d[x]);
  }
  function fmtPC(n) {
    return toFaPC(Number(n || 0).toLocaleString('en-US'));
  }

  async function loadState() {
    wishlistState = new Set();
    if (isLoggedIn()) {
      try {
        const items = await Api.getWishlist();
        items.forEach(w => wishlistState.add(w.productId));
      } catch (e) { /* بی‌سروصدا رد شو */ }
    }
  }

  function render(p) {
    const price = p.discountPrice || p.price;
    const oldPrice = p.discountPrice ? p.price : null;
    const inWishlist = wishlistState.has(p.id);
    const productUrl = `product.html?slug=${encodeURIComponent(p.slug)}`;

    const badge = p.badge
      ? `<span class="absolute right-3 top-3 rounded-full px-2.5 py-1 text-[11px] font-semibold ${p.badge === 'جدید' ? 'bg-emerald-soft text-emerald' : 'bg-danger/10 text-danger'}">${escapeHtmlPC(p.badge)}</span>`
      : '';
    const outOfStock = !p.inStock
      ? `<div class="absolute inset-0 flex items-center justify-center bg-surface/80 backdrop-blur-[1px]"><span class="rounded-full bg-danger/10 px-3 py-1.5 text-xs font-semibold text-danger">ناموجود</span></div>`
      : '';

    // کل کارت یک لینک به صفحه محصوله (به‌جز دکمه‌ی علاقه‌مندی که به‌صورت خواهر/برادرِ همین لینک،
    // با موقعیت absolute روش قرار می‌گیره تا کلیکش با ناوبری کارت تداخل نکنه)
    return `<div class="tilt-card product-card relative w-full rounded-2xl border border-line bg-surface p-3" data-product-id="${p.id}">
      <button type="button" class="pc-wishlist-btn absolute left-3 top-3 z-10 flex h-9 w-9 items-center justify-center rounded-full bg-surface shadow-sm ${inWishlist ? 'text-danger' : 'text-foreground/70'}" data-pid="${p.id}" aria-label="افزودن به علاقه‌مندی‌ها">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="${inWishlist ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="2"><path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.8 1-1a5.5 5.5 0 0 0 0-7.8Z"/></svg>
      </button>
      <a href="${productUrl}" class="block">
        <div class="relative aspect-square overflow-hidden rounded-xl bg-media">
          <img src="${p.mainImageUrl || 'https://picsum.photos/400/400'}" class="h-full w-full object-cover" loading="lazy" alt="${escapeHtmlPC(p.name)}" />
          ${badge}${outOfStock}
        </div>
        <div class="mt-3 px-1">
          <div class="text-xs text-muted">${escapeHtmlPC(p.categoryName || '')}</div>
          <h3 class="mt-1 text-sm font-semibold leading-tight hover:text-emerald">${escapeHtmlPC(p.name)}</h3>
          <div class="mt-1.5 flex items-center gap-1 text-xs text-muted">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor" class="text-gold"><path d="m12 2 3.1 6.3 6.9 1-5 4.9 1.2 6.8L12 17.8 5.8 21l1.2-6.8-5-4.9 6.9-1Z"/></svg>
            <span class="ticker">${toFaPC(p.averageRating ?? 0)}</span><span class="ticker">(${toFaPC(p.reviewCount ?? 0)})</span>
          </div>
          <div class="price-row mt-2.5">
            <span class="ticker text-[15px] font-bold">${fmtPC(price)}</span>
            <span class="text-[11px] text-muted">تومان</span>
            ${oldPrice ? `<span class="old-price ticker text-xs text-muted line-through">${fmtPC(oldPrice)}</span>` : ''}
          </div>
        </div>
      </a>
    </div>`;
  }

  function refreshWishlistBtn(pid) {
    const inWishlist = wishlistState.has(pid);
    document.querySelectorAll(`.pc-wishlist-btn[data-pid="${pid}"]`).forEach(btn => {
      btn.classList.toggle('text-danger', inWishlist);
      btn.classList.toggle('text-foreground/70', !inWishlist);
      const svg = btn.querySelector('svg');
      if (svg) svg.setAttribute('fill', inWishlist ? 'currentColor' : 'none');
    });
  }

  async function handleWishlistToggle(pid) {
    if (!isLoggedIn()) {
      // بعد از لاگین باید بلافاصله همین محصول به علاقه‌مندی‌ها اضافه بشه
      localStorage.setItem(PENDING_WISHLIST_KEY, String(pid));
      const here = window.location.pathname.split('/').pop() + window.location.search;
      window.location.href = 'auth.html?redirect=' + encodeURIComponent(here || 'index.html');
      return;
    }
    try {
      if (wishlistState.has(pid)) {
        await Api.removeFromWishlist(pid);
        wishlistState.delete(pid);
        if (typeof showToast === 'function') showToast('success', 'از علاقه‌مندی‌ها حذف شد');
      } else {
        await Api.addToWishlist(pid);
        wishlistState.add(pid);
        if (typeof showToast === 'function') showToast('success', 'به علاقه‌مندی‌ها اضافه شد');
      }
      refreshWishlistBtn(pid);
    } catch (e) {
      if (typeof showToast === 'function') showToast('error', e.message || 'خطا در بروزرسانی علاقه‌مندی‌ها');
    }
  }

  // بعد از برگشتن از صفحه لاگین (که به‌خاطر زدن قلب علاقه‌مندی به اونجا فرستاده شده بود)
  async function resolvePendingWishlist() {
    const pendingRaw = localStorage.getItem(PENDING_WISHLIST_KEY);
    if (!pendingRaw) return;
    localStorage.removeItem(PENDING_WISHLIST_KEY);
    if (!isLoggedIn()) return;

    const pid = parseInt(pendingRaw, 10);
    if (!pid) return;
    try {
      await Api.addToWishlist(pid);
      wishlistState.add(pid);
      refreshWishlistBtn(pid);
      if (typeof showToast === 'function') showToast('success', 'به علاقه‌مندی‌ها اضافه شد');
    } catch (e) { /* بی‌سروصدا رد شو */ }
  }

  function bind(container) {
    container.addEventListener('click', (e) => {
      const wishBtn = e.target.closest('.pc-wishlist-btn');
      if (wishBtn) { e.preventDefault(); e.stopPropagation(); handleWishlistToggle(parseInt(wishBtn.dataset.pid, 10)); }
    });
  }

  return { loadState, render, bind, resolvePendingWishlist };
})();
