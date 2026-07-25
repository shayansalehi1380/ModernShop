/* ==============================================================
   تبدیل تاریخ میلادی <-> شمسی (جلالی) + کامپوننت دیت‌پیکر شمسی
   الگوریتم تبدیل، پیاده‌سازی استاندارد و شناخته‌شده‌ی تقویم جلالی است
   (بر پایه‌ی محاسبات منتشرشده‌ی تقویم جلالی، بدون وابستگی به کتابخانه‌ی خارجی).
   ============================================================== */

const Jalali = (function () {
  function div(a, b) { return ~~(a / b); }
  function mod(a, b) { return a - ~~(a / b) * b; }

  const breaks = [-61, 9, 38, 199, 426, 686, 756, 818, 1111, 1181, 1210, 1635, 2060, 2097, 2192, 2262, 2324, 2394, 2456, 3178];

  function jalCal(jy) {
    const bl = breaks.length;
    const gy = jy + 621;
    let leapJ = -14, jp = breaks[0];
    if (jy < jp || jy >= breaks[bl - 1]) throw new Error('سال جلالی نامعتبر: ' + jy);
    let jump = 0;
    for (let i = 1; i < bl; i += 1) {
      const jm = breaks[i];
      jump = jm - jp;
      if (jy < jm) break;
      leapJ = leapJ + div(jump, 33) * 8 + div(mod(jump, 33), 4);
      jp = jm;
    }
    let n = jy - jp;
    leapJ = leapJ + div(n, 33) * 8 + div(mod(n, 33) + 3, 4);
    if (mod(jump, 33) === 4 && jump - n === 4) leapJ += 1;
    const leapG = div(gy, 4) - div((div(gy, 100) + 1) * 3, 4) - 150;
    const march = 20 + leapJ - leapG;
    if (jump - n < 6) n = n - jump + div(jump, 33) * 33;
    let leap = mod(mod(n + 1, 33) - 1, 4);
    if (leap === -1) leap = 4;
    return { leap, gy, march };
  }

  function g2d(gy, gm, gd) {
    let d = div((gy + div(gm - 8, 6) + 100100) * 1461, 4)
      + div(153 * mod(gm + 9, 12) + 2, 5)
      + gd - 34840408;
    d = d - div(div(gy + 100100 + div(gm - 8, 6), 100) * 3, 4) + 752;
    return d;
  }

  function d2g(jdn) {
    let j = 4 * jdn + 139361631;
    j = j + div(div(4 * jdn + 183187720, 146097) * 3, 4) * 4 - 3908;
    const i = div(mod(j, 1461), 4) * 5 + 308;
    const gd = div(mod(i, 153), 5) + 1;
    const gm = mod(div(i, 153), 12) + 1;
    const gy = div(j, 1461) - 100100 + div(8 - gm, 6);
    return { gy, gm, gd };
  }

  function j2d(jy, jm, jd) {
    const r = jalCal(jy);
    return g2d(r.gy, 3, r.march) + (jm - 1) * 31 - div(jm, 7) * (jm - 7) + jd - 1;
  }

  function d2j(jdn) {
    const gy = d2g(jdn).gy;
    let jy = gy - 621;
    const r = jalCal(jy);
    const jdn1f = g2d(gy, 3, r.march);
    let jd, jm, k;
    k = jdn - jdn1f;
    if (k >= 0) {
      if (k <= 185) {
        jm = 1 + div(k, 31);
        jd = mod(k, 31) + 1;
        return { jy, jm, jd };
      }
      k -= 186;
    } else {
      jy -= 1;
      k += 179;
      if (r.leap === 1) k += 1;
    }
    jm = 7 + div(k, 30);
    jd = mod(k, 30) + 1;
    return { jy, jm, jd };
  }

  function toJalali(gy, gm, gd) { return d2j(g2d(gy, gm, gd)); }
  function toGregorian(jy, jm, jd) { return d2g(j2d(jy, jm, jd)); }
  function isLeapJalaliYear(jy) { return jalCal(jy).leap === 0; }
  function monthLength(jy, jm) {
    if (jm <= 6) return 31;
    if (jm <= 11) return 30;
    return isLeapJalaliYear(jy) ? 30 : 29;
  }

  const MONTH_NAMES = ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'];
  const WEEKDAY_SHORT = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج']; // شنبه تا جمعه

  function faDigits(n) {
    const d = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];
    return String(n).replace(/[0-9]/g, x => d[x]);
  }
  function pad2(n) { return n < 10 ? '0' + n : '' + n; }

  // dow جلالی: 0=شنبه ... 6=جمعه؛ روز هفته‌ی جاوااسکریپت (getDay) 0=یکشنبه است
  function jalaliWeekday(gy, gm, gd) {
    const jsDay = new Date(Date.UTC(gy, gm - 1, gd)).getUTCDay(); // 0=یکشنبه..6=شنبه
    return (jsDay + 1) % 7; // 0=شنبه..6=جمعه
  }

  return { toJalali, toGregorian, isLeapJalaliYear, monthLength, MONTH_NAMES, WEEKDAY_SHORT, faDigits, pad2, jalaliWeekday };
})();

