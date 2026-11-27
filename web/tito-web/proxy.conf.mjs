// Dev-server proxy: the browser calls /api/... on the Angular origin and `ng serve`
// forwards it to the API. Only used by `ng serve`, never in production builds.
//
// Under Aspire, `.WithReference(api)` injects the API address as environment variables
// named services__<resource>__<scheme>__<index>. Without Aspire, fall back to the
// API's launchSettings.json http URL.
const target =
  process.env['services__api__https__0'] ??
  process.env['services__api__http__0'] ??
  'http://localhost:5010';

export default {
  '/api': {
    target,
    secure: false, // accept the local ASP.NET Core dev certificate
    changeOrigin: true,
  },
};
