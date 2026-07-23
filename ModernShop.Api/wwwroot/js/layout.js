/* ==============================================================
   Atelier — بارگذاری هدر و فوتر مشترک از partials/
   این فایل باید بعد از js/api.js و قبل از اسکریپت اختصاصی هر صفحه لود بشه.
   هر صفحه فقط باید دو تا <div> جای‌گیرنده داشته باشه:
     <div id="header-placeholder"></div>
     <div id="footer-placeholder"></div>
   ============================================================== */

async function loadPartial(url, mountSelector) {
  const mount = document.querySelector(mountSelector);
  if (!mount) return;

  try {
    // no-store تا مرورگر (خصوصاً بعضی مرورگرهای موبایل) یک نسخه‌ی قدیمی/کش‌شده‌ی این partial رو
    // به‌جای نسخه‌ی واقعی سرو نکنه - این فایل روی هر صفحه‌ای جدا fetch می‌شه، پس باید همیشه تازه باشه
    const res = await fetch(url, { cache: 'no-store' });
    if (!res.ok) throw new Error(`دریافت ${url} با خطا مواجه شد`);
    mount.outerHTML = await res.text();
  } catch (e) {
    console.error('خطا در بارگذاری partial:', e);
  }
}

/* لینک ناوبری مطابق صفحه فعلی رنگی می‌شه (مقایسه بر اساس اسم فایل، بدون در نظر گرفتن query string) */
function highlightActiveNav() {
  const currentPath = window.location.pathname.split('/').pop() || 'index.html';

  document.querySelectorAll('[data-nav-link]').forEach(link => {
    const linkPath = link.getAttribute('href').split('?')[0];
    const isActive = linkPath === currentPath;
    link.classList.toggle('text-emerald', isActive);
    link.classList.toggle('text-foreground/75', !isActive);
  });
}

/* تب فعال نوار پایین موبایل (partials/mobile-nav.html)، بر اساس اسم فایل صفحه فعلی */
const MNAV_PAGE_KEY_MAP = { 'product.html': 'shop.html', 'checkout.html': 'cart.html' };
function highlightActiveMobileNav() {
  const currentPath = window.location.pathname.split('/').pop() || 'index.html';
  const activeKey = MNAV_PAGE_KEY_MAP[currentPath] || currentPath;

  document.querySelectorAll('[data-mnav-key]').forEach(link => {
    const isActive = link.dataset.mnavKey === activeKey;
    link.classList.toggle('text-emerald', isActive);
    link.classList.toggle('text-muted', !isActive);
  });
}

async function loadLayout() {
  await Promise.all([
    loadPartial('partials/header.html', '#header-placeholder'),
    loadPartial('partials/footer.html', '#footer-placeholder'),
    loadPartial('partials/mobile-nav.html', '#mobile-nav-placeholder'),
  ]);

  highlightActiveNav();
  highlightActiveMobileNav();

  // این دو تابع تو api.js تعریف شدن و به المنت‌های داخل هدر (data-auth-link / data-cart-badge) نیاز دارن،
  // پس باید بعد از تزریق هدر صدا زده بشن نه قبلش
  if (typeof updateAuthUI === 'function') updateAuthUI();
  if (typeof updateCartBadge === 'function') updateCartBadge();

  // جستجوی زنده تو هدر دسکتاپ - بعد از تزریق هدر، چون قبلش این المان‌ها وجود ندارن
  if (typeof AtelierSearch === 'object') {
    AtelierSearch.attach(
      document.getElementById('header-search-input'),
      document.getElementById('header-search-dropdown'),
      document.getElementById('header-search-wrap')
    );
  }
}

document.addEventListener('DOMContentLoaded', loadLayout);

/* ---------- دراپ‌دان «حساب کاربری» تو هدر دسکتاپ ---------- */
function toggleAccountMenu(e) {
  if (e) e.stopPropagation();
  if (!isLoggedIn()) {
    window.location.href = 'auth.html';
    return;
  }
  const dropdown = document.getElementById('account-menu-dropdown');
  if (dropdown) dropdown.classList.toggle('hidden');
}

function handleHeaderLogout() {
  clearToken();
  window.location.href = 'index.html';
}

document.addEventListener('click', (e) => {
  const dropdown = document.getElementById('account-menu-dropdown');
  const btn = document.getElementById('account-menu-btn');
  if (!dropdown || dropdown.classList.contains('hidden')) return;
  if (!e.target.closest('#account-menu-dropdown') && !e.target.closest('#account-menu-btn')) {
    dropdown.classList.add('hidden');
  }
});
