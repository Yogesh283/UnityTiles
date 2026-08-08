/* Shared WXO wallet — one balance for all games & tournaments */
(function (global) {
  const BAL_KEY = 'wxo_balance';
  const TX_KEY = 'wxo_tx_v1';
  const DEFAULT_BAL = 0;

  function getBal() {
    const raw = localStorage.getItem(BAL_KEY);
    if (raw === null || raw === '') return DEFAULT_BAL;
    const n = Number(raw);
    return Number.isFinite(n) ? n : DEFAULT_BAL;
  }

  function setBal(n) {
    const v = Math.max(0, Math.round(n));
    localStorage.setItem(BAL_KEY, String(v));
    global.dispatchEvent(new CustomEvent('wxo:balance', { detail: { balance: v } }));
    return v;
  }

  /** Pull server-side balance (source of truth) when the user is logged in. */
  function syncFromServer() {
    if (!global.WXOAuth || !global.WXOAuth.isLoggedIn()) {
      return Promise.resolve({ ok: false, offline: true });
    }
    return global.WXOAuth.walletBalance()
      .then(function (data) {
        if (data && typeof data.balance === 'number') {
          setBal(data.balance);
          paintBalanceElements();
          return { ok: true, balance: data.balance };
        }
        return { ok: false };
      })
      .catch(function () {
        return { ok: false };
      });
  }

  function addTx(tx) {
    const list = getTx();
    list.unshift({
      id: 'tx_' + Date.now(),
      at: new Date().toISOString(),
      ...tx
    });
    localStorage.setItem(TX_KEY, JSON.stringify(list.slice(0, 50)));
  }

  function getTx() {
    try { return JSON.parse(localStorage.getItem(TX_KEY) || '[]'); }
    catch { return []; }
  }

  function deposit(amount, method) {
    const amt = Math.floor(Number(amount) || 0);
    if (amt < 10) return { ok: false, error: 'Minimum deposit ₹10' };
    const bal = setBal(getBal() + amt);
    addTx({ type: 'in', amount: amt, title: 'Deposit', note: method || 'UPI' });
    return { ok: true, balance: bal, amount: amt };
  }

  function withdraw(amount, method) {
    const amt = Math.floor(Number(amount) || 0);
    if (amt < 10) return { ok: false, error: 'Minimum withdrawal 10 WXO' };
    if (amt > getBal()) return { ok: false, error: 'Insufficient balance' };
    const bal = setBal(getBal() - amt);
    addTx({ type: 'out', amount: amt, title: 'Withdraw', note: method || 'UPI' });
    return { ok: true, balance: bal, amount: amt };
  }

  /** Spend entry fee for any game / tournament */
  function spend(amount, title, note) {
    const amt = Math.floor(Number(amount) || 0);
    if (amt <= 0) return { ok: true, balance: getBal(), amount: 0 };
    if (amt > getBal()) return { ok: false, error: 'Insufficient WXO. Please deposit first.', need: amt - getBal() };
    const bal = setBal(getBal() - amt);
    addTx({ type: 'out', amount: amt, title: title || 'Game entry', note: note || '' });
    return { ok: true, balance: bal, amount: amt };
  }

  /** Credit win reward for any game */
  function creditWin(amount, title, note) {
    const amt = Math.floor(Number(amount) || 0);
    if (amt <= 0) return { ok: true, balance: getBal(), amount: 0 };
    const bal = setBal(getBal() + amt);
    addTx({ type: 'in', amount: amt, title: title || 'Tournament won', note: note || '' });
    return { ok: true, balance: bal, amount: amt };
  }

  function ensurePopupStyles() {
    if (document.getElementById('wxo-popup-css')) return;
    const css = document.createElement('style');
    css.id = 'wxo-popup-css';
    css.textContent = `
      .wxo-overlay{position:fixed;inset:0;background:rgba(0,0,0,.55);z-index:200;display:flex;align-items:center;justify-content:center;padding:16px;opacity:0;pointer-events:none;transition:.25s}
      .wxo-overlay.show{opacity:1;pointer-events:auto}
      .wxo-modal{width:100%;max-width:340px;background:#fff;border-radius:20px;padding:22px 18px;text-align:center;box-shadow:0 20px 50px rgba(0,0,0,.35);transform:scale(.92);transition:.25s}
      .wxo-overlay.show .wxo-modal{transform:scale(1)}
      .wxo-modal .ico{font-size:48px;margin-bottom:8px}
      .wxo-modal h3{font-family:Montserrat,system-ui,sans-serif;font-size:20px;font-weight:800;margin:0 0 6px;color:#111}
      .wxo-modal p{font-size:13px;color:#6B7280;margin:0 0 14px;line-height:1.45}
      .wxo-modal .prize{font-family:Montserrat,system-ui,sans-serif;font-size:28px;font-weight:800;color:#E31C23;margin-bottom:6px}
      .wxo-modal .bal{font-size:12px;color:#B45309;font-weight:700;margin-bottom:14px}
      .wxo-modal .acts{display:flex;flex-direction:column;gap:8px}
      .wxo-modal .btn{display:block;width:100%;border:0;border-radius:12px;padding:12px;font-weight:800;font-size:14px;cursor:pointer;font-family:inherit}
      .wxo-modal .btn.primary{background:#E31C23;color:#fff}
      .wxo-modal .btn.ghost{background:#F7F8FA;color:#111;border:1px solid #ECEEF2}
      .wxo-modal.lose .prize{color:#6B7280;font-size:18px}
    `;
    document.head.appendChild(css);
  }

  function closePopup() {
    const el = document.getElementById('wxoOverlay');
    if (el) el.classList.remove('show');
  }

  function showPopup(opts) {
    ensurePopupStyles();
    let el = document.getElementById('wxoOverlay');
    if (!el) {
      el = document.createElement('div');
      el.id = 'wxoOverlay';
      el.className = 'wxo-overlay';
      el.innerHTML = '<div class="wxo-modal" id="wxoModal"></div>';
      el.addEventListener('click', (e) => { if (e.target === el) closePopup(); });
      document.body.appendChild(el);
    }
    const modal = el.querySelector('#wxoModal');
    modal.className = 'wxo-modal' + (opts.lose ? ' lose' : '');
    modal.innerHTML =
      '<div class="ico">' + (opts.icon || '🏆') + '</div>' +
      '<h3>' + (opts.title || 'You Won!') + '</h3>' +
      (opts.prize != null ? '<div class="prize">+' + Number(opts.prize).toLocaleString('en-IN') + ' WXO</div>' : '') +
      '<p>' + (opts.message || '') + '</p>' +
      '<div class="bal">Wallet: ' + getBal().toLocaleString('en-IN') + ' WXO</div>' +
      '<div class="acts">' +
        '<button type="button" class="btn primary" id="wxoPopOk">' + (opts.okText || 'Awesome') + '</button>' +
        (opts.secondaryHtml || '') +
      '</div>';
    el.classList.add('show');
    modal.querySelector('#wxoPopOk').onclick = () => {
      closePopup();
      if (typeof opts.onOk === 'function') opts.onOk();
    };
  }

  /**
   * Join any game/tournament using shared wallet.
   * Deducts entry → short play → win popup + credit reward (demo skill win).
   */
  function playGame(options) {
    const {
      game = 'Game',
      entry = 0,
      prize = 0,
      winChance = 0.65,
      onNeedDeposit
    } = options || {};

    const spendRes = spend(entry, game + ' entry', 'Entry fee');
    if (!spendRes.ok) {
      showPopup({
        lose: true,
        icon: '👛',
        title: 'Not enough coins',
        prize: null,
        message: spendRes.error + (spendRes.need ? ' Need ' + spendRes.need + ' more WXO.' : ''),
        okText: 'Deposit Now',
        onOk: () => {
          if (typeof onNeedDeposit === 'function') onNeedDeposit();
          else location.href = '/wallet.html#deposit';
        }
      });
      return { ok: false };
    }

    showPopup({
      lose: true,
      icon: '🎮',
      title: 'Playing ' + game + '…',
      prize: null,
      message: entry ? ('Entry ' + entry + ' WXO deducted. Good luck!') : 'Match starting…',
      okText: 'Please wait…'
    });
    const waitBtn = document.querySelector('#wxoPopOk');
    if (waitBtn) waitBtn.disabled = true;

    setTimeout(() => {
      const won = Math.random() < winChance;
      if (won) {
        creditWin(prize, game + ' won', 'Prize reward');
        showPopup({
          icon: '🏆',
          title: 'You Won!',
          prize: prize,
          message: 'Congratulations! Prize credited to your WXO wallet — usable in all games.',
          okText: 'Collect Reward',
          secondaryHtml: '<a class="btn ghost" href="/tournaments.html">Play Again</a>'
        });
      } else {
        showPopup({
          lose: true,
          icon: '😔',
          title: 'Better luck next time',
          prize: null,
          message: 'You lost this round. Entry was used. Deposit once — play any game on WXO.',
          okText: 'Try Again',
          secondaryHtml: '<a class="btn ghost" href="/wallet.html#deposit">Add Coins</a>'
        });
      }
    }, 1400);

    return { ok: true };
  }

  function paintBalanceElements() {
    const bal = getBal();
    document.querySelectorAll('[data-wxo-balance]').forEach((el) => {
      el.textContent = bal.toLocaleString('en-IN');
    });
    document.querySelectorAll('[data-wxo-balance-label]').forEach((el) => {
      el.textContent = bal.toLocaleString('en-IN') + ' WXO';
    });
  }

  global.addEventListener('wxo:balance', paintBalanceElements);

  function isInApp() {
    try {
      if (typeof window !== 'undefined' && window.WXO_IN_APP) return true;
      var ua = (typeof navigator !== 'undefined' && navigator.userAgent) ? navigator.userAgent : '';
      if (/WXO-App/i.test(ua) || /fun\.wxo\.app/i.test(ua)) return true;
    } catch (e) {}
    return false;
  }

  /**
   * Open shared match lobby (2/5/10/20, live, free). Same page pattern for every game.
   */
  function openMatchLobby(options) {
    const opts = options || {};
    const gameId = opts.gameId || slugGame(opts.game || 'IQ Match');
    const q = '?game=' + encodeURIComponent(gameId);
    try {
      window.location.assign('/match-lobby.html' + q);
    } catch (e) {
      window.location.href = '/match-lobby.html' + q;
    }
    return { ok: true, lobby: true };
  }

  function slugGame(name) {
    const n = String(name || '').toLowerCase();
    if (n.indexOf('ludo') >= 0) return 'ludo';
    if (n.indexOf('rac') >= 0) return 'racing';
    return 'iq-match';
  }

  /**
   * After lobby fee is paid: open native Unity (WXO APK) or HTML play fallback.
   * Never downloads another APK. skipLobby required from match-lobby Join.
   */
  function openUnityGame(options) {
    const opts = options || {};
    const game = opts.game || 'IQ Match';
    const gameId = opts.gameId || slugGame(game);
    const roomKey = opts.roomKey || null;
    const entry = Number(opts.entry) || 0;
    const prize = opts.prize != null ? Number(opts.prize) : entry * 2;
    const mode = opts.mode || 'tournament';
    const players = Number(opts.players) || 2;
    const live = !!opts.live;

    // Default Play buttons → lobby first (fee / room select)
    if (!opts.skipLobby) {
      return openMatchLobby({ game: game, gameId: gameId });
    }

    try {
      if (
        typeof window !== 'undefined' &&
        window.ReactNativeWebView &&
        typeof window.ReactNativeWebView.postMessage === 'function'
      ) {
        window.ReactNativeWebView.postMessage(
          JSON.stringify({
            type: 'PLAY_UNITY',
            game: game,
            gameId: gameId,
            roomKey: roomKey,
            entry: entry,
            prize: prize,
            mode: mode,
            players: players,
            live: live,
            // The app joins the server room as this account, so hand over the web session.
            token: global.WXOAuth ? global.WXOAuth.getToken() : null,
            user: global.WXOAuth ? global.WXOAuth.getUser() : null
          })
        );
        return { ok: true, native: true };
      }
    } catch (e) {}

    const q =
      '?game=' + encodeURIComponent(game) +
      '&entry=' + encodeURIComponent(String(entry)) +
      '&prize=' + encodeURIComponent(String(prize)) +
      '&inapp=1';

    try {
      window.location.assign('/play.html' + q);
    } catch (e) {
      window.location.href = '/play.html' + q;
    }
    return { ok: true };
  }

  /** Apply win/lose from native Unity automatically (shared wallet). */
  function applyMatchResult(payload) {
    const p = payload || {};
    let pending = null;
    try {
      pending = JSON.parse(sessionStorage.getItem('wxo_pending_unity') || 'null');
    } catch (_) {}
    const prize = Number(p.prize != null ? p.prize : (pending && pending.prize) || 0);
    const game = p.game || (pending && pending.game) || 'IQ Match';
    const matchId = p.matchId || (pending && pending.matchId) || ('m_' + Date.now());

    if (sessionStorage.getItem('wxo_unity_claim_shown') === matchId) {
      return { ok: true, skipped: true };
    }
    sessionStorage.setItem('wxo_unity_claim_shown', matchId);
    sessionStorage.removeItem('wxo_pending_unity');

    if (p.won) {
      creditWin(prize, game + ' won', '2× entry reward');
      if (!p.silent) {
        showPopup({
          icon: '🏆',
          title: 'You Won!',
          prize: prize,
          message: '2× entry credited to your shared WXO wallet. Use it in any game.',
          okText: 'Awesome'
        });
      }
      return { ok: true, won: true, prize: prize };
    }

    if (!p.silent) {
      showPopup({
        lose: true,
        icon: '😔',
        title: 'Good game',
        prize: null,
        message: 'No reward this round. Entry was used. Same wallet for all games — try again.',
        okText: 'Back to Lobby',
        onOk: function () {
          const gid = (pending && pending.gameId) || slugGame(game);
          location.href = '/match-lobby.html?game=' + encodeURIComponent(gid);
        }
      });
    }
    return { ok: true, won: false };
  }

  /** If user returns from Unity without auto result, offer win claim popup */
  function checkUnityReturn() {
    // The match is still being played on the game page itself — nothing to claim yet
    if (/\/play\.html$/i.test(location.pathname)) return;

    let pending = null;
    try {
      pending = JSON.parse(sessionStorage.getItem('wxo_pending_unity') || 'null');
    } catch (_) {}
    if (!pending || !pending.at) return;
    if (Date.now() - pending.at > 2 * 60 * 60 * 1000) {
      sessionStorage.removeItem('wxo_pending_unity');
      return;
    }
    if (sessionStorage.getItem('wxo_unity_claim_shown') === pending.matchId) return;

    ensurePopupStyles();
    showPopup({
      icon: '🏆',
      title: 'Back from ' + (pending.game || 'IQ Match') + '?',
      prize: pending.prize,
      message: 'Did you win? Claim 2× entry to your shared WXO wallet.',
      okText: 'I Won — Claim',
      secondaryHtml: '<button type="button" class="btn ghost" id="wxoLostBtn">I Lost</button>',
      onOk: () => {
        applyMatchResult({ won: true, prize: pending.prize, game: pending.game, matchId: pending.matchId });
      }
    });

    const lost = document.getElementById('wxoLostBtn');
    if (lost) {
      lost.onclick = () => {
        closePopup();
        applyMatchResult({ won: false, prize: pending.prize, game: pending.game, matchId: pending.matchId });
      };
    }
  }

  global.WXOWallet = {
    getBal, getBalance: getBal, setBal, deposit, withdraw, spend, creditWin,
    playGame, launchUnity: openMatchLobby, openUnityGame, openMatchLobby, applyMatchResult, checkUnityReturn,
    showPopup, closePopup, getTx, addTx, paintBalanceElements, isInApp, syncFromServer
  };

  function boot() {
    paintBalanceElements();
    checkUnityReturn();
    syncFromServer();
  }

  global.addEventListener('wxo:auth', syncFromServer);

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})(window);
