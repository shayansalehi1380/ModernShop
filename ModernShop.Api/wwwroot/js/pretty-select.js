/* ==============================================================
   کامپوننت دراپ‌داون سفارشی (جایگزین select بومی مرورگر)
   لیست باز شونده‌ی select بومی مرورگر (روی همه‌ی مرورگرها) گوشه‌تیز و
   غیرقابل استایل‌دهیه؛ این کامپوننت یه لیست کاملاً گرد، سفید با متن مشکی
   و حاشیه‌ی سبز روی گزینه‌ی انتخاب‌شده می‌سازه، ولی خود <select> اصلی رو
   (مخفی ولی زنده) نگه می‌داره تا هر کدی که با .value/.disabled/رویداد
   change کار می‌کنه بدون تغییر درست کار کنه.
   ============================================================== */
const PrettySelect = (function () {
  function escapeHtmlPS(str) {
    return String(str ?? '').replace(/[&<>"']/g, s => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[s]));
  }

  function attach(selectEl) {
    const wrap = document.createElement('div');
    wrap.className = 'pretty-select-wrap relative';
    selectEl.parentNode.insertBefore(wrap, selectEl);
    wrap.appendChild(selectEl);
    selectEl.style.display = 'none';
    selectEl.setAttribute('aria-hidden', 'true');
    selectEl.tabIndex = -1;

    const baseTriggerClass = selectEl.className || '';
    const trigger = document.createElement('button');
    trigger.type = 'button';
    // اگه خود select قبلاً w-full داشت (مثل فیلدهای فرم چک‌اوت)، همون حفظ می‌شه؛
    // وگرنه (مثل دراپ‌داون مرتب‌سازی که یه pill کوچیکه) عرضش با محتوا تعیین می‌شه
    const widthClass = /\bw-full\b/.test(baseTriggerClass) ? 'w-full' : '';
    trigger.className = `${baseTriggerClass} inline-flex ${widthClass} items-center justify-between gap-2 text-right`;
    wrap.appendChild(trigger);
    if (widthClass) wrap.classList.add('w-full');

    const list = document.createElement('div');
    list.className = 'ps-list hidden absolute right-0 z-30 mt-1.5 max-h-64 w-full min-w-[180px] overflow-y-auto whitespace-nowrap rounded-2xl border border-line bg-white p-1.5 text-black shadow-lg';
    list.setAttribute('role', 'listbox');
    wrap.appendChild(list);

    function currentLabel() {
      const opt = selectEl.options[selectEl.selectedIndex];
      return opt ? opt.textContent : '';
    }

    function renderTrigger() {
      trigger.disabled = selectEl.disabled;
      trigger.classList.toggle('cursor-not-allowed', selectEl.disabled);
      trigger.classList.toggle('opacity-60', selectEl.disabled);
      const hasValue = !!selectEl.value;
      trigger.innerHTML = `
        <span class="truncate ${hasValue ? '' : 'text-muted'}">${escapeHtmlPS(currentLabel())}</span>
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="ps-chevron shrink-0 text-muted transition-transform duration-150"><path d="m6 9 6 6 6-6"/></svg>
      `;
    }

    function renderList() {
      list.innerHTML = Array.from(selectEl.options).map((opt, i) => `
        <button type="button" data-idx="${i}" role="option" class="ps-opt block w-full rounded-xl px-3.5 py-2.5 text-right text-sm transition
          ${i === selectEl.selectedIndex ? 'bg-emerald-soft font-semibold text-emerald' : 'text-black hover:bg-surface-muted'}">${escapeHtmlPS(opt.textContent)}</button>
      `).join('');
      list.querySelectorAll('.ps-opt').forEach(btn => {
        btn.addEventListener('click', () => {
          const idx = parseInt(btn.dataset.idx, 10);
          if (idx === selectEl.selectedIndex) { close(); return; }
          selectEl.selectedIndex = idx;
          selectEl.dispatchEvent(new Event('change', { bubbles: true }));
          renderTrigger();
          close();
        });
      });
    }

    function open() {
      if (selectEl.disabled) return;
      renderList();
      list.classList.remove('hidden');
      const chevron = trigger.querySelector('.ps-chevron');
      if (chevron) chevron.style.transform = 'rotate(180deg)';
      document.addEventListener('mousedown', outsideClick, true);
    }
    function close() {
      list.classList.add('hidden');
      const chevron = trigger.querySelector('.ps-chevron');
      if (chevron) chevron.style.transform = '';
      document.removeEventListener('mousedown', outsideClick, true);
    }
    function outsideClick(e) {
      if (!wrap.contains(e.target)) close();
    }

    trigger.addEventListener('click', () => {
      if (list.classList.contains('hidden')) open(); else close();
    });

    renderTrigger();

    const api = { refresh: () => { renderTrigger(); if (!list.classList.contains('hidden')) renderList(); } };
    // اگه جای دیگه‌ای از کد، مستقیم .value رو ست کنه یا innerHTML رو عوض کنه (مثلاً پر کردن
    // دوباره‌ی گزینه‌های شهر بعد از انتخاب استان)، باید بعدش refresh() صدا زده بشه
    selectEl._prettySelect = api;
    return api;
  }

  return { attach };
})();
