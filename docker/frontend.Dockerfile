FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/yarn.lock ./
RUN yarn install --frozen-lockfile
COPY frontend/ .
ENV NUXT_PROXY_TARGET=http://email-api:8080
RUN yarn build

FROM node:22-alpine
WORKDIR /app
COPY --from=build /app/.output .
EXPOSE 3000
ENV NUXT_HOST=0.0.0.0
ENV NUXT_PORT=3000
CMD ["node", "server/index.mjs"]
