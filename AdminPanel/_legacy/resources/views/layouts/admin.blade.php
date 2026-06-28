<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>TilesClash Admin</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; background: #0f172a; color: #e2e8f0; }
        nav { background: #1e293b; padding: 1rem 2rem; display: flex; gap: 1rem; flex-wrap: wrap; }
        nav a { color: #93c5fd; text-decoration: none; }
        main { padding: 2rem; }
        .card { background: #1e293b; border-radius: 8px; padding: 1rem; margin-bottom: 1rem; }
        table { width: 100%; border-collapse: collapse; }
        th, td { padding: 0.5rem; border-bottom: 1px solid #334155; text-align: left; }
    </style>
</head>
<body>
    <nav>
        <a href="/">Dashboard</a>
        <a href="/users">Users</a>
        <a href="/wallet">Wallet</a>
        <a href="/tournaments">Tournaments</a>
        <a href="/rooms">Rooms</a>
        <a href="/leaderboard">Leaderboard</a>
        <a href="/reports">Reports</a>
        <a href="/analytics">Analytics</a>
        <a href="/settings">Settings</a>
        <a href="/banners">Banners</a>
        <a href="/notifications">Notifications</a>
        <a href="/support">Support</a>
        <a href="/logs">Logs</a>
    </nav>
    <main>
        @yield('content')
    </main>
</body>
</html>
