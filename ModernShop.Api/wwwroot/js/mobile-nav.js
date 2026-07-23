/* ==============================================================
   Atelier — نوار پایین موبایل + شیت‌های دسته‌بندی/جستجو/منو
   این کامپوننت داخل partials/mobile-nav.html هست و از طریق js/layout.js رو هر
   صفحه‌ای تزریق می‌شه؛ باید بعد از js/api.js و js/search.js لود بشه.

   قبلاً هر صفحه یا از این partial استفاده می‌کرد (با یک تب «حساب/ورود» که فقط
   لینک بود) یا index.html یک نسخه‌ی کاملاً جدا و دستی از منوی پایین (با شیت
   دسته‌بندی/جستجو/منو) داشت. این دوتایی بودن باعث می‌شد فقط index.html درست
   کار کنه و بقیه‌ی صفحات یک تجربه‌ی متفاوت (و به‌قول کاربر گیج‌کننده) داشته
   باشن. الان فقط همین یک پیاده‌سازی هست و همه‌ی صفحات (از جمله خود index.html)
   دقیقاً از همینِ یکی استفاده می‌کنن — پس دیگه هیچ‌وقت از هم عقب نمی‌افتن.
   ============================================================== */
const MobileNavSheet = (function () {
  let categoriesData = [];
  let currentSheet = null;

  // فقط صفحاتی که واقعاً یک تب مستقیم (نه شیت) دارن این‌جا نگاشت می‌شن؛ بقیه‌ی صفحات
  // (فروشگاه/محصول/تسویه‌حساب/حساب کاربری/وبلاگ/...) هیچ‌کدوم از ۵ تب رو «فعال» نشون نمی‌دن
  const PAGE_TAB_MAP = { 'index.html': 'home', 'cart.html': 'cart', 'checkout.html': 'cart' };

  const sheetTitles = { categories: 'دسته‌بندی‌ها', search: 'جستجو', menu: 'منو' };

  function menuSheetHTML() {
    return `<div class="flex flex-col gap-1">
      ${[["صفحه اصلی", "index.html"], ["پوشاک", "shop.html?category=fashion"], ["دیجیتال", "shop.html?category=digital"], ["خانه و آشپزخانه", "shop.html?category=home-kitchen"], ["زیبایی", "shop.html?category=beauty-health"], ["وبلاگ", "blog.html"]]
        .map(([l, href]) => `<a href="${href}" class="rounded-xl px-3 py-3 text-sm font-medium hover:bg-surface-muted">${l}</a>`).join('')}
    </div>`;
  }

  function categoriesSheetHTML() {
    if (categoriesData.length === 0) {
      return `<p class="py-10 text-center text-sm text-muted">هنوز دسته‌بندی‌ای ثبت نشده است</p>`;
    }
    return `<div class="grid grid-cols-2 gap-3">${categoriesData.map(c => `
      <a href="shop.html?category=${c.id}" class="group relative aspect-[4/3] overflow-hidden rounded-2xl text-right">
        <img src="${c.image}" class="h-full w-full object-cover" alt="${c.title}" />
        <div class="absolute inset-0 bg-gradient-to-t from-black/70 via-black/10 to-transparent"></div>
        <div class="absolute bottom-3 right-3 text-white">
          <div class="text-sm font-bold">${c.title}</div>
          <div class="text-[11px] text-white/80">${c.subtitle}</div>
        </div>
      </a>`).join('')}</div>`;
  }

  function searchSheetHTML() {
    return `<div id="mnav-search-wrap" class="relative">
      <div class="flex items-center gap-2 rounded-full border border-line bg-surface-muted px-4 py-3">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="shrink-0 text-muted"><circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/></svg>
        <input id="mnav-search-input" autofocus type="text" autocomplete="off" placeholder="جستجوی کالا، برند..." class="w-full bg-transparent text-sm outline-none placeholder:text-muted" />
      </div>
      <div id="mnav-search-dropdown" class="hidden mt-2 max-h-80 overflow-y-auto rounded-2xl border border-line bg-surface py-1.5 shadow-lg"></div>
    </div>`;
  }

  const sheets = { categories: categoriesSheetHTML, search: searchSheetHTML, menu: menuSheetHTML };

  function setActiveTab(tab) {
    document.querySelectorAll('.mnav-tab').forEach(btn => {
      const active = btn.dataset.tab === tab;
      btn.classList.toggle('text-emerald', active);
      btn.classList.toggle('text-muted', !active);
    });
  }

  function open(type) {
    if (currentSheet === type) { close(); return; }
    // اگه صفحه‌ای مثل shop.html شیت مخصوص خودش (فیلتر/مرتب‌سازی) رو باز داره،
    // اول همون بسته بشه که این شیت روش قرار نگیره، بلافاصله بعدش این شیت باز بشه
    if (typeof closeSheet === 'function') closeSheet();
    currentSheet = type;
    const title = document.getElementById('mnav-sheet-title');
    const body = document.getElementById('mnav-sheet-body');
    if (!title || !body) return;

    title.textContent = sheetTitles[type];
    body.innerHTML = sheets[type]();

    if (type === 'search' && typeof AtelierSearch === 'object') {
      AtelierSearch.attach(
        document.getElementById('mnav-search-input'),
        document.getElementById('mnav-search-dropdown'),
        document.getElementById('mnav-search-wrap')
      );
    }

    document.getElementById('mnav-sheet-overlay').classList.remove('hidden');
    const panel = document.getElementById('mnav-sheet-panel');
    panel.classList.remove('hidden');
    requestAnimationFrame(() => panel.classList.remove('translate-y-full'));
    setActiveTab(type);
  }

  function close() {
    currentSheet = null;
    const panel = document.getElementById('mnav-sheet-panel');
    if (!panel) return;
    panel.classList.add('translate-y-full');
    document.getElementById('mnav-sheet-overlay').classList.add('hidden');
    setTimeout(() => panel.classList.add('hidden'), 300);

    const currentPath = window.location.pathname.split('/').pop() || 'index.html';
    setActiveTab(PAGE_TAB_MAP[currentPath] || null);
  }

  async function init() {
    if (!document.getElementById('mnav-sheet-overlay')) return; // partial هنوز تزریق نشده

    try {
      const cats = await Api.getCategories();
      categoriesData = cats.map(c => ({ id: c.slug, title: c.name, subtitle: `${c.productCount} کالای جدید`, image: c.imageUrl }));
    } catch (e) {
      categoriesData = [];
    }

    const currentPath = window.location.pathname.split('/').pop() || 'index.html';
    setActiveTab(PAGE_TAB_MAP[currentPath] || null);
  }

  return { open, close, init };
})();
