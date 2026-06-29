<?php

namespace App\Filament\Pages;

use App\Support\GooglePlayBillingManager;
use BackedEnum;
use Filament\Notifications\Notification;
use Filament\Pages\Page;
use Filament\Support\Icons\Heroicon;
use UnitEnum;

class GooglePlayBilling extends Page
{
    protected static string|BackedEnum|null $navigationIcon = Heroicon::OutlinedCreditCard;

    protected static ?string $navigationLabel = 'Play Billing';

    protected static string|UnitEnum|null $navigationGroup = 'System';

    protected static ?int $navigationSort = 2;

    protected string $view = 'filament.pages.google-play-billing';

    public string $serviceAccountJson = '';

    public string $packageName = 'fun.matchiq.game';

    public bool $isActive = false;

    public ?string $apiStatus = null;

    public function mount(GooglePlayBillingManager $manager): void
    {
        $this->serviceAccountJson = $manager->readCredentialsJson() ?? '';
        $this->packageName = $manager->readPackageName();
        $this->syncStatus($manager);
    }

    public function save(GooglePlayBillingManager $manager): void
    {
        try {
            $manager->saveCredentials($this->serviceAccountJson);
            $manager->updatePackageName($this->packageName);
            $this->syncStatus($manager);

            Notification::make()
                ->title('Billing key saved')
                ->body(
                    $this->isActive
                        ? 'Key is valid. Run: systemctl restart matchiq-api'
                        : 'Key saved — check JSON format.'
                )
                ->success()
                ->send();
        } catch (\InvalidArgumentException $e) {
            Notification::make()
                ->title('Save failed')
                ->body($e->getMessage())
                ->danger()
                ->send();
        }
    }

    public function refreshStatus(): void
    {
        $this->syncStatus(app(GooglePlayBillingManager::class));
    }

    private function syncStatus(GooglePlayBillingManager $manager): void
    {
        $this->isActive = $manager->isConfigured();
        $this->apiStatus = $manager->fetchApiStatus();
    }
}
