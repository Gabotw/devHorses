export const environment = {
  production: true,
  // En prod el panel (Vercel) llama directo al backend en Render (dominio distinto),
  // por eso las URLs son absolutas. El backend habilita CORS para el dominio de Vercel.
  apiBaseUrl: 'https://gymflow-api-rrtn.onrender.com/api',
  hubBaseUrl: 'https://gymflow-api-rrtn.onrender.com/hubs',
};
