<?php

namespace App\Support;

use Illuminate\Support\Facades\Http;
use InvalidArgumentException;

class GooglePlayBillingManager
{
    public function credentialsPath(): string
    {
        return base_path('../Backend/secrets/google-play-service-account.json');
    }

    public function backendEnvPath(): string
    {
        return base_path('../Backend/.env');
    }

    public function readCredentialsJson(): ?string
    {
        $path = $this->credentialsPath();
        if (! is_file($path)) {
            return null;
        }

        return file_get_contents($path) ?: null;
    }

    public function readPackageName(): string
    {
        $env = $this->readEnvValue('GOOGLE_PLAY_PACKAGE_NAME');

        return $env !== '' ? $env : 'fun.matchiq.game';
    }

    public function isConfigured(): bool
    {
        $path = $this->credentialsPath();
        if (! is_file($path)) {
            return false;
        }

        $data = json_decode(file_get_contents($path) ?: '', true);

        return is_array($data)
            && ! empty($data['client_email'])
            && ! empty($data['private_key']);
    }

    public function saveCredentials(string $json): void
    {
        $json = trim($json);
        if ($json === '') {
            throw new InvalidArgumentException('Service account JSON is required.');
        }

        $data = json_decode($json, true);
        if (! is_array($data) || empty($data['client_email']) || empty($data['private_key'])) {
            throw new InvalidArgumentException('Invalid Google Play service account JSON.');
        }

        $path = $this->credentialsPath();
        $dir = dirname($path);
        if (! is_dir($dir) && ! mkdir($dir, 0750, true) && ! is_dir($dir)) {
            throw new InvalidArgumentException('Could not create secrets directory.');
        }

        file_put_contents(
            $path,
            json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES).PHP_EOL
        );

        $this->upsertEnvValue(
            'GOOGLE_PLAY_SERVICE_ACCOUNT_JSON',
            'secrets/google-play-service-account.json'
        );
    }

    public function updatePackageName(string $packageName): void
    {
        $packageName = trim($packageName);
        if ($packageName === '') {
            throw new InvalidArgumentException('Package name is required.');
        }

        $this->upsertEnvValue('GOOGLE_PLAY_PACKAGE_NAME', $packageName);
    }

    public function fetchApiStatus(): ?string
    {
        $baseUrl = rtrim((string) config('services.matchiq.api_base_url', env('API_BASE_URL', '')), '/');
        if ($baseUrl === '') {
            return 'API_BASE_URL not set in Admin .env';
        }

        try {
            $response = Http::timeout(8)->get($baseUrl.'/api/v1/payments/google/status');
            if (! $response->successful()) {
                return 'API error: HTTP '.$response->status();
            }

            $active = (bool) ($response->json('active') ?? $response->json('configured'));
            $error = $response->json('error');

            if ($active) {
                return 'Active on API server';
            }

            return $error ? 'Not active: '.$error : 'Not active on API server (restart matchiq-api after saving key)';
        } catch (\Throwable $e) {
            return 'Could not reach API: '.$e->getMessage();
        }
    }

    private function readEnvValue(string $key): string
    {
        $path = $this->backendEnvPath();
        if (! is_file($path)) {
            return '';
        }

        $lines = file($path, FILE_IGNORE_NEW_LINES) ?: [];
        foreach ($lines as $line) {
            if (str_starts_with(trim($line), $key.'=')) {
                return trim(substr($line, strlen($key) + 1), " \t\"'");
            }
        }

        return '';
    }

    private function upsertEnvValue(string $key, string $value): void
    {
        $path = $this->backendEnvPath();
        if (! is_file($path)) {
            return;
        }

        $lines = file($path, FILE_IGNORE_NEW_LINES) ?: [];
        $found = false;
        $updated = [];

        foreach ($lines as $line) {
            if (str_starts_with(trim($line), $key.'=')) {
                $updated[] = $key.'='.$value;
                $found = true;
            } else {
                $updated[] = $line;
            }
        }

        if (! $found) {
            $updated[] = $key.'='.$value;
        }

        file_put_contents($path, implode(PHP_EOL, $updated).PHP_EOL);
    }
}
