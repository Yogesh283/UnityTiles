/**
 * Shared WXO game catalog — ek user ID + ek wallet, har game ka alag lobby.
 * Naya game add: yahan entry + match-lobby.html?game=ID
 */
(function (global) {
  const GAMES = {
    'iq-match': {
      id: 'iq-match',
      name: 'IQ Match',
      tagline: 'Tile race — real Unity',
      engine: 'unity',
      color: '#E31C23',
      rooms: [
        { id: 'duel', label: '2 Players', players: 2, entry: 25, live: false, free: false },
        { id: 'squad5', label: '5 Players', players: 5, entry: 50, live: false, free: false },
        { id: 'room10', label: '10 Players', players: 10, entry: 100, live: false, free: false },
        { id: 'room20', label: '20 Players', players: 20, entry: 200, live: false, free: false },
        { id: 'live5', label: 'Live Stream • 5', players: 5, entry: 75, live: true, free: false },
        { id: 'live10', label: 'Live Stream • 10', players: 10, entry: 150, live: true, free: false },
        { id: 'free', label: 'Free Practice', players: 2, entry: 0, live: false, free: true }
      ]
    },
    ludo: {
      id: 'ludo',
      name: 'Ludo',
      tagline: 'Coming soon',
      engine: 'soon',
      color: '#2563EB',
      rooms: [
        { id: 'duel', label: '2 Players', players: 2, entry: 25, live: false, free: false },
        { id: 'squad4', label: '4 Players', players: 4, entry: 50, live: false, free: false },
        { id: 'free', label: 'Free Practice', players: 2, entry: 0, live: false, free: true }
      ]
    },
    racing: {
      id: 'racing',
      name: 'Car Racing',
      tagline: 'Coming soon',
      engine: 'soon',
      color: '#059669',
      rooms: [
        { id: 'duel', label: '2 Players', players: 2, entry: 30, live: false, free: false },
        { id: 'room8', label: '8 Players', players: 8, entry: 80, live: false, free: false },
        { id: 'live', label: 'Live Stream', players: 8, entry: 100, live: true, free: false },
        { id: 'free', label: 'Free Practice', players: 2, entry: 0, live: false, free: true }
      ]
    }
  };

  /** Winner reward = 2 × entry fee (WXO coins). Losers get 0. */
  function winPrize(entry) {
    const e = Math.floor(Number(entry) || 0);
    return e * 2;
  }

  function getGame(id) {
    const key = String(id || 'iq-match').toLowerCase().replace(/\s+/g, '-');
    if (GAMES[key]) return GAMES[key];
    if (key === 'iqmatch' || key === 'matchiq' || key === 'iq-match' || key === 'iq') return GAMES['iq-match'];
    return GAMES['iq-match'];
  }

  function listGames() {
    return Object.keys(GAMES).map(function (k) { return GAMES[k]; });
  }

  global.WXOGames = { GAMES: GAMES, getGame: getGame, listGames: listGames, winPrize: winPrize };
})(typeof window !== 'undefined' ? window : this);