/* ---------------- کامپوننت دیت‌پیکر شمسی (پاپ‌آور) ---------------- */
const PersianDatePicker = (function () {
  function attach(displayInput, hiddenIsoInput, opts) {
    opts = opts || {};
    let jy, jm, jdSelected = null;
    const today = new Date();
    const todayJ = Jalali.toJalali(today.getFullYear(), today.getMonth() + 1, today.getDate());
    jy = todayJ.jy; jm = todayJ.jm;

    const wrap = document.createElement('div');
    wrap.className = 'relative';
    displayInput.parentNode.insertBefore(wrap, displayInput);
    wrap.appendChild(displayInput);

    const popover = document.createElement('div');
    popover.className = 'pdp-popover hidden absolute z-30 mt-2 w-72 max-w-[90vw] rounded-2xl border border-line bg-surface p-3 shadow-lg';
    popover.style.top = '100%';
    popover.style.right = '0';
    wrap.appendChild(popover);

    function setFromIso(isoStr) {
      if (!isoStr) { jdSelected = null; return; }
      const d = new Date(isoStr);
      if (isNaN(d.getTime())) { jdSelected = null; return; }
      const j = Jalali.toJalali(d.getUTCFullYear(), d.getUTCMonth() + 1, d.getUTCDate());
      jy = j.jy; jm = j.jm; jdSelected = j.jd;
      renderDisplay();
    }

    function renderDisplay() {
      if (jdSelected) {
        displayInput.value = `${Jalali.faDigits(jy)}/${Jalali.faDigits(Jalali.pad2(jm))}/${Jalali.faDigits(Jalali.pad2(jdSelected))}`;
      } else {
        displayInput.value = '';
      }
    }

    function commitSelection(d) {
      jdSelected = d;
      const g = Jalali.toGregorian(jy, jm, d);
      const iso = `${g.gy.toString().padStart(4, '0')}-${Jalali.pad2(g.gm)}-${Jalali.pad2(g.gd)}`;
      hiddenIsoInput.value = iso;
      renderDisplay();
      closePopover();
      if (typeof opts.onChange === 'function') opts.onChange(iso);
    }

    function yearOptions() {
      const nowJy = todayJ.jy;
      const start = nowJy - 100;
      const end = nowJy;
      let html = '';
      for (let y = end; y >= start; y--) html += `<option value="${y}" ${y === jy ? 'selected' : ''}>${Jalali.faDigits(y)}</option>`;
      return html;
    }

    function renderCalendar() {
      const len = Jalali.monthLength(jy, jm);
      const g1 = Jalali.toGregorian(jy, jm, 1);
      const startWeekday = Jalali.jalaliWeekday(g1.gy, g1.gm, g1.gd);
      let cells = '';
      for (let i = 0; i < startWeekday; i++) cells += `<span></span>`;
      for (let d = 1; d <= len; d++) {
        const isToday = jy === todayJ.jy && jm === todayJ.jm && d === todayJ.jd;
        const isSelected = jdSelected === d;
        cells += `<button type="button" data-day="${d}" class="pdp-day flex h-8 w-8 items-center justify-center rounded-full text-xs transition
          ${isSelected ? 'bg-emerald text-white font-bold' : isToday ? 'border border-emerald text-emerald font-semibold' : 'hover:bg-surface-muted'}">${Jalali.faDigits(d)}</button>`;
      }

      popover.innerHTML = `
        <div class="flex items-center justify-between gap-2">
          <button type="button" class="pdp-next flex h-8 w-8 items-center justify-center rounded-full hover:bg-surface-muted" aria-label="ماه بعد">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="m9 6 6 6-6 6"/></svg>
          </button>
          <div class="flex items-center gap-1.5">
            <select class="pdp-month rounded-lg border border-line bg-surface-muted px-2 py-1 text-xs outline-none">
              ${Jalali.MONTH_NAMES.map((mn, i) => `<option value="${i + 1}" ${i + 1 === jm ? 'selected' : ''}>${mn}</option>`).join('')}
            </select>
            <select class="pdp-year rounded-lg border border-line bg-surface-muted px-2 py-1 text-xs outline-none">${yearOptions()}</select>
          </div>
          <button type="button" class="pdp-prev flex h-8 w-8 items-center justify-center rounded-full hover:bg-surface-muted" aria-label="ماه قبل">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="m15 18-6-6 6-6"/></svg>
          </button>
        </div>
        <div class="mt-3 grid grid-cols-7 gap-1 text-center text-[11px] text-muted">
          ${Jalali.WEEKDAY_SHORT.map(w => `<span class="flex h-6 items-center justify-center">${w}</span>`).join('')}
        </div>
        <div class="mt-1 grid grid-cols-7 place-items-center gap-1">${cells}</div>
      `;

      popover.querySelector('.pdp-next').addEventListener('click', () => { jm++; if (jm > 12) { jm = 1; jy++; } renderCalendar(); });
      popover.querySelector('.pdp-prev').addEventListener('click', () => { jm--; if (jm < 1) { jm = 12; jy--; } renderCalendar(); });
      popover.querySelector('.pdp-month').addEventListener('change', (e) => { jm = parseInt(e.target.value, 10); renderCalendar(); });
      popover.querySelector('.pdp-year').addEventListener('change', (e) => { jy = parseInt(e.target.value, 10); renderCalendar(); });
      popover.querySelectorAll('.pdp-day').forEach(btn => {
        btn.addEventListener('click', () => commitSelection(parseInt(btn.dataset.day, 10)));
      });
    }

    function openPopover() {
      if (jdSelected == null) { jy = todayJ.jy; jm = todayJ.jm; }
      renderCalendar();
      popover.classList.remove('hidden');
      document.addEventListener('mousedown', outsideClick, true);
    }
    function closePopover() {
      popover.classList.add('hidden');
      document.removeEventListener('mousedown', outsideClick, true);
    }
    function outsideClick(e) {
      if (!wrap.contains(e.target)) closePopover();
    }

    displayInput.readOnly = true;
    displayInput.addEventListener('click', () => {
      if (popover.classList.contains('hidden')) openPopover(); else closePopover();
    });

    return { setFromIso };
  }

  return { attach };
})();
