/* ==============================================================
   Atelier — کلاینت مشترک برای صحبت با Atelier.Api
   این فایل تو همه صفحات include می‌شه (index.html و بعداً بقیه صفحات).
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

  // احراز هویت
  sendOtp: (phoneNumber) => apiFetch('/auth/send-otp', { method: 'POST', body: JSON.stringify({ phoneNumber }) }),
  verifyOtp: (phoneNumber, code) => apiFetch('/auth/verify-otp', { method: 'POST', body: JSON.stringify({ phoneNumber, code }) }),

  // سبد خرید (هم کاربر مهمان هم لاگین‌کرده)
  getCart: () => apiFetch('/cart'),
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
function formatPrice(n) {
  return Number(n).toLocaleString('en-US');
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
      el.setAttribute('href', 'account.html');
    } else {
      const text = el.dataset.authLoggedoutText || (label ? label.textContent : el.textContent);
      if (label) label.textContent = text; else el.textContent = text;
      el.setAttribute('href', 'auth.html');
    }
  });

  // دکمه حساب کاربری تو هدر دسکتاپ (partials/header.html) - دراپ‌دان داره، پس جدا از data-auth-link مدیریت می‌شه
  const menuLabel = document.getElementById('account-menu-label');
  if (menuLabel) menuLabel.textContent = loggedIn ? 'حساب کاربری' : 'ورود | ثبت‌نام';

  // وقتی کاربر لاگین نکرده، دکمه فقط به auth.html می‌ره (دراپ‌دانی نداره)، پس فلش کشویی نمایش داده نشه
  const menuArrow = document.getElementById('account-menu-arrow');
  if (menuArrow) menuArrow.classList.toggle('hidden', !loggedIn);
}

async function updateCartBadge() {
  try {
    const cart = await Api.getCart();
    const count = (cart?.items || []).reduce((sum, i) => sum + i.quantity, 0);
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
