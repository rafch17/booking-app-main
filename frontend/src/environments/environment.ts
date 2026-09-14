// Production defaults. Overridden by environment.development.ts in dev builds
// (see angular.json -> build.configurations.development.fileReplacements).
// Adjust apiUrl to the real deployed backend URL before shipping to production.
export const environment = {
  production: true,
  apiUrl: '/api',
};
