/**
 * Production build (served by nginx in Docker). The SPA and the API share an origin:
 * nginx forwards /api and /hubs to the gateway, so the base URL is empty.
 */
export const environment = {
  production: true,
  demo: false,
  apiBaseUrl: ''
};
