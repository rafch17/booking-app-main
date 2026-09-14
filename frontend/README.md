# booking-app - Frontend

Backend service for the booking-app project.
This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.17.

## Prerequisites

- Node.js >= 18.19 (required by Angular CLI 19; this machine's active `node` was found to
  be v16.20.2 via nvm — run `nvm use 20.20.2` or similar before `ng serve`/`ng build`)
- npm / yarn / pnpm
- Git

Check the backend `package.json` for exact engine versions.

## Environment / API URL

The backend base URL lives in `src/environments/environment.ts` (production default,
`apiUrl: '/api'`) and `src/environments/environment.development.ts`
(`apiUrl: 'http://localhost:5170/api'`, used automatically by `ng serve` / the
`development` build configuration). Adjust `environment.ts` before deploying if the
frontend and backend aren't served from the same origin.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Building

To build the project run:

```bash
ng build
```

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```
