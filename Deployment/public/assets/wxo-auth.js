/* WXO auth — one account for every game (server: FastAPI + MySQL) */
(function (global) {
  // Auto-detect API base: localhost/LAN → local FastAPI (:8000), else live server.
  // Production (rmsurveyai.com) is unaffected; only local/LAN hosts switch to :8000.
  const API_BASE = (function () {
    try {
      if (global.WXO_API_BASE) return global.WXO_API_BASE;
      const h = location.hostname;
      if (h === 'localhost' || h === '127.0.0.1' || /^192\.168\./.test(h) || /^10\./.test(h) || /^172\.(1[6-9]|2\d|3[01])\./.test(h)) {
        return location.protocol + '//' + h + ':8000/api/v1';
      }
    } catch (e) {}
    return 'https://rmsurveyai.com/api/v1';
  })();
  const TOKEN_KEY = 'wxo_token';
  const USER_KEY = 'wxo_user_v1';
  const DEVICE_KEY = 'wxo_device_id';

  function getToken() {
    try { return localStorage.getItem(TOKEN_KEY) || ''; } catch (e) { return ''; }
  }

  function getUser() {
    try { return JSON.parse(localStorage.getItem(USER_KEY) || 'null'); }
    catch (e) { return null; }
  }

  function isLoggedIn() {
    return !!getToken();
  }

  function deviceId() {
    let id = '';
    try { id = localStorage.getItem(DEVICE_KEY) || ''; } catch (e) {}
    if (!id) {
      id = 'web_' + Math.random().toString(36).slice(2) + Date.now().toString(36);
      try { localStorage.setItem(DEVICE_KEY, id); } catch (e) {}
    }
    return id;
  }

  function setSession(data) {
    if (!data || !data.access_token) return null;
    const user = {
      userId: data.user_id,
      uuid: data.user_uuid,
      name: data.display_name || 'Player',
      email: data.email || ''
    };
    try {
      localStorage.setItem(TOKEN_KEY, data.access_token);
      localStorage.setItem(USER_KEY, JSON.stringify(user));
    } catch (e) {}
    global.dispatchEvent(new CustomEvent('wxo:auth', { detail: { user: user } }));
    return user;
  }

  function clearSession() {
    try {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    } catch (e) {}
    global.dispatchEvent(new CustomEvent('wxo:auth', { detail: { user: null } }));
  }

  function logout(redirect) {
    clearSession();
    location.href = redirect || '/login.html';
  }

  /** Fetch wrapper: adds bearer token, parses FastAPI error detail */
  async function api(path, options) {
    const opts = options || {};
    const headers = Object.assign(
      { 'Content-Type': 'application/json', 'X-Device-Id': deviceId() },
      opts.headers || {}
    );
    const token = getToken();
    if (token) headers.Authorization = 'Bearer ' + token;

    let res;
    try {
      res = await fetch(API_BASE + path, {
        method: opts.method || 'GET',
        headers: headers,
        body: opts.body != null ? JSON.stringify(opts.body) : undefined
      });
    } catch (e) {
      throw new Error('Network error. Check your internet and try again.');
    }

    let data = null;
    try { data = await res.json(); } catch (e) {}

    if (!res.ok) {
      if (res.status === 401 && token) clearSession();
      throw new Error(errorText(data, res.status));
    }
    return data;
  }

  function errorText(data, status) {
    const detail = data && data.detail;
    if (typeof detail === 'string') return detail;
    if (Array.isArray(detail) && detail.length) {
      const first = detail[0];
      if (first && first.msg) {
        const field = Array.isArray(first.loc) ? first.loc[first.loc.length - 1] : '';
        return (field ? field + ': ' : '') + first.msg;
      }
    }
    if (status === 429) return 'Too many attempts. Please wait a minute.';
    if (status >= 500) return 'Server error. Please try again.';
    return 'Something went wrong (' + status + ')';
  }

  function register(email, password, displayName, referralCode) {
    const body = { email: email, password: password, display_name: displayName || 'Player' };
    const ref = referralCode || pendingReferral();
    if (ref) body.referral_code = ref;
    return api('/auth/register', {
      method: 'POST',
      body: body
    }).then(function (data) {
      setSession(Object.assign({ email: email }, data));
      return data;
    });
  }

  function login(email, password) {
    return api('/auth/login', {
      method: 'POST',
      body: { email: email, password: password }
    }).then(function (data) {
      setSession(Object.assign({ email: email }, data));
      claimPendingReferral();
      return data;
    });
  }

  function guestLogin(displayName) {
    const body = { guest_id: deviceId(), display_name: displayName || 'Guest' };
    const ref = pendingReferral();
    if (ref) body.referral_code = ref;
    return api('/auth/guest', {
      method: 'POST',
      body: body
    }).then(function (data) {
      setSession(data);
      return data;
    });
  }

  function me() {
    return api('/auth/me');
  }

  function walletBalance() {
    return api('/wallet/balance');
  }

  function walletTransactions() {
    return api('/wallet/transactions');
  }

  function pendingReferral() {
    try {
      const q = new URLSearchParams(location.search).get('ref');
      if (q) {
        localStorage.setItem('wxo_referred_by', q.trim());
        return q.trim();
      }
      return (localStorage.getItem('wxo_referred_by') || '').trim();
    } catch (e) {
      return '';
    }
  }

  function claimPendingReferral() {
    const code = pendingReferral();
    if (!code || !isLoggedIn()) return Promise.resolve(null);
    return api('/referral/claim', { method: 'POST', body: { referral_code: code } })
      .then(function (data) {
        if (data && (data.attached || data.already)) {
          try { localStorage.removeItem('wxo_referred_by'); } catch (e) {}
        }
        return data;
      })
      .catch(function () { return null; });
  }

  /** Send user to login if no session. Returns true when allowed. */
  function requireAuth() {
    if (isLoggedIn()) return true;
    const next = encodeURIComponent(location.pathname + location.search);
    location.replace('/login.html?next=' + next);
    return false;
  }

  /** Numeric User ID from login cache (instant, no network). */
  function getUserId() {
    const u = getUser();
    if (u && u.userId != null && u.userId !== '') return String(u.userId);
    return '';
  }

  function setUserIdCache(id) {
    if (id == null || id === '') return;
    const u = getUser();
    if (!u) return;
    if (String(u.userId) === String(id)) return;
    u.userId = id;
    try { localStorage.setItem(USER_KEY, JSON.stringify(u)); } catch (e) {}
  }

  /** Resolve User ID: cache first, then /auth/me. */
  function refreshUserId() {
    const cached = getUserId();
    if (!isLoggedIn()) return Promise.resolve(cached);
    return me().then(function (data) {
      const id = data && data.id != null ? String(data.id) : cached;
      if (id) setUserIdCache(id);
      return id;
    }).catch(function () { return cached; });
  }

  function copyText(text) {
    if (!text) return Promise.resolve(false);
    if (navigator.clipboard && navigator.clipboard.writeText) {
      return navigator.clipboard.writeText(text).then(function () { return true; }).catch(function () { return false; });
    }
    return Promise.resolve(false);
  }

  /** Fill element(s) with User ID. opts: { onCopy(msg) } */
  function paintUserId(target, opts) {
    const options = opts || {};
    const nodes = typeof target === 'string'
      ? Array.prototype.slice.call(document.querySelectorAll(target))
      : (target ? [target] : []);
    if (!nodes.length) return refreshUserId();

    function apply(id) {
      const val = id || '—';
      nodes.forEach(function (el) { el.textContent = val; });
    }
    apply(getUserId());
    return refreshUserId().then(apply);
  }

  global.WXOAuth = {
    API_BASE: API_BASE,
    getToken, getUser, getUserId, isLoggedIn, deviceId,
    setSession, clearSession, logout,
    api, register, login, guestLogin, me,
    walletBalance, walletTransactions, requireAuth,
    refreshUserId, paintUserId, copyText,
    pendingReferral, claimPendingReferral
  };
})(window);
