/* ==============================================================
   Atelier — کامپوننت مشترک کارت محصول (استپر تعداد + علاقه‌مندی + انتخاب تنوع)
   این فایل باید بعد از js/api.js و js/color-map.js لود بشه (برای محصول متغیر،
   پاپ‌آپ انتخاب رنگ/سایز از Api.getProduct و ColorMap استفاده می‌کنه). روی هر
   صفحه‌ای که کارت محصول
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

  let cartState = new Map();      // productId -> { totalQuantity, cartItemId (فقط وقتی lineCount===1 معتبره), lineCount }
  let wishlistState = new Set();  // productId
  let productMeta = new Map();    // productId -> { isVariable, slug } — برای این‌که پاپ‌آپ انتخاب تنوع بدونه چی رو بگیره
  const productDetailCache = new Map(); // slug -> ProductDetailDto (تنوع‌ها فقط موقع باز شدن پاپ‌آپ، یک‌بار گرفته می‌شه)
  let quickAdd = null; // { pid, detail, selectedColor, selectedVariantId, qty } — وضعیت پاپ‌آپ باز

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

  // محصول متغیر می‌تونه چند خط سبد جدا (هر تنوع یک خط) داشته باشه؛ برای نمایش رو کارت
  // فقط جمع تعداد لازمه، ولی cartItemId فقط وقتی معتبره که دقیقاً یک خط داشته باشیم
  // (وگرنه معلوم نیست +/- باید کدوم تنوع رو تغییر بده)
  function buildCartStateFromItems(items) {
    const map = new Map();
    (items || []).forEach(i => {
      const existing = map.get(i.productId);
      if (existing) {
        existing.totalQuantity += i.quantity;
        existing.lineCount += 1;
        existing.cartItemId = null;
      } else {
        map.set(i.productId, { totalQuantity: i.quantity, cartItemId: i.id, lineCount: 1 });
      }
    });
    return map;
  }

  async function loadState() {
    try {
      const cart = await Api.getCart();
      cartState = buildCartStateFromItems(cart.items);
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

  // برای محصول متغیر (چندتا تنوع رنگ/سایز)، دکمه‌ی +/- مستقیم نشون داده نمی‌شه چون معلوم
  // نیست باید کدوم خط سبد (کدوم تنوع) رو تغییر بده؛ به‌جاش با کلیک روی دکمه، همین‌جا رو خود
  // کارت یک پاپ‌آپ کوچیک (quick-add) باز می‌شه که توش رنگ/سایز و تعداد رو انتخاب می‌کنه و
  // بدون خارج شدن از صفحه به سبد اضافه می‌شه (نسخه قبلی کاربر رو به صفحه محصول می‌فرستاد که
  // تجربه‌ی خوبی نبود)
  function cartAreaHTML(pid, qty, inStock) {
    const meta = productMeta.get(pid) || {};
    if (!inStock) {
      return `<button disabled class="flex w-full items-center justify-center gap-2 rounded-xl border border-line py-2 text-xs font-semibold opacity-40">ناموجود</button>`;
    }
    if (meta.isVariable) {
      if (qty > 0) {
        return `<button type="button" class="pc-quickadd flex w-full items-center justify-center gap-1.5 rounded-xl border border-emerald bg-emerald-soft py-2 text-xs font-semibold text-emerald-deep" data-pid="${pid}">
          <span class="ticker" data-pc-qty="${pid}">${qty}</span> عدد در سبد · افزودن بیشتر
        </button>`;
      }
      return `<button type="button" class="pc-quickadd flex w-full items-center justify-center gap-2 rounded-xl border border-line py-2 text-xs font-semibold hover:border-emerald hover:text-emerald" data-pid="${pid}">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/><path d="M1 1h4l2.7 13.4a2 2 0 0 0 2 1.6h9.7a2 2 0 0 0 2-1.6L23 6H6"/></svg>
        افزودن به سبد
      </button>`;
    }
    if (qty > 0) {
      return `<div class="flex items-center justify-between rounded-xl border border-emerald bg-emerald-soft px-2 py-1.5">
        <button type="button" class="pc-dec flex h-7 w-7 items-center justify-center rounded-lg bg-white text-emerald text-base font-bold leading-none" data-pid="${pid}" aria-label="کم کردن">−</button>
        <span class="ticker text-sm font-bold text-emerald-deep" data-pc-qty="${pid}">${qty}</span>
        <button type="button" class="pc-inc flex h-7 w-7 items-center justify-center rounded-lg bg-white text-emerald text-base font-bold leading-none" data-pid="${pid}" aria-label="زیاد کردن">+</button>
      </div>`;
    }
    return `<button type="button" class="pc-add flex w-full items-center justify-center gap-2 rounded-xl border border-line py-2 text-xs font-semibold hover:border-emerald hover:text-emerald" data-pid="${pid}">
      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/><path d="M1 1h4l2.7 13.4a2 2 0 0 0 2 1.6h9.7a2 2 0 0 0 2-1.6L23 6H6"/></svg>
      افزودن به سبد
    </button>`;
  }

  function render(p) {
    const price = p.discountPrice || p.price;
    const oldPrice = p.discountPrice ? p.price : null;
    const inWishlist = wishlistState.has(p.id);
    const entry = cartState.get(p.id);
    const qty = entry ? entry.totalQuantity : 0;
    const productUrl = `product.html?slug=${encodeURIComponent(p.slug)}`;
    productMeta.set(p.id, { isVariable: !!p.isVariable, slug: p.slug });

    const badge = p.badge
      ? `<span class="absolute right-3 top-3 rounded-full px-2.5 py-1 text-[11px] font-semibold ${p.badge === 'جدید' ? 'bg-emerald-soft text-emerald' : 'bg-danger/10 text-danger'}">${escapeHtmlPC(p.badge)}</span>`
      : '';
    const outOfStock = !p.inStock
      ? `<div class="absolute inset-0 flex items-center justify-center bg-surface/80 backdrop-blur-[1px]"><span class="rounded-full bg-danger/10 px-3 py-1.5 text-xs font-semibold text-danger">ناموجود</span></div>`
      : '';

    return `<div class="tilt-card product-card relative w-full rounded-2xl border border-line bg-surface p-3" data-product-id="${p.id}">
      <a href="${productUrl}" class="relative block aspect-square overflow-hidden rounded-xl bg-media">
        <img src="${p.mainImageUrl || 'https://picsum.photos/400/400'}" class="h-full w-full object-cover" loading="lazy" alt="${escapeHtmlPC(p.name)}" />
        ${badge}${outOfStock}
      </a>
      <button type="button" class="pc-wishlist-btn absolute left-3 top-3 z-10 flex h-9 w-9 items-center justify-center rounded-full bg-surface shadow-sm ${inWishlist ? 'text-danger' : 'text-foreground/70'}" data-pid="${p.id}" aria-label="افزودن به علاقه‌مندی‌ها">
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
    const qty = entry ? entry.totalQuantity : 0;
    document.querySelectorAll(`[data-pc-cart-area="${pid}"]`).forEach(el => {
      el.innerHTML = cartAreaHTML(pid, qty, true);
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
    cartState = buildCartStateFromItems(cart.items);
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
    if (!entry || !entry.cartItemId) return;
    const newQty = entry.totalQuantity + delta;
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

  /* ================= پاپ‌آپ انتخاب تنوع (quick-add) ================= */

  function ensureQuickAddModal() {
    if (document.getElementById('pc-quickadd-overlay')) return;

    const overlay = document.createElement('div');
    overlay.id = 'pc-quickadd-overlay';
    overlay.className = 'fixed inset-0 z-[205] hidden items-end justify-center bg-black/40 sm:items-center sm:p-4';
    overlay.innerHTML = `
      <div class="max-h-[85vh] w-full overflow-y-auto rounded-t-3xl bg-surface p-5 sm:max-w-sm sm:rounded-3xl">
        <div class="mb-4 flex items-center justify-between">
          <h3 class="text-sm font-bold">انتخاب تنوع</h3>
          <button type="button" data-pc-qa-close class="rounded-full p-1.5 text-foreground/70 hover:bg-surface-muted" aria-label="بستن">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 6 6 18M6 6l12 12"/></svg>
          </button>
        </div>
        <div id="pc-quickadd-body"></div>
      </div>`;

    overlay.addEventListener('click', (e) => {
      if (e.target === overlay) { closeQuickAdd(); return; }
      const closeBtn = e.target.closest('[data-pc-qa-close]');
      const colorBtn = e.target.closest('[data-pc-qa-color]');
      const variantBtn = e.target.closest('[data-pc-qa-variant]');
      const qtyBtn = e.target.closest('[data-pc-qa-qty]');
      const confirmBtn = e.target.closest('[data-pc-qa-confirm]');

      if (closeBtn) closeQuickAdd();
      else if (colorBtn) quickAddSelectColor(colorBtn.dataset.pcQaColor);
      else if (variantBtn) quickAddSelectVariant(parseInt(variantBtn.dataset.pcQaVariant, 10));
      else if (qtyBtn) quickAddChangeQty(parseInt(qtyBtn.dataset.pcQaQty, 10));
      else if (confirmBtn) quickAddConfirm();
    });

    document.body.appendChild(overlay);
  }

  function showQuickAddOverlay() {
    const overlay = document.getElementById('pc-quickadd-overlay');
    overlay.classList.remove('hidden');
    overlay.classList.add('flex');
  }

  function closeQuickAdd() {
    const overlay = document.getElementById('pc-quickadd-overlay');
    if (overlay) { overlay.classList.add('hidden'); overlay.classList.remove('flex'); }
    quickAdd = null;
  }

  async function openQuickAdd(pid) {
    const meta = productMeta.get(pid);
    if (!meta) return;

    ensureQuickAddModal();
    quickAdd = { pid, detail: null, selectedColor: null, selectedVariantId: null, qty: 1 };
    showQuickAddOverlay();
    renderQuickAddBody();

    let detail = productDetailCache.get(meta.slug);
    if (!detail) {
      try {
        detail = await Api.getProduct(meta.slug);
        productDetailCache.set(meta.slug, detail);
      } catch (e) {
        closeQuickAdd();
        if (typeof showToast === 'function') showToast('error', 'خطا در دریافت اطلاعات محصول');
        return;
      }
    }
    if (!quickAdd || quickAdd.pid !== pid) return; // تا رسیدن جواب، کاربر مودال رو بسته یا محصول دیگه‌ای باز کرده

    const variants = detail.variants || [];
    const firstInStock = variants.find(v => v.stockQuantity > 0) || variants[0] || null;
    quickAdd.detail = detail;
    quickAdd.selectedColor = firstInStock ? (firstInStock.color || '—') : null;
    quickAdd.selectedVariantId = firstInStock ? firstInStock.id : null;
    quickAdd.qty = 1;

    renderQuickAddBody();
  }

  function quickAddSelectColor(color) {
    if (!quickAdd || !quickAdd.detail) return;
    quickAdd.selectedColor = color;
    const forColor = (quickAdd.detail.variants || []).filter(v => (v.color || '—') === color);
    const sizesForColor = forColor.filter(v => v.size && v.size !== '-');
    const chosen = sizesForColor[0] || forColor[0] || null;
    quickAdd.selectedVariantId = chosen ? chosen.id : null;
    quickAdd.qty = 1;
    renderQuickAddBody();
  }

  function quickAddSelectVariant(variantId) {
    if (!quickAdd) return;
    quickAdd.selectedVariantId = variantId;
    quickAdd.qty = 1;
    renderQuickAddBody();
  }

  function quickAddChangeQty(delta) {
    if (!quickAdd || !quickAdd.detail) return;
    const variant = (quickAdd.detail.variants || []).find(v => v.id === quickAdd.selectedVariantId);
    const stock = variant ? variant.stockQuantity : 0;
    const next = quickAdd.qty + delta;
    if (next < 1 || next > stock) return;
    quickAdd.qty = next;
    renderQuickAddBody();
  }

  async function quickAddConfirm() {
    if (!quickAdd || !quickAdd.selectedVariantId) return;
    const { pid, selectedVariantId, qty } = quickAdd;
    try {
      const cart = await Api.addToCart(pid, qty, selectedVariantId);
      syncCartFromDto(cart);
      refreshCartArea(pid);
      if (typeof updateCartBadge === 'function') updateCartBadge();
      if (typeof showToast === 'function') showToast('success', 'به سبد خرید اضافه شد');
      closeQuickAdd();
    } catch (e) {
      if (typeof showToast === 'function') showToast('error', e.message || 'خطا در افزودن به سبد خرید');
    }
  }

  function renderQuickAddBody() {
    const body = document.getElementById('pc-quickadd-body');
    if (!body || !quickAdd) return;

    if (!quickAdd.detail) {
      body.innerHTML = `<div class="flex items-center justify-center py-10 text-xs text-muted">در حال بارگذاری...</div>`;
      return;
    }

    const detail = quickAdd.detail;
    const variants = detail.variants || [];

    if (!variants.length) {
      body.innerHTML = `<div class="py-6 text-center text-xs text-muted">این محصول تنوعی ندارد.</div>`;
      return;
    }

    const variantsByColor = new Map();
    variants.forEach(v => {
      const key = v.color || '—';
      if (!variantsByColor.has(key)) variantsByColor.set(key, []);
      variantsByColor.get(key).push(v);
    });
    const colors = [...variantsByColor.keys()];

    const sizesForColor = (variantsByColor.get(quickAdd.selectedColor) || []).filter(v => v.size && v.size !== '-');
    const currentVariant = variants.find(v => v.id === quickAdd.selectedVariantId) || null;
    const stock = currentVariant ? currentVariant.stockQuantity : 0;
    const basePrice = detail.discountPrice || detail.price;
    const finalPrice = basePrice + (currentVariant?.priceAdjustment || 0);
    const mainImage = (detail.images || []).find(i => i.isMain) || detail.images?.[0];

    body.innerHTML = `
      <div class="mb-4 flex items-center gap-3">
        <img src="${mainImage?.imageUrl || 'https://picsum.photos/200/200'}" class="h-14 w-14 rounded-xl object-cover" alt="" />
        <div>
          <p class="text-sm font-semibold leading-snug">${escapeHtmlPC(detail.name)}</p>
          <p class="mt-1 text-sm font-bold text-emerald-deep"><span class="ticker">${fmtPC(finalPrice)}</span> <span class="text-[11px] font-normal text-muted">تومان</span></p>
        </div>
      </div>

      ${colors.length ? `
      <div class="mb-4">
        <p class="mb-2 text-xs font-semibold text-muted">رنگ: <span class="text-foreground">${escapeHtmlPC(quickAdd.selectedColor || '')}</span></p>
        <div class="flex flex-wrap items-center gap-2">
          ${colors.map(c => `
            <button type="button" data-pc-qa-color="${escapeHtmlPC(c)}" class="flex h-9 w-9 items-center justify-center rounded-full border-2 p-0.5 ${c === quickAdd.selectedColor ? 'border-emerald' : 'border-transparent'}" aria-label="${escapeHtmlPC(c)}">
              <span class="block h-full w-full rounded-full border border-line" style="background:${ColorMap.hexFor(c)}"></span>
            </button>`).join('')}
        </div>
      </div>` : ''}

      ${sizesForColor.length ? `
      <div class="mb-4">
        <p class="mb-2 text-xs font-semibold text-muted">سایز</p>
        <div class="flex flex-wrap gap-2">
          ${sizesForColor.map(v => `
            <button type="button" data-pc-qa-variant="${v.id}" class="rounded-xl border-2 px-3.5 py-1.5 text-xs font-semibold ${v.id === quickAdd.selectedVariantId ? 'border-emerald text-emerald' : 'border-line'}">${escapeHtmlPC(v.size)}</button>`).join('')}
        </div>
      </div>` : ''}

      <div class="mb-4 flex items-center justify-between rounded-xl border border-line px-3 py-2">
        <span class="text-xs font-semibold text-muted">تعداد</span>
        <div class="flex items-center gap-3">
          <button type="button" data-pc-qa-qty="-1" class="flex h-8 w-8 items-center justify-center rounded-lg bg-surface-muted text-base font-bold leading-none disabled:opacity-40" ${quickAdd.qty <= 1 ? 'disabled' : ''}>−</button>
          <span class="ticker w-6 text-center text-sm font-bold">${toFaPC(quickAdd.qty)}</span>
          <button type="button" data-pc-qa-qty="1" class="flex h-8 w-8 items-center justify-center rounded-lg bg-surface-muted text-base font-bold leading-none disabled:opacity-40" ${quickAdd.qty >= stock ? 'disabled' : ''}>+</button>
        </div>
      </div>

      ${stock <= 0
        ? `<button type="button" disabled class="w-full rounded-xl border border-line py-2.5 text-sm font-semibold opacity-40">این تنوع موجود نیست</button>`
        : `<button type="button" data-pc-qa-confirm class="w-full rounded-xl bg-emerald py-2.5 text-sm font-semibold text-white hover:bg-emerald-deep">افزودن به سبد</button>`}
    `;
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
      const quickAddBtn = e.target.closest('.pc-quickadd');

      if (addBtn) { e.preventDefault(); handleAdd(parseInt(addBtn.dataset.pid, 10)); }
      else if (incBtn) { e.preventDefault(); handleQtyChange(parseInt(incBtn.dataset.pid, 10), 1); }
      else if (decBtn) { e.preventDefault(); handleQtyChange(parseInt(decBtn.dataset.pid, 10), -1); }
      else if (wishBtn) { e.preventDefault(); e.stopPropagation(); handleWishlistToggle(parseInt(wishBtn.dataset.pid, 10)); }
      else if (quickAddBtn) { e.preventDefault(); openQuickAdd(parseInt(quickAddBtn.dataset.pid, 10)); }
    });
  }

  function getCartQty(pid) {
    const entry = cartState.get(pid);
    return entry ? entry.totalQuantity : 0;
  }

  // برای صفحاتی که کارت کامل ProductCard.render رو نمی‌خوان ولی به همون استپر تعداد نیاز دارن
  // (مثلا تب علاقه‌مندی‌های account.html که ساختار کارتش فرق داره). چون bind() بر اساس
  // کلاس‌های .pc-add/.pc-inc/.pc-dec عمل می‌کنه، همین markup داخل هر کارت سفارشی هم کار می‌کنه.
  function renderCartArea(pid, inStock = true) {
    return cartAreaHTML(pid, getCartQty(pid), inStock);
  }

  return { loadState, render, bind, resolvePendingWishlist, getCartQty, renderCartArea };
})();
