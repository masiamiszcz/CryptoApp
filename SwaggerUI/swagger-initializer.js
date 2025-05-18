window.onload = function() {
  window.ui = SwaggerUIBundle({
    urls: [
    { name: "CoinGecko",  url: "http://localhost:5000/swagger/v1/swagger.json" },
    { name: "Currency",   url: "http://localhost:5010/swagger/v1/swagger.json" },
    { name: "CryptoDb",    url: "http://localhost:5020/swagger/v1/swagger.json" },
    { name: "PDF Service", url: "http://localhost:5030/swagger/v1/swagger.json" },
    { name: "WebAppi",     url: "http://localhost:8050/swagger/v1/swagger.json" },
    { name: "Logger",      url: "http://localhost:8500/swagger/v1/swagger.json" }
    ],
    dom_id: '#swagger-ui',
    deepLinking: true,
    presets: [
      SwaggerUIBundle.presets.apis,
      SwaggerUIStandalonePreset
    ],
    layout: "StandaloneLayout"
  });
};
