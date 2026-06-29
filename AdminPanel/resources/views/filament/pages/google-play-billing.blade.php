<x-filament-panels::page>
    <div class="space-y-6">
        <div class="rounded-xl bg-white p-6 shadow-sm ring-1 ring-gray-950/5 dark:bg-gray-900 dark:ring-white/10">
            <h2 class="text-base font-semibold">Billing status</h2>
            <div class="mt-3 space-y-2 text-sm">
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
                    After saving the key, run
                    <code>systemctl restart matchiq-api</code>
                    on the server.
                </p>
            </div>
        </div>

        <form wire:submit="save" class="space-y-6">
            <div class="rounded-xl bg-white p-6 shadow-sm ring-1 ring-gray-950/5 dark:bg-gray-900 dark:ring-white/10">
                <h2 class="text-base font-semibold">Google Play service account</h2>

                <div class="mt-4 space-y-4">
                    <div>
                        <label class="text-sm font-medium">Package name</label>
                        <input
                            type="text"
                            wire:model="packageName"
                            class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 shadow-sm dark:border-gray-700 dark:bg-gray-950"
                            placeholder="fun.matchiq.game"
                        />
                    </div>

                    <div>
                        <label class="text-sm font-medium">Service account JSON</label>
                        <textarea
                            wire:model="serviceAccountJson"
                            rows="14"
                            class="mt-1 block w-full rounded-lg border border-gray-300 px-3 py-2 font-mono text-xs shadow-sm dark:border-gray-700 dark:bg-gray-950"
                            placeholder="Paste Google Play Console service account JSON here"
                        ></textarea>
                    </div>
                </div>
            </div>

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
