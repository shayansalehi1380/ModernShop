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
    const res = await fetch(url);
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

async function loadLayout() {
  await Promise.all([
    loadPartial('partials/header.html', '#header-placeholder'),
    loadPartial('partials/footer.html', '#footer-placeholder'),
  ]);

  highlightActiveNav();

  // این دو تابع تو api.js تعریف شدن و به المنت‌های داخل هدر (data-auth-link / data-cart-badge) نیاز دارن،
  // پس باید بعد از تزریق هدر صدا زده بشن نه قبلش
  if (typeof updateAuthUI === 'function') updateAuthUI();
  if (typeof updateCartBadge === 'function') updateCartBadge();
}

document.addEventListener('DOMContentLoaded', loadLayout);
