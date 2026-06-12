# Phase 1 placeholder: serves a static HTML page.
# Will be replaced with a Node/Vite multi-stage build when the React frontend is scaffolded.
FROM nginx:alpine

COPY apps/web/public /usr/share/nginx/html
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
