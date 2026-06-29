<x-filament-panels::page>
    <div class="space-y-6">
        <x-filament::section>
            <x-slot name="heading">Billing status</x-slot>

            <div class="space-y-2 text-sm">
                <p>
                    <strong>Local key:</strong>
                    @if ($isActive)
                        <span class="text-success-600">Valid — billing ready</span>
                    @else
                        <span class="text-danger-600">Not set or invalid</span>
                    @endif
                </p>
                <p><strong>API server:</strong> {{ $apiStatus ?? 'Unknown' }}</p>
                <p class="text-gray-500">
                    Key save hone ke baad server par
                    <code>systemctl restart matchiq-api</code>
                    chalao — billing active ho jayega.
                </p>
            </div>
        </x-filament::section>

        <form wire:submit="save" class="space-y-6">
            <x-filament::section>
                <x-slot name="heading">Google Play service account</x-slot>

                <div class="space-y-4">
                    <div>
                        <label class="text-sm font-medium">Package name</label>
                        <input
                            type="text"
                            wire:model="packageName"
                            class="mt-1 block w-full rounded-lg border-gray-300 shadow-sm"
                            placeholder="fun.matchiq.game"
                        />
                    </div>

                    <div>
                        <label class="text-sm font-medium">Service account JSON</label>
                        <textarea
                            wire:model="serviceAccountJson"
                            rows="14"
                            class="mt-1 block w-full rounded-lg border-gray-300 font-mono text-xs shadow-sm"
                            placeholder='Paste Google Play Console service account JSON here'
                        ></textarea>
                    </div>
                </div>
            </x-filament::section>

            <div class="flex gap-3">
                <x-filament::button type="submit">
                    Save key
                </x-filament::button>

                <x-filament::button type="button" color="gray" wire:click="refreshStatus">
                    Refresh status
                </x-filament::button>
            </div>
        </form>
    </div>
</x-filament-panels::page>
