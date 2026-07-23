/* ==============================================================
   Atelier — جستجوی زنده (پیشنهاد لحظه‌ای همراه با عکس + اینتر برای جستجوی کلی)
   بعد از js/api.js لود بشه. هم تو هدر دسکتاپ (partials/header.html، توسط
   js/layout.js صدا زده می‌شه) و هم تو شیت جستجوی موبایل (index.html) استفاده می‌شه.
   ============================================================== */

const AtelierSearch = (function () {
  function debounce(fn, ms) {
    let timer;
    return (...args) => {
      clearTimeout(timer);
      timer = setTimeout(() => fn(...args), ms);
    };
  }

  function escapeHtmlS(str) {
    return String(str ?? '').replace(/[&<>"']/g, s => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[s]));
  }

  function suggestionItemHTML(p) {
    const price = p.discountPrice || p.price;
    return `<a href="product.html?slug=${encodeURIComponent(p.slug)}" class="flex items-center gap-3 px-4 py-2.5 text-right hover:bg-surface-muted">
      <img src="${p.imageUrl || 'https://picsum.photos/100/100'}" class="h-10 w-10 shrink-0 rounded-lg object-cover bg-media" />
      <div class="min-w-0 flex-1">
        <div class="truncate text-sm">${escapeHtmlS(p.name)}</div>
        <div class="ticker text-xs text-muted">${Number(price || 0).toLocaleString('en-US')} تومان</div>
      </div>
    </a>`;
  }

  function goToShopSearch(q) {
    window.location.href = 'shop.html?search=' + encodeURIComponent(q);
  }

  function attach(input, dropdown, wrapper) {
    if (!input || !dropdown) return;

    const runSearch = debounce(async () => {
      const q = input.value.trim();
      if (q.length < 2) {
        dropdown.classList.add('hidden');
        dropdown.innerHTML = '';
        return;
      }
      try {
        const results = await Api.searchSuggestions(q);
        dropdown.innerHTML = results.length
          ? results.map(suggestionItemHTML).join('') + `<button type="button" data-search-all class="block w-full border-t border-line px-4 py-2.5 text-center text-xs font-semibold text-emerald hover:bg-surface-muted">مشاهده همه نتایج برای «${escapeHtmlS(q)}»</button>`
          : `<p class="px-4 py-3 text-center text-xs text-muted">نتیجه‌ای یافت نشد</p>`;
        dropdown.classList.remove('hidden');

        const allBtn = dropdown.querySelector('[data-search-all]');
        if (allBtn) allBtn.addEventListener('click', () => goToShopSearch(q));
      } catch (e) {
        dropdown.classList.add('hidden');
      }
    }, 300);

    input.addEventListener('input', runSearch);
    input.addEventListener('focus', () => {
      if (input.value.trim().length >= 2 && dropdown.innerHTML) dropdown.classList.remove('hidden');
    });
    input.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        const q = input.value.trim();
        if (q) goToShopSearch(q);
      }
    });
    document.addEventListener('click', (e) => {
      if (dropdown.classList.contains('hidden')) return;
      if (wrapper && !wrapper.contains(e.target)) dropdown.classList.add('hidden');
    });
  }

  return { attach };
})();
