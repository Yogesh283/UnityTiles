# XAMPP Setup Guide

## Requirements
- XAMPP (Apache + MySQL)
- Python 3.11+
- PHP 8.2+ and Composer (for Admin Panel)
- Unity 6000.3.17f1

## MySQL Database

1. Start MySQL in XAMPP Control Panel
2. Open http://localhost/phpmyadmin
3. Import `Database/tilesclash.sql`
4. Database name: **`game`**
5. Default credentials: `root` / (empty password)

## Backend

```bash
cd Backend
python -m venv venv
venv\Scripts\activate
pip install -r requirements.txt
copy .env.example .env
```

`.env` for XAMPP:
```
DATABASE_URL=mysql+pymysql://root:@localhost:3306/game
```

Start API:
```bash
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

## Admin Panel

```bash
cd AdminPanel
composer create-project laravel/laravel . --prefer-dist
# OR if scaffold exists:
composer install
copy .env.example .env
php artisan key:generate
php artisan serve --port=8080
```

`.env` database section:
```
DB_DATABASE=game
DB_USERNAME=root
DB_PASSWORD=
```

Default admin (after SQL import): `admin@tilesclash.com` / `admin123`

## Unity

1. Open `Unity_Game/` in Unity Hub
2. Build APK to `Unity_Game/Builds/`
3. For local API testing, set `TilesClashApiConfig.UseProduction = false`
