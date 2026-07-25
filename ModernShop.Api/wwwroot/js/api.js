/* ==============================================================
   Atelier — کلاینت مشترک برای صحبت با Atelier.Api
   این فایل تو همه صفحات include می‌شه (home و بعداً بقیه صفحات).
   ============================================================== */

// چون فرانت از همون wwwroot خود Api سرو می‌شه، مسیر نسبی کافیه و به CORS نیازی نیست.
const API_BASE = '/api';

/* ---------------- توکن و شناسه کاربر مهمان ---------------- */
function getToken() { return localStorage.getItem('atelier_token'); }
function setToken(token) { localStorage.setItem('atelier_token', token); }
function clearToken() { localStorage.removeItem('atelier_token'); }
function isLoggedIn() { return !!getToken(); }

function getGuestSessionId() {
  let id = localStorage.getItem('atelier_guest_session');
  if (!id) {
    id = (crypto.randomUUID ? crypto.randomUUID() : ('guest-' + Date.now() + '-' + Math.random().toString(16).slice(2)));
    localStorage.setItem('atelier_guest_session', id);
  }
  return id;
}

/* ---------------- fetch wrapper ---------------- */
async function apiFetch(path, options = {}) {
  const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };

  const token = getToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  } else {
    // برای اندپوینت‌های سبد خرید که کاربر مهمان هم بهشون دسترسی داره
    headers['X-Guest-Session-Id'] = getGuestSessionId();
  }

  const res = await fetch(`${API_BASE}${path}`, { ...options, headers });

  if (!res.ok) {
    let message = 'خطایی رخ داد، لطفاً دوباره تلاش کنید';
    try {
      const data = await res.json();
      if (data?.message) message = data.message;
    } catch (e) { /* بدنه پاسخ JSON نبود */ }
    const error = new Error(message);
    error.status = res.status;
    throw error;
  }

  if (res.status === 204) return null;
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

/* ---------------- سینک سبد خرید بین تب‌ها و بخش‌های مختلف همون صفحه ----------------
   وقتی سبد خرید از هرجایی (کارت محصول، پاپ‌آپ انتخاب تنوع، صفحه محصول، خود صفحه سبد
   خرید) تغییر می‌کنه، این تابع صدا زده می‌شه؛ یک رویداد سراسری atelier:cart-changed
   پخش می‌کنه که هر بخشی از همون صفحه (نشان تعداد سبد، کارت‌های محصول، ...) می‌تونه
   بهش گوش بده و خودش رو با وضعیت واقعی سبد به‌روز کنه. علاوه بر این، چون تو localStorage
   هم یک مقدار می‌نویسه، مرورگر به‌صورت خودکار رویداد استاندارد "storage" رو تو بقیه‌ی
   تب‌های باز همین سایت (نه تب فعلی) triggers می‌کنه - پس اگه کاربر همزمان سبد خرید رو
   تو یک تب دیگه باز کرده باشه، اونم به‌محض برگشتن به اون تب (یا حتی در پس‌زمینه) خودش
   رو با آخرین وضعیت هماهنگ می‌کنه. */
function notifyCartChanged() {
  try { localStorage.setItem('atelier_cart_sync', String(Date.now())); } catch (e) { /* بی‌خیال (مثلاً حالت خصوصی مرورگر) */ }
  window.dispatchEvent(new CustomEvent('atelier:cart-changed'));
}

window.addEventListener('storage', (e) => {
  if (e.key === 'atelier_cart_sync') {
    window.dispatchEvent(new CustomEvent('atelier:cart-changed'));
  }
});

