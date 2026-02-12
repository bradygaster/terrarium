// Terrarium Service Worker — PWA offline capability
// Basic shell caching for offline support

const CACHE_NAME = 'terrarium-shell-v1';
const SHELL_ASSETS = [
  '/',
  '/css/glass-theme.css',
  '/css/glass-components.css',
  '/css/pages.css',
  '/js/terrarium-renderer.js',
  '/manifest.json'
];

// Install event — cache shell assets
self.addEventListener('install', event => {
  console.log('[ServiceWorker] Installing...');
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('[ServiceWorker] Caching shell assets');
        return cache.addAll(SHELL_ASSETS);
      })
      .then(() => self.skipWaiting())
  );
});

// Activate event — clean up old caches
self.addEventListener('activate', event => {
  console.log('[ServiceWorker] Activating...');
  event.waitUntil(
    caches.keys()
      .then(cacheNames => {
        return Promise.all(
          cacheNames
            .filter(cacheName => cacheName !== CACHE_NAME)
            .map(cacheName => {
              console.log('[ServiceWorker] Deleting old cache:', cacheName);
              return caches.delete(cacheName);
            })
        );
      })
      .then(() => self.clients.claim())
  );
});

// Fetch event — network-first strategy with cache fallback
self.addEventListener('fetch', event => {
  // Skip non-GET requests
  if (event.request.method !== 'GET') {
    return;
  }

  // Skip SignalR and API requests
  const url = new URL(event.request.url);
  if (url.pathname.startsWith('/_') || 
      url.pathname.startsWith('/api/') ||
      url.pathname.includes('/hub/')) {
    return;
  }

  event.respondWith(
    fetch(event.request)
      .then(response => {
        // Clone the response before caching
        const responseToCache = response.clone();
        
        // Cache successful responses
        if (response.status === 200) {
          caches.open(CACHE_NAME)
            .then(cache => cache.put(event.request, responseToCache));
        }
        
        return response;
      })
      .catch(() => {
        // Network failed — try cache
        return caches.match(event.request)
          .then(cachedResponse => {
            if (cachedResponse) {
              console.log('[ServiceWorker] Serving from cache:', event.request.url);
              return cachedResponse;
            }
            
            // Not in cache — return offline page or error
            if (event.request.mode === 'navigate') {
              return caches.match('/');
            }
            
            // For other resources, just fail
            return new Response('Offline - resource not cached', {
              status: 503,
              statusText: 'Service Unavailable'
            });
          });
      })
  );
});

// Message event — allow clients to control cache
self.addEventListener('message', event => {
  if (event.data && event.data.type === 'SKIP_WAITING') {
    self.skipWaiting();
  }
  
  if (event.data && event.data.type === 'CLEAR_CACHE') {
    event.waitUntil(
      caches.delete(CACHE_NAME)
        .then(() => {
          console.log('[ServiceWorker] Cache cleared');
          return self.registration.update();
        })
    );
  }
});
