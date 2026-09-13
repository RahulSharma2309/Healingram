FROM node:22-alpine AS build
WORKDIR /web
ARG VITE_API_BASE_URL=
ARG VITE_DEMO_MODE=false
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_DEMO_MODE=$VITE_DEMO_MODE
COPY package.json package-lock.json ./
RUN npm ci
COPY index.html vite.config.ts tsconfig.json tsconfig.app.json tsconfig.node.json ./
COPY src ./src
COPY public ./public
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY infra/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 80