/* ---------------- همه‌ی endpoint هایی که تو صفحات استفاده می‌شن ---------------- */
const Api = {
  // کاتالوگ
  getBanners: () => apiFetch('/banners'),
  getCategories: () => apiFetch('/categories'),
  getBrands: () => apiFetch('/brands'),
  getFeaturedBrands: () => apiFetch('/brands?featuredOnly=true'),
  getProducts: (queryString = '') => apiFetch(`/products${queryString}`),
  searchSuggestions: (q) => apiFetch(`/products/search-suggestions?q=${encodeURIComponent(q)}`),
  getProduct: (slug) => apiFetch(`/products/${encodeURIComponent(slug)}`),
  addReview: (productId, rating, comment) =>
    apiFetch('/products/reviews', { method: 'POST', body: JSON.stringify({ productId, rating, comment }) }),
  getStaticPage: (slug) => apiFetch(`/static-pages/${encodeURIComponent(slug)}`),

  // احراز هویت
  sendOtp: (phoneNumber) => apiFetch('/auth/send-otp', { method: 'POST', body: JSON.stringify({ phoneNumber }) }),
  verifyOtp: (phoneNumber, code) => apiFetch('/auth/verify-otp', { method: 'POST', body: JSON.stringify({ phoneNumber, code }) }),

  // سبد خرید (هم کاربر مهمان هم لاگین‌کرده)
  getCart: () => apiFetch('/cart'),
  // فقط تعداد آیتم‌های سبد (برای نشان روی آیکون سبد خرید تو هدر/منوی موبایل) - خیلی سبک‌تر از
  // گرفتن کل سبد با جزئیات محصولات، چون این یکی تقریباً تو هر صفحه‌ای صدا زده می‌شه
  getCartCount: () => apiFetch('/cart/count'),
  addToCart: async (productId, quantity = 1, productVariantId = null) => {
    const cart = await apiFetch('/cart/items', { method: 'POST', body: JSON.stringify({ productId, quantity, productVariantId }) });
    notifyCartChanged();
    return cart;
  },
  updateCartItem: async (cartItemId, quantity) => {
    const cart = await apiFetch('/cart/items', { method: 'PUT', body: JSON.stringify({ cartItemId, quantity }) });
    notifyCartChanged();
    return cart;
  },
  removeCartItem: async (cartItemId) => {
    const cart = await apiFetch(`/cart/items/${cartItemId}`, { method: 'DELETE' });
    notifyCartChanged();
    return cart;
  },
  applyDiscount: (code) => apiFetch('/cart/apply-discount', { method: 'POST', body: JSON.stringify({ code }) }),

  // سفارش (نیاز به لاگین)
  createOrder: (addressId, paymentMethod, discountCode) =>
    apiFetch('/orders', { method: 'POST', body: JSON.stringify({ addressId, paymentMethod, discountCode }) }),
  getMyOrders: () => apiFetch('/orders'),
  getOrder: (orderNumber) => apiFetch(`/orders/${encodeURIComponent(orderNumber)}`),
  retryPayment: (orderNumber) => apiFetch(`/orders/${encodeURIComponent(orderNumber)}/retry-payment`, { method: 'POST' }),

  // حساب کاربری (نیاز به لاگین)
  getProfile: () => apiFetch('/account/profile'),
  updateProfile: (data) => apiFetch('/account/profile', { method: 'PUT', body: JSON.stringify(data) }),
  getAddresses: () => apiFetch('/account/addresses'),
  upsertAddress: (data) => apiFetch('/account/addresses', { method: 'POST', body: JSON.stringify(data) }),
  deleteAddress: (id) => apiFetch(`/account/addresses/${id}`, { method: 'DELETE' }),
  getWishlist: () => apiFetch('/account/wishlist'),
  addToWishlist: (productId) => apiFetch(`/account/wishlist/${productId}`, { method: 'POST' }),
  removeFromWishlist: (productId) => apiFetch(`/account/wishlist/${productId}`, { method: 'DELETE' }),

  // وبلاگ
  getBlogCategories: () => apiFetch('/blog/categories'),
  getBlogPosts: (queryString = '') => apiFetch(`/blog/posts${queryString}`),
  getBlogPost: (slug) => apiFetch(`/blog/posts/${encodeURIComponent(slug)}`),
  addComment: (blogPostId, content) =>
    apiFetch('/blog/comments', { method: 'POST', body: JSON.stringify({ blogPostId, content }) }),

  // خبرنامه
  subscribeNewsletter: (email) => apiFetch('/newsletter/subscribe', { method: 'POST', body: JSON.stringify({ email }) }),
};

/* ---------------- کمکی‌های مشترک UI (چون همه صفحات به این‌ها نیاز دارن) ---------------- */
// همه‌ی اعداد سایت (قیمت‌ها، تعدادها، امتیازها...) با همین دو تابع به رقم فارسی تبدیل می‌شن
// تا سبک نمایش عدد رو کل سایت یکسان باشه (دقیقاً همون سبکی که تو فوتر و کارت محصولات هست)
function toFaDigits(str) {
  const d = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];
  return String(str).replace(/[0-9]/g, x => d[x]);
}
function formatPrice(n) {
  return toFaDigits(Number(n || 0).toLocaleString('en-US'));
}

// اگه آدرس عکس از آپلودهای خودمون باشه (uploads/products)، یه نسخه‌ی کوچیک‌شده و کش‌شده‌ی
// همون عکس رو درخواست می‌کنه (خیلی سبک‌تر از فایل اصلی که ممکنه چندمگابایتی باشه و کارت‌های
// محصول رو کل سایت کند بارگذاری کنه). برای آدرس‌های خارجی (لینک مستقیم از یه سایت دیگه)
// دست‌نخورده برمی‌گرده، چون این قابلیت فقط سمت سرور خودمون کار می‌کنه.
// دامنه‌های خارجی که تأیید شده از داخل ایران در دسترس نیستن (مثلاً Unsplash - هر درخواست
// بهش با net::ERR_CONNECTION_REFUSED بعد از چند ثانیه معطلی شکست می‌خوره). بعضی محصولات/
// پست‌های وبلاگ قدیمی تو دیتابیس عکسشون یه لینک از همین دامنه‌هاست؛ چون این معطلی روی هر
// تصویر می‌تونه اتصال‌های همزمان مرورگر رو اشغال کنه و حتی درخواست‌های API خود سایت (که
// ربطی به عکس ندارن) رو هم کند کنه، این آدرس‌ها اصلاً نباید امتحان بشن - باید همون اول رد بشن
const BLOCKED_IMG_HOSTS = ['images.unsplash.com', 'picsum.photos'];
function isBlockedImgHost(url) {
  return !!url && BLOCKED_IMG_HOSTS.some(h => url.includes(h));
}

