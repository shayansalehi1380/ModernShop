/* ==============================================================
   Atelier — کامپوننت مشترک کارت محصول (استپر تعداد + علاقه‌مندی)
   این فایل باید بعد از js/api.js لود بشه. روی هر صفحه‌ای که کارت محصول
   نشون می‌ده (index.html، shop.html، account.html تب علاقه‌مندی‌ها) استفاده می‌شه:

     await ProductCard.loadState();     // وضعیت واقعی سبد + علاقه‌مندی‌ها رو از سرور می‌خونه
     grid.innerHTML = products.map(ProductCard.render).join('');
     ProductCard.bind(grid);            // event delegation برای دکمه‌های داخل کارت‌ها
     ProductCard.resolvePendingWishlist(); // اگه از لاگین برگشته و یه علاقه‌مندی معلق داشته

   ساختار مورد انتظار هر محصول: { id, name, slug, mainImageUrl, categoryName,
   price, discountPrice, averageRating, reviewCount, inStock, badge }
   ============================================================== */

const ProductCard = (function () {
  const PENDING_WISHLIST_KEY = 'atelier_pending_wishlist';

  let cartState = new Map();      // productId -> { cartItemId, quantity }
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
    try {
      const cart = await Api.getCart();
      cartState = new Map();
      (cart.items || []).forEach(i => cartState.set(i.productId, { cartItemId: i.id, quantity: i.quantity }));
    } catch (e) {
      cartState = new Map();
    }

    wishlistState = new Set();
    if (isLoggedIn()) {
      try {
        const items = await Api.getWishlist();
        items.forEach(w => wishlistState.add(w.productId));
      } catch (e) { /* بی‌سروصدا رد شو */ }
    }
  }

  function cartAreaHTML(pid, qty, inStock) {
    if (!inStock) {
      return `<button disabled class="flex w-full items-center justify-center gap-2 rounded-xl border border-line py-2 text-xs font-semibold opacity-40">ناموجود</button>`;
    }
    if (qty > 0) {
      return `<div class="flex items-center justify-between rounded-xl border border-emerald bg-emerald-soft px-2 py-1.5">
        <button type="button" class="pc-dec flex h-7 w-7 items-center justify-center rounded-lg bg-white text-emerald text-base font-bold leading-none" data-pid="${pid}" aria-label="کم کردن">−</button>
        <span class="ticker text-sm font-bold text-emerald-deep" data-pc-qty="${pid}">${toFaPC(qty)}</span>
        <button type="button" class="pc-inc flex h-7 w-7 items-center justify-center rounded-lg bg-white text-emerald text-base font-bold leading-none" data-pid="${pid}" aria-label="زیاد کردن">+</button>
      </div>`;
    }
    return `<button type="button" class="pc-add flex w-full items-center justify-center gap-2 rounded-xl border border-line py-2 text-xs font-semibold hover:border-emerald hover:text-emerald" data-pid="${pid}">
      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/><path d="M1 1h4l2.7 13.4a2 2 0 0 0 2 1.6h9.7a2 2 0 0 0 2-1.6L23 6H6"/></svg>
      افزودن به سبد
    </button>`;
  }

  // نسخه‌ی بزرگ همون دکمه/استپر، برای صفحه‌ی محصول (دکمه‌ی اصلی + نوار چسبان موبایل)؛
  // از همون کلاس‌های pc-add/pc-inc/pc-dec استفاده می‌کنه، پس با bind() یکسان کار می‌کنه
  function cartAreaHTMLLarge(pid, qty, inStock) {
    if (!inStock) {
      return `<button disabled class="flex flex-1 w-full items-center justify-center gap-2 rounded-2xl border border-line py-3 text-sm font-bold opacity-40">ناموجود</button>`;
    }
    if (qty > 0) {
      return `<div class="flex flex-1 items-center justify-between rounded-2xl border border-emerald bg-emerald-soft px-3 py-2">
        <button type="button" class="pc-dec flex h-9 w-9 items-center justify-center rounded-xl bg-white text-emerald text-lg font-bold leading-none" data-pid="${pid}" aria-label="کم کردن">−</button>
        <span class="ticker text-base font-bold text-emerald-deep" data-pc-qty="${pid}">${toFaPC(qty)}</span>
        <button type="button" class="pc-inc flex h-9 w-9 items-center justify-center rounded-xl bg-white text-emerald text-lg font-bold leading-none" data-pid="${pid}" aria-label="زیاد کردن">+</button>
      </div>`;
    }
    return `<button type="button" class="pc-add flex flex-1 items-center justify-center gap-2 rounded-2xl bg-emerald bg-emerald-hover py-3 text-sm font-bold text-white transition-colors duration-200" data-pid="${pid}">
      <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/><path d="M1 1h4l2.7 13.4a2 2 0 0 0 2 1.6h9.7a2 2 0 0 0 2-1.6L23 6H6"/></svg>
      افزودن به سبد خرید
    </button>`;
  }

  function render(p) {
    const price = p.discountPrice || p.price;
    const oldPrice = p.discountPrice ? p.price : null;
    const inWishlist = wishlistState.has(p.id);
    const entry = cartState.get(p.id);
    const qty = entry ? entry.quantity : 0;
    const productUrl = `product.html?slug=${encodeURIComponent(p.slug)}`;

    const badge = p.badge
      ? `<span class="absolute right-3 top-3 rounded-full px-2.5 py-1 text-[11px] font-semibold ${p.badge === 'جدید' ? 'bg-emerald-soft text-emerald' : 'bg-danger/10 text-danger'}">${escapeHtmlPC(p.badge)}</span>`
      : '';
    const outOfStock = !p.inStock
      ? `<div class="absolute inset-0 flex items-center justify-center bg-surface/80 backdrop-blur-[1px]"><span class="rounded-full bg-danger/10 px-3 py-1.5 text-xs font-semibold text-danger">ناموجود</span></div>`
      : '';

    return `<div class="tilt-card product-card w-full rounded-2xl border border-line bg-surface p-3" data-product-id="${p.id}">
      <a href="${productUrl}" class="relative block aspect-square overflow-hidden rounded-xl bg-media">
        <img src="${p.mainImageUrl || 'https://picsum.photos/400/400'}" class="h-full w-full object-cover" loading="lazy" alt="${escapeHtmlPC(p.name)}" />
        ${badge}${outOfStock}
      </a>
      <button type="button" class="pc-wishlist-btn absolute left-3 top-3 flex h-9 w-9 items-center justify-center rounded-full bg-surface/90 ${inWishlist ? 'text-danger' : 'text-foreground/70'}" data-pid="${p.id}" aria-label="افزودن به علاقه‌مندی‌ها">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="${inWishlist ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="2"><path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.8 1-1a5.5 5.5 0 0 0 0-7.8Z"/></svg>
      </button>
      <div class="mt-3 px-1">
        <div class="text-xs text-muted">${escapeHtmlPC(p.categoryName || '')}</div>
        <a href="${productUrl}"><h3 class="mt-1 text-sm font-semibold leading-tight hover:text-emerald">${escapeHtmlPC(p.name)}</h3></a>
        <div class="mt-1.5 flex items-center gap-1 text-xs text-muted">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor" class="text-gold"><path d="m12 2 3.1 6.3 6.9 1-5 4.9 1.2 6.8L12 17.8 5.8 21l1.2-6.8-5-4.9 6.9-1Z"/></svg>
          <span class="ticker">${p.averageRating ?? 0}</span><span>(${toFaPC(p.reviewCount ?? 0)})</span>
        </div>
        <div class="price-row mt-2.5">
          <span class="ticker text-[15px] font-bold">${fmtPC(price)}</span>
          <span class="text-[11px] text-muted">تومان</span>
          ${oldPrice ? `<span class="old-price ticker text-xs text-muted line-through">${fmtPC(oldPrice)}</span>` : ''}
        </div>
        <div class="pc-cart-area mt-3" data-pc-cart-area="${p.id}">${cartAreaHTML(p.id, qty, p.inStock)}</div>
      </div>
    </div>`;
  }

  function refreshCartArea(pid) {
    const entry = cartState.get(pid);
    const qty = entry ? entry.quantity : 0;
    document.querySelectorAll(`[data-pc-cart-area="${pid}"]`).forEach(el => {
      const render = el.dataset.pcSize === 'large' ? cartAreaHTMLLarge : cartAreaHTML;
      el.innerHTML = render(pid, qty, true);
    });
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

  function syncCartFromDto(cart) {
    cartState = new Map();
    (cart.items || []).forEach(i => cartState.set(i.productId, { cartItemId: i.id, quantity: i.quantity }));
  }

  async function handleAdd(pid) {
    try {
      const cart = await Api.addToCart(pid, 1);
      syncCartFromDto(cart);
      refreshCartArea(pid);
      if (typeof updateCartBadge === 'function') updateCartBadge();
      if (typeof showToast === 'function') showToast('success', 'به سبد خرید اضافه شد');
    } catch (e) {
      if (typeof showToast === 'function') showToast('error', e.message || 'خطا در افزودن به سبد خرید');
    }
  }

  async function handleQtyChange(pid, delta) {
    const entry = cartState.get(pid);
    if (!entry) return;
    const newQty = entry.quantity + delta;
    try {
      const cart = newQty <= 0
        ? await Api.removeCartItem(entry.cartItemId)
        : await Api.updateCartItem(entry.cartItemId, newQty);
      syncCartFromDto(cart);
      refreshCartArea(pid);
      if (typeof updateCartBadge === 'function') updateCartBadge();
    } catch (e) {
      if (typeof showToast === 'function') showToast('error', e.message || 'خطا در بروزرسانی سبد خرید');
    }
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
      const addBtn = e.target.closest('.pc-add');
      const incBtn = e.target.closest('.pc-inc');
      const decBtn = e.target.closest('.pc-dec');
      const wishBtn = e.target.closest('.pc-wishlist-btn');

      if (addBtn) { e.preventDefault(); handleAdd(parseInt(addBtn.dataset.pid, 10)); }
      else if (incBtn) { e.preventDefault(); handleQtyChange(parseInt(incBtn.dataset.pid, 10), 1); }
      else if (decBtn) { e.preventDefault(); handleQtyChange(parseInt(decBtn.dataset.pid, 10), -1); }
      else if (wishBtn) { e.preventDefault(); e.stopPropagation(); handleWishlistToggle(parseInt(wishBtn.dataset.pid, 10)); }
    });
  }

  function getCartQty(pid) {
    const entry = cartState.get(pid);
    return entry ? entry.quantity : 0;
  }

  // برای صفحاتی که کارت کامل ProductCard.render رو نمی‌خوان ولی به همون استپر تعداد نیاز دارن
  // (مثلا تب علاقه‌مندی‌های account.html که ساختار کارتش فرق داره). چون bind() بر اساس
  // کلاس‌های .pc-add/.pc-inc/.pc-dec عمل می‌کنه، همین markup داخل هر کارت سفارشی هم کار می‌کنه.
  function renderCartArea(pid, inStock = true) {
    return cartAreaHTML(pid, getCartQty(pid), inStock);
  }

  return { loadState, render, bind, resolvePendingWishlist, getCartQty, renderCartArea };
})();
