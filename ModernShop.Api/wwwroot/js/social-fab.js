/* ==============================================================
   Atelier — دکمه شناور تماس سریع (اینستاگرام/واتساپ/تماس)
   فقط تو index.html، shop.html و product.html صدا زده می‌شه (نه بقیه صفحات).
   ============================================================== */
(function () {
  const INSTAGRAM_URL = 'https://instagram.com/dorinmarket.ir';
  const WHATSAPP_URL = 'https://wa.me/989015538133';
  const CALL_URL = 'tel:09015538133';

  const ICON_MESSAGE = '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z"/></svg>';
  const ICON_CLOSE = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"><path d="M18 6 6 18M6 6l12 12"/></svg>';

  const ICON_INSTAGRAM = `
    <svg width="24" height="24" viewBox="0 0 24 24">
      <defs>
        <linearGradient id="fabIgGradient" x1="0%" y1="100%" x2="100%" y2="0%">
          <stop offset="0%" stop-color="#f9ce34"/>
          <stop offset="30%" stop-color="#ee2a7b"/>
          <stop offset="65%" stop-color="#6228d7"/>
          <stop offset="100%" stop-color="#6228d7"/>
        </linearGradient>
      </defs>
      <path fill="url(#fabIgGradient)" d="M12 2c2.7 0 3.05.01 4.12.06 1.06.05 1.79.22 2.43.47.66.26 1.21.6 1.76 1.15.5.5.9 1.1 1.15 1.76.25.64.42 1.37.47 2.43.05 1.07.06 1.42.06 4.13s-.01 3.06-.06 4.13c-.05 1.06-.22 1.79-.47 2.43a4.9 4.9 0 0 1-1.15 1.76 4.9 4.9 0 0 1-1.76 1.15c-.64.25-1.37.42-2.43.47-1.07.05-1.42.06-4.12.06s-3.06-.01-4.13-.06c-1.06-.05-1.79-.22-2.43-.47a4.9 4.9 0 0 1-1.76-1.15 4.9 4.9 0 0 1-1.15-1.76c-.25-.64-.42-1.37-.47-2.43C2.01 15.06 2 14.7 2 12s.01-3.06.06-4.13c.05-1.06.22-1.79.47-2.43.26-.66.6-1.21 1.15-1.76A4.9 4.9 0 0 1 5.44 2.53c.64-.25 1.37-.42 2.43-.47C8.94 2.01 9.3 2 12 2zm0 1.8c-2.66 0-2.97.01-4.02.06-.87.04-1.34.18-1.66.31-.42.16-.72.36-1.03.67-.32.32-.51.61-.68 1.03-.12.32-.27.8-.31 1.66C4.25 8.53 4.24 8.84 4.24 12s.01 3.47.06 4.47c.04.86.19 1.34.31 1.66.16.42.36.71.68 1.03.31.31.61.51 1.03.67.32.13.79.27 1.66.31 1.05.05 1.36.06 4.02.06s2.97-.01 4.02-.06c.87-.04 1.34-.18 1.66-.31.42-.16.72-.36 1.03-.67.32-.32.51-.61.68-1.03.12-.32.27-.8.31-1.66.05-1 .06-1.31.06-4.47s-.01-3.47-.06-4.47c-.04-.86-.19-1.34-.31-1.66a2.76 2.76 0 0 0-.68-1.03 2.76 2.76 0 0 0-1.03-.67c-.32-.13-.79-.27-1.66-.31-1.05-.05-1.36-.06-4.02-.06zM12 6.87A5.13 5.13 0 1 1 6.87 12 5.13 5.13 0 0 1 12 6.87zm0 8.46A3.33 3.33 0 1 0 8.67 12 3.33 3.33 0 0 0 12 15.33zm5.34-8.66a1.2 1.2 0 1 1-1.2-1.2 1.2 1.2 0 0 1 1.2 1.2z"/>
    </svg>`;

  const ICON_WHATSAPP = `
    <svg width="24" height="24" viewBox="0 0 24 24" fill="#25D366">
      <path d="M17.47 14.38c-.28-.14-1.67-.82-1.93-.92-.26-.1-.45-.14-.64.14-.19.28-.74.92-.9 1.1-.17.19-.33.21-.61.07-.28-.14-1.19-.44-2.27-1.4a8.5 8.5 0 0 1-1.57-1.95c-.16-.28-.02-.44.12-.58.13-.13.28-.33.42-.5.14-.16.19-.28.28-.47.1-.19.05-.35-.02-.5-.07-.14-.64-1.54-.87-2.11-.23-.55-.47-.48-.64-.49h-.55c-.19 0-.5.07-.76.35-.26.28-1 1-1 2.42 0 1.43 1.02 2.82 1.17 3.01.14.19 2 3.06 4.86 4.29.68.29 1.21.47 1.62.6.68.22 1.3.19 1.79.11.55-.08 1.67-.68 1.9-1.34.24-.66.24-1.22.17-1.34-.07-.12-.26-.19-.54-.33z"/>
      <path d="M12.02 2C6.5 2 2 6.48 2 11.98c0 1.88.52 3.65 1.42 5.16L2 22l5.02-1.38a10 10 0 0 0 5 1.35h.01c5.5 0 10-4.48 10-9.98A9.94 9.94 0 0 0 12.02 2zm5.9 15.87a8.3 8.3 0 0 1-5.9 2.44 8.3 8.3 0 0 1-4.24-1.15l-.3-.18-3 .82.8-2.92-.2-.31a8.24 8.24 0 0 1-1.28-4.4A8.29 8.29 0 0 1 12.02 3.7a8.3 8.3 0 0 1 5.9 14.17z"/>
    </svg>`;

  const ICON_CALL = `
    <svg width="24" height="24" viewBox="0 0 24 24" fill="#2F80ED">
      <path d="M6.62 10.79a15.05 15.05 0 0 0 6.59 6.59l2.2-2.2a1 1 0 0 1 1.02-.24 11.36 11.36 0 0 0 3.57.57 1 1 0 0 1 1 1V20a1 1 0 0 1-1 1A17 17 0 0 1 3 4a1 1 0 0 1 1-1h3.5a1 1 0 0 1 1 1 11.36 11.36 0 0 0 .57 3.57 1 1 0 0 1-.25 1.02l-2.2 2.2z"/>
    </svg>`;

  function buildHTML() {
    return `
    <style>
      #social-fab-root{position:fixed;bottom:5rem;left:1rem;z-index:93;transform:translateY(120px);opacity:0;transition:transform .6s cubic-bezier(.22,1,.36,1), opacity .6s ease;}
      #social-fab-root.fab-entered{transform:translateY(0);opacity:1;}
      #social-fab-root.fab-tall-clearance{bottom:9.5rem;}
      @media (min-width:1024px){#social-fab-root, #social-fab-root.fab-tall-clearance{bottom:1.5rem;left:1.5rem;}}
      #social-fab-stack{position:relative;width:56px;height:56px;}
      .fab-item{position:absolute;bottom:0;left:4px;width:48px;height:48px;display:flex;align-items:center;justify-content:center;border-radius:9999px;background:#fff;border:2px solid rgba(31,111,92,.32);box-shadow:0 4px 14px rgba(0,0,0,.16);opacity:0;transform:translateY(0) scale(.5);pointer-events:none;transition:transform .35s cubic-bezier(.34,1.56,.64,1), opacity .25s ease;}
      #social-fab-stack.open .fab-item{opacity:1;pointer-events:auto;transform:translateY(var(--fab-y)) scale(1);}
      #social-fab-main{position:relative;z-index:1;width:56px;height:56px;border-radius:9999px;background:#1F6F5C;display:flex;align-items:center;justify-content:center;color:#fff;border:none;cursor:pointer;box-shadow:0 6px 20px rgba(0,0,0,.22);}
      #social-fab-main .fab-icon{display:flex;align-items:center;justify-content:center;transition:transform .25s ease;}
      #social-fab-main.pulsing::before{content:"";position:absolute;inset:0;border-radius:9999px;background:#1F6F5C;animation:fabPulse 2.2s ease-out infinite;}
      @keyframes fabPulse{0%{transform:scale(1);opacity:.55}70%{transform:scale(1.9);opacity:0}100%{transform:scale(1.9);opacity:0}}
    </style>
    <div id="social-fab-root">
      <div id="social-fab-stack">
        <a class="fab-item" style="--fab-y:-64px" href="${INSTAGRAM_URL}" target="_blank" rel="noopener" aria-label="اینستاگرام">${ICON_INSTAGRAM}</a>
        <a class="fab-item" style="--fab-y:-128px" href="${WHATSAPP_URL}" target="_blank" rel="noopener" aria-label="واتساپ">${ICON_WHATSAPP}</a>
        <a class="fab-item" style="--fab-y:-192px" href="${CALL_URL}" aria-label="تماس">${ICON_CALL}</a>
        <button id="social-fab-main" class="pulsing" type="button" aria-label="راه‌های ارتباطی">
          <span id="social-fab-icon" class="fab-icon">${ICON_MESSAGE}</span>
        </button>
      </div>
    </div>`;
  }

  function init() {
    document.body.insertAdjacentHTML('beforeend', buildHTML());

    const root = document.getElementById('social-fab-root');
    const stack = document.getElementById('social-fab-stack');
    const mainBtn = document.getElementById('social-fab-main');
    const iconEl = document.getElementById('social-fab-icon');
    let open = false;

    // product.html یه نوار چسبان قیمت/افزودن-به-سبد اضافه رو موبایل داره که باید ازش رد بشیم
    const currentPage = window.location.pathname.split('/').pop() || 'index.html';
    if (currentPage === 'product.html') root.classList.add('fab-tall-clearance');

    function setOpen(next) {
      open = next;
      stack.classList.toggle('open', open);
      mainBtn.classList.toggle('pulsing', !open);
      iconEl.innerHTML = open ? ICON_CLOSE : ICON_MESSAGE;
    }

    mainBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      setOpen(!open);
    });

    document.addEventListener('click', (e) => {
      if (open && !stack.contains(e.target)) setOpen(false);
    });

    // ورود آروم از پایین، بعد از لود کامل صفحه
    requestAnimationFrame(() => {
      setTimeout(() => root.classList.add('fab-entered'), 400);
    });
  }

  if (document.readyState === 'complete') {
    init();
  } else {
    window.addEventListener('load', init);
  }
})();
