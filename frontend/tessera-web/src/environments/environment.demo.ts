/**
 * Offline showcase: an HTTP interceptor emulates the gateway in the browser, so the UI can be
 * explored (and screenshotted) without Docker. Never used by the real deployment.
 */
export const environment = {
  production: false,
  demo: true,
  apiBaseUrl: ''
};