// اگه آدرس عکس متعلق به یکی از دامنه‌های بالا باشه، رشته‌ی خالی برمی‌گرده (یعنی مرورگر
// اصلاً درخواستی به اون سمت نمی‌فرسته) - برای هر جایی که عکس مستقیماً (بدون productThumb)
// از دیتابیس نمایش داده می‌شه، مثل عکس شاخص پست‌های وبلاگ
function safeImageUrl(url) {
  return isBlockedImgHost(url) ? PRODUCT_IMG_FALLBACK : (url || '');
}

function productThumb(url, width) {
  if (!url) return url;
  if (isBlockedImgHost(url)) return PRODUCT_IMG_FALLBACK;
  const marker = '/uploads/products/';
  const idx = url.indexOf(marker);
  if (idx === -1) return url;
  const fileName = url.slice(idx + marker.length);
  if (!fileName || fileName.includes('/')) return url; // از قبل تامبنیل بوده یا مسیر غیرمنتظره
  return `${url.slice(0, idx)}${marker}thumb/${width}/${fileName}`;
}

// SVG محلی (بدون هیچ درخواست شبکه‌ای) - جایگزین فوری وقتی عکس اصلی (مثلاً یه لینک خارجی
// قدیمی/خراب که تو ایران در دسترس نیست) لود نشه، به‌جای اینکه مرورگر چند ثانیه معطل بمونه
const PRODUCT_IMG_FALLBACK = 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA0MDAgNDAwIj48cmVjdCB3aWR0aD0iNDAwIiBoZWlnaHQ9IjQwMCIgZmlsbD0iIzE2MTgxQyIvPjxjaXJjbGUgY3g9IjE2NSIgY3k9IjE1NSIgcj0iMjgiIGZpbGw9IiMzMzM2M2MiLz48cGF0aCBkPSJNMTIwIDI5MGw1NS03MCA0MiA0NiAzNC00MCA1OCA2NEgxMjB6IiBmaWxsPSIjMzMzNjNjIi8+PC9zdmc+';

// روی هر <img> که ممکنه لینک خارجی/قدیمی داشته باشه صدا زده می‌شه: اگه لود نشد، بی‌سروصدا
// و فوری با تصویر جایگزین محلی عوض می‌شه (به‌جای آیکون شکسته یا معطلی چندثانیه‌ای مرورگر).
// این همچنان لازمه چون safeImageUrl فقط دامنه‌های شناخته‌شده رو رد می‌کنه؛ لینک‌های خارجی
// دیگه که خرابن (نه لزوماً Unsplash) هنوز باید موقع خطا جایگزین بشن
function onProductImgError(img) {
  img.onerror = null;
  img.src = PRODUCT_IMG_FALLBACK;
}

function updateAuthUI() {
  const loggedIn = isLoggedIn();
  document.querySelectorAll('[data-auth-link]').forEach(el => {
    // اگه یه span[data-auth-label] داخلش باشه (برای نگه‌داشتن آیکون کنار متن)، فقط متن همون span عوض می‌شه؛
    // وگرنه (برای عناصر ساده‌ی بدون آیکون) کل textContent عوض می‌شه
    const label = el.querySelector('[data-auth-label]');
    if (loggedIn) {
      const text = el.dataset.authLoggedinText || 'حساب کاربری';
      if (label) label.textContent = text; else el.textContent = text;
      el.setAttribute('href', 'account');
    } else {
      const text = el.dataset.authLoggedoutText || (label ? label.textContent : el.textContent);
      if (label) label.textContent = text; else el.textContent = text;
      el.setAttribute('href', 'auth');
    }
  });

  // دکمه حساب کاربری تو هدر دسکتاپ (partials/header.html) - دراپ‌دان داره، پس جدا از data-auth-link مدیریت می‌شه
  const menuLabel = document.getElementById('account-menu-label');
  if (menuLabel) menuLabel.textContent = loggedIn ? 'حساب کاربری' : 'ورود | ثبت‌نام';

  // وقتی کاربر لاگین نکرده، دکمه فقط به auth می‌ره (دراپ‌دانی نداره)، پس فلش کشویی نمایش داده نشه
  const menuArrow = document.getElementById('account-menu-arrow');
  if (menuArrow) menuArrow.classList.toggle('hidden', !loggedIn);
}

async function updateCartBadge() {
  try {
    const res = await Api.getCartCount();
    const count = res?.count || 0;
    document.querySelectorAll('[data-cart-badge]').forEach(el => {
      el.textContent = count;
      el.style.display = count > 0 ? '' : 'none';
    });
  } catch (e) {
    // اگه هنوز سبدی نساخته شده یا خطای شبکه بود، بی‌سروصدا رد شو
  }
}

// نشان تعداد سبد خرید (هدر + منوی پایین موبایل) رو تمام صفحات، خودش رو با هر تغییر سبد
// (چه تو همین صفحه چه تو یک تب دیگه) هماهنگ نگه می‌داره
window.addEventListener('atelier:cart-changed', () => updateCartBadge());
