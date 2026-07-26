export const environment = {
  production: true,
  // En dev el proxy redirige /api y /hubs al backend; en prod se sirven tras el mismo dominio.
  apiBaseUrl: '/api',
  hubBaseUrl: '/hubs',
};
